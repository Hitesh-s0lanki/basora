# 05 — PostgreSQL Data Layer

Owner project: **`Basora.PostgreSQL`**. This is the only project permitted to reference
Npgsql. Everything it returns is a `Basora.Core` domain type.

---

## 1. Connection lifecycle

### NpgsqlDataSource per session

One `NpgsqlDataSource` per `IDatabaseSession`, built once at open time. It owns the
pool, type mappings and logging configuration.

```csharp
var builder = new NpgsqlDataSourceBuilder(connectionString);
builder.UseLoggerFactory(loggerFactory);
builder.ConnectionStringBuilder.ApplicationName = profile.ApplicationName;
builder.ConnectionStringBuilder.Timeout = (int)profile.ConnectTimeout.TotalSeconds;
builder.ConnectionStringBuilder.CommandTimeout = (int)profile.CommandTimeout.TotalSeconds;
builder.ConnectionStringBuilder.IncludeErrorDetail = true;   // gives us column/constraint on errors
await using var dataSource = builder.Build();
```

### Three logical channels

| Channel | Pool budget | Purpose |
|---|---|---|
| `IQueryChannel` | up to N (settings, default 5) | User-initiated queries. Cancellable. One connection per running query so parallel tabs work |
| `IMetadataChannel` | 1 dedicated | Object tree, table descriptors, autocomplete refresh. Must never queue behind a long user query |
| `IMonitorChannel` | 1 dedicated, lazily opened | Dashboard/activity polling. Separate so cancelling a query does not kill monitoring, and vice versa |

Each channel sets a session-level `application_name` suffix
(`Basora/query`, `Basora/meta`, `Basora/monitor`) so the user can identify our own
connections in `pg_stat_activity` — including our own monitoring connection, which we
filter out of the activity view by default.

### Read-only enforcement

When safe mode or a read-only profile is active, the query channel issues
`SET SESSION CHARACTERISTICS AS TRANSACTION READ ONLY` on open. This is a real server-side
guarantee, not a client-side string check, and it is the backstop behind the safety
engine.

### Reconnection

On a broken connection (`57P01` admin shutdown, network drop), the session moves to
`Reconnecting`, retries with backoff (1s, 2s, 5s, 10s, then manual), and surfaces a
non-modal banner. Open documents keep their text and results; only live cursors are lost.

---

## 2. Server capability probe

Run once immediately after connect, in a single round trip where possible:

```sql
SELECT
    current_setting('server_version')                        AS server_version,
    current_setting('server_version_num')::int               AS server_version_num,
    current_user                                             AS current_user,
    current_database()                                       AS current_database,
    pg_is_in_recovery()                                      AS is_replica,
    (SELECT rolsuper FROM pg_roles WHERE rolname = current_user) AS is_superuser;

SELECT extname, extversion, nspname
FROM pg_extension e
JOIN pg_namespace n ON n.oid = e.extnamespace;
```

`pg_stat_statements` needs two separate checks — installed, and readable by this role:

```sql
SELECT to_regclass('pg_stat_statements') IS NOT NULL AS installed,
       has_table_privilege(current_user, 'pg_stat_statements', 'SELECT') AS readable;
```

Results populate `ServerCapabilities`. **Never branch on a raw version number outside
this probe.**

### Minimum supported server

PostgreSQL **13**. Below that, connect but show a persistent banner and disable the
intelligence features that depend on newer catalog columns. Above: feature-detect.

---

## 3. Catalog queries

All metadata comes from `pg_catalog`, not `information_schema`. `information_schema` is
standards-portable but slow, incomplete for PostgreSQL specifics (partitioning, index
methods, `INCLUDE` columns, generated columns) and hides objects the current role cannot
access in ways that confuse users.

Every catalog query lives as an embedded `.sql` resource under
`Basora.PostgreSQL/Metadata/Sql/`, one file per query, named after its method. This
keeps SQL reviewable and diffable, and lets `Verify` snapshot-test the generated shapes.

### Query inventory (MVP 1)

| File | Returns | Notes |
|---|---|---|
| `list_databases.sql` | databases + size + encoding + owner | Exclude templates unless "show system" |
| `list_schemas.sql` | schemas + owner + comment | `pg_catalog`, `information_schema`, `pg_toast*` hidden by default |
| `list_tables.sql` | tables, partitioned tables, foreign tables | Includes `reltuples`, `pg_total_relation_size`, `relpersistence` |
| `list_views.sql` | views + matviews | Separate `relkind` filter |
| `list_columns.sql` | columns for one relation | `format_type`, `attnotnull`, `pg_get_expr` default, `attidentity`, `attgenerated` |
| `list_indexes.sql` | indexes for one relation | `pg_get_indexdef`, `pg_relation_size`, joined to `pg_stat_user_indexes` for scans |
| `list_constraints.sql` | all constraint kinds | `pg_get_constraintdef` |
| `list_foreign_keys.sql` | FK graph for a schema | Both directions; used by relations tab and ER diagram |
| `list_triggers.sql` | triggers | Exclude internal FK triggers (`tgisinternal`) |
| `list_functions.sql` | functions + procedures | `prokind` split; `pg_get_functiondef` on demand only |
| `list_sequences.sql` | sequences + last value | `pg_sequences` view |
| `list_types.sql` | enums, domains, composites | |
| `list_extensions.sql` | installed + available | `pg_extension` UNION `pg_available_extensions` |
| `list_roles.sql` | roles + attributes + memberships | |
| `table_statistics.sql` | live/dead tuples, vacuum/analyze times, sizes | `pg_stat_user_tables` + `pg_class` |
| `search_objects.sql` | Open Anything / global search | One query across relations, columns, functions with rank |

### Cardinal rules

1. **Always parameterise.** Identifiers that cannot be parameters go through
   `QuoteIdentifier` (section 6), never string concatenation of raw user input.
2. **Always bound the result.** Catalog queries on databases with 50k+ relations must
   paginate or filter server-side. Never `SELECT *` a whole catalog into memory.
3. **`reltuples` is an estimate.** Label it as such in the UI ("~4.2M"). Exact counts
   require a scan and are opt-in per table.
4. **Handle permission gaps gracefully.** A role that cannot see a schema gets an empty
   list, not an exception. `has_schema_privilege` filtering is part of each query.

### Caching and invalidation

Metadata is cached per session in an `IMetadataCache` with these invalidation triggers:

- explicit user refresh (F5 on a node)
- any DDL statement executed through Basora (the safety engine already classifies it)
- a background revalidation on a timer for the currently expanded subtree only

Cache entries are keyed by `(SessionId, DbObjectRef, Aspect)`. Never cache across
sessions — a reconnect may land on a different server behind a load balancer.

---

## 4. Query execution and streaming

### Statement splitting

Multi-statement editor text is split by our own lexer (`Basora.Core/Sql/`), which must
correctly handle:

- string literals `'...'` with doubled quotes
- dollar-quoted bodies `$$ ... $$` and `$tag$ ... $tag$` (function definitions)
- line comments `--` and nested block comments
- `E'...'` escape strings, `U&'...'` unicode strings
- semicolons inside any of the above

Splitting on `;` with a regex is wrong and will corrupt user function definitions.

### Streaming contract

```csharp
IAsyncEnumerable<ResultChunk> ExecuteStreamingAsync(QueryRequest request, CancellationToken ct);
```

Implementation shape:

```csharp
await using var cmd = dataSource.CreateCommand(sql);
await using var reader = await cmd.ExecuteReaderAsync(
    CommandBehavior.SequentialAccess, ct);

do
{
    var columns = ReadSchema(reader);
    var buffer = new List<object?[]>(ChunkSize);
    while (await reader.ReadAsync(ct))
    {
        buffer.Add(ReadRow(reader, columns));
        if (buffer.Count == ChunkSize)
        {
            yield return new ResultChunk(...);
            buffer = new List<object?[]>(ChunkSize);
        }
    }
    yield return new ResultChunk(..., IsFinal: true);
}
while (await reader.NextResultAsync(ct));
```

- **Chunk size** starts at 200 rows and adapts: the first chunk is emitted as soon as it
  is available so the grid paints fast, later chunks grow to 2000 to reduce overhead.
- **Row cap** — a hard `MaxRows` from settings (default 50,000 for the editor). On hit,
  stop reading, mark `IsTruncated`, and offer "load more" or "export full result",
  which streams straight to a file without materialising in memory.
- **Memory ceiling** — track approximate bytes buffered; above the ceiling, stop and
  surface the same truncation UI. Never OOM the app because someone typed
  `SELECT * FROM events`.

### Cancellation

Passing a `CancellationToken` to `ExecuteReaderAsync` makes Npgsql send a cancellation
request to the server. Two rules:

1. Cancellation must be **immediate in the UI** — the tab returns to idle at once, even
   if the server takes a moment to acknowledge.
2. After cancelling, the connection may be in an unknown state; the channel discards it
   and takes a fresh one from the pool rather than reusing it.

Also expose **"Terminate"** as an escalation, which issues `pg_cancel_backend(pid)` and
then `pg_terminate_backend(pid)` from the *monitor* channel — a separate connection,
because the query connection is busy.

### Timing

Report three numbers, and label them honestly:

| Metric | Source |
|---|---|
| Total elapsed | Stopwatch around the whole call, includes network |
| Server execution | `EXPLAIN ANALYZE` execution time when plan capture is on |
| Rows fetched | Counted client-side as chunks arrive |

### Notices and RAISE output

Hook `NpgsqlConnection.Notice` and surface `RAISE NOTICE` output in a Messages pane.
Developers debugging PL/pgSQL depend on this and most GUI clients swallow it.

---

## 5. Transactions

```csharp
public interface ITransactionScope : IAsyncDisposable
{
    Guid Id { get; }
    TransactionState State { get; }
    IReadOnlyList<string> ExecutedStatements { get; }
    Task<Result> BeginAsync(IsolationLevel level, CancellationToken ct);
    Task<Result> CommitAsync(CancellationToken ct);
    Task<Result> RollbackAsync(CancellationToken ct);
    Task<Result> SavepointAsync(string name, CancellationToken ct);
    Task<Result> RollbackToAsync(string name, CancellationToken ct);
}
```

- A transaction **pins one physical connection** for its whole life. The channel marks
  it unavailable to other work.
- An open transaction is **always visible** in the status bar with elapsed time and
  statement count, and warns above a configurable age (default 60s) because idle-in-
  transaction blocks vacuum.
- Closing a tab with an open transaction prompts; it never silently commits.
- The data grid's "commit pending changes" wraps in `BEGIN`/`COMMIT` by default. If any
  statement fails, roll back the whole set and report which statement failed.

---

## 6. Identifier quoting and SQL generation

One utility, used everywhere, no exceptions:

```csharp
public static string QuoteIdentifier(string identifier);   // "my table" -> "my table" quoted
public static string QuoteQualified(string? schema, string name);
public static string QuoteLiteral(string value);           // last resort only
```

Rules:

- Quote **always**, not "only when needed". Deciding when to quote is where bugs live.
  `users` becomes `"users"`. It is uglier in generated SQL and it is always correct.
- Embedded `"` is doubled.
- **Never** build a `WHERE` value by string concatenation. Values are parameters. The
  only exception is generated migration DDL shown to the user for review, where defaults
  and check expressions are echoed verbatim from `pg_get_expr`.
- Reject identifiers containing a null byte outright.

---

## 7. Type mapping

| PostgreSQL | .NET (Npgsql) | `PgTypeCategory` | Grid rendering | Editor |
|---|---|---|---|---|
| `bool` | `bool` | Boolean | checkbox glyph | tri-state (true/false/NULL) |
| `int2/4/8`, `numeric`, `float4/8` | `short/int/long/decimal/float/double` | Numeric | right-aligned, thousands sep optional | numeric text box |
| `text`, `varchar`, `char`, `citext` | `string` | Text | left-aligned, single line, truncated | text box, expand to multiline |
| `date`, `time`, `timestamp`, `timestamptz`, `interval` | `DateOnly`/`TimeOnly`/`DateTime`/`DateTimeOffset`/`NpgsqlInterval` | DateTime | ISO-8601, timezone shown for `timestamptz` | date/time picker + raw text |
| `uuid` | `Guid` | Uuid | monospace | text box with validation |
| `json`, `jsonb` | `string` (kept raw) | Json | collapsed one-line preview | JSON editor with validate + format |
| `bytea` | `byte[]` | Binary | `<binary N bytes>` | hex viewer, save-to-file, never inline edit by default |
| `T[]` | `T[]` | Array | `{a,b,c}` | array editor |
| `int4range`, `tstzrange` etc. | `NpgsqlRange<T>` | Range | `[a,b)` | text box |
| enum types | `string` | Enum | plain | dropdown of `enumlabel` values |
| domains | underlying | Domain | as underlying | as underlying, plus constraint hint |
| composite | `object[]` | Composite | `(a,b)` | read-only in MVP 1 |
| `vector` (pgvector) | `float[]` | Vector | `[N dims]` | read-only in MVP 1 |
| unknown/custom | `string` via text output | Unknown | plain text | read-only, with a note |

**Rules:**

- Read `jsonb` as a raw string, never round-trip through a .NET JSON DOM — that would
  reorder keys and destroy the user's formatting.
- `timestamptz` is displayed in a user-selected timezone (setting: server / UTC / local)
  with the choice always visible in the column header tooltip. This is a top source of
  confusion in every database tool.
- Unknown types must **degrade to text, never crash the grid.** Npgsql's
  `GetFieldValue<string>` on an unmapped type is the fallback path.
- `NULL` renders as a dimmed `NULL` badge, visually distinct from an empty string. This
  distinction is non-negotiable — see `07-design-system.md`.

---

## 8. COPY for import and export

`COPY` is dramatically faster than row-by-row `INSERT` and is the only acceptable path
for bulk work.

**Export** — `BeginTextExport` / `BeginRawBinaryCopy`:

```sql
COPY (SELECT ...) TO STDOUT WITH (FORMAT csv, HEADER true, DELIMITER ',', NULL '')
```

Streams straight to the destination file. Memory stays flat regardless of table size.

**Import** — `BeginBinaryImport` for typed data after column mapping and validation:

```csharp
await using var writer = await conn.BeginBinaryImportAsync(
    $"COPY {qualified} ({columnList}) FROM STDIN (FORMAT BINARY)", ct);
```

Import contract:

- Validate and preview the first 100 rows **before** opening the writer.
- Wrap in an explicit transaction so a mid-file failure leaves nothing behind.
- Report progress by rows written, and support cancellation (which rolls back).
- On a type conversion failure, report the **file line number, column name and offending
  value** — not just "22P02 invalid input syntax". This is the difference between a
  usable importer and a frustrating one.

---

## 9. Monitoring queries (MVP 2, listed for planning)

| Purpose | Source |
|---|---|
| Active queries | `pg_stat_activity` filtered to non-idle, excluding our own monitor backend |
| Locks and blocking tree | `pg_locks` joined to `pg_stat_activity`, plus `pg_blocking_pids()` |
| Slow queries | `pg_stat_statements` ordered by `mean_exec_time` / `total_exec_time` |
| Cache hit ratio | `pg_statio_user_tables`, `pg_stat_database` |
| Database and relation sizes | `pg_database_size`, `pg_total_relation_size`, `pg_indexes_size` |
| Bloat estimate | Standard estimation query over `pg_stats` — **label it an estimate** |
| Index usage | `pg_stat_user_indexes` |
| Replication lag | `pg_stat_replication`, `pg_last_wal_receive_lsn()` |
| Vacuum state | `pg_stat_user_tables` vacuum/analyze timestamps and counts |
| Settings | `pg_settings` with `source` and `pending_restart` |

Polling defaults to 5s, is pausable, and stops entirely when the dashboard is not
visible. Never poll a production server on a hidden tab.

---

## 10. Error mapping

Map SQLSTATE to a Basora error code plus an actionable message. Minimum set for MVP 1:

| SQLSTATE | Meaning | Basora message |
|---|---|---|
| `28P01` | Bad password | "Authentication failed for user X." + re-prompt inline |
| `28000` | Invalid authorization | Include `pg_hba.conf` hint |
| `3D000` | Database does not exist | Offer the list of databases that do |
| `42501` | Insufficient privilege | Name the object and show the exact `GRANT` needed |
| `42P01` | Undefined table | Offer near-name matches from the metadata cache |
| `42703` | Undefined column | Same, scoped to the table in the statement |
| `42601` | Syntax error | Map `Position` to an editor caret offset and highlight it |
| `23505` | Unique violation | Name the constraint and the conflicting values |
| `23503` | FK violation | Name both tables and the referencing rows |
| `23502` | Not-null violation | Name the column |
| `40001` | Serialization failure | Offer retry |
| `40P01` | Deadlock | Link to the lock viewer |
| `53300` | Too many connections | Show current vs `max_connections` |
| `57014` | Cancelled by user | Not an error; render as "Cancelled" |
| `57P01` | Admin shutdown | Trigger reconnect flow |
| `22P02` | Invalid text representation | On import, resolve to file line + column |

The `Position` field on `PostgresException` gives a 1-based byte offset into the
statement. Converting it to a caret position in the editor (accounting for the statement
offset within the document and UTF-8 vs UTF-16 indexing) is `T-Q05` and is one of the
highest-value small features in the product.

---

## 11. Testing

Integration tests use `Testcontainers.PostgreSql` with a **matrix of server versions**
(13, 15, 17, latest). Catalog queries are exactly the code that breaks across versions,
so the matrix is not optional.

A shared fixture seeds a schema exercising: partitioned tables, generated columns,
identity columns, enums, domains, composite types, arrays, `jsonb`, ranges, partial
indexes, `INCLUDE` indexes, expression indexes, deferred constraints, self-referencing
FKs, multi-column FKs, views, matviews, and a function with a dollar-quoted body
containing semicolons. If a metadata feature is not in the seed, it is not tested.
