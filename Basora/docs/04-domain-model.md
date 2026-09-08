# 04 — Domain Model

Everything here lives in **`Basora.Core`** and has no dependency on Avalonia or Npgsql.
These are the types every other project agrees on. Getting them landed early (Wave 0)
is what unblocks parallel work.

Conventions: `record` for data, `readonly record struct` for small value types, `enum`
for closed sets, `init`-only properties, non-nullable by default. Collections are
`IReadOnlyList<T>`.

---

## 1. Connections

```csharp
public sealed record ConnectionProfile
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }          // "Production"
    public required string Host { get; init; }
    public int Port { get; init; } = 5432;
    public required string Database { get; init; }
    public required string Username { get; init; }
    public string? SecretRef { get; init; }             // key into ISecretStore, NEVER the password
    public SslMode SslMode { get; init; } = SslMode.Prefer;
    public string? RootCertificatePath { get; init; }
    public string? ClientCertificatePath { get; init; }
    public SshTunnelConfig? SshTunnel { get; init; }
    public DeploymentEnvironment Environment { get; init; } = DeploymentEnvironment.Local;
    public string? ColorHex { get; init; }              // null => derived from Environment
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan CommandTimeout { get; init; } = TimeSpan.FromSeconds(120);
    public string ApplicationName { get; init; } = "Basora";
    public bool ReadOnlyByDefault { get; init; }
    public string? DefaultSchema { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public DateTimeOffset? LastConnectedAt { get; init; }
    public Guid? FolderId { get; init; }
}

public enum DeploymentEnvironment { Local, Development, Staging, Production }

public enum SslMode { Disable, Allow, Prefer, Require, VerifyCa, VerifyFull }

public sealed record SshTunnelConfig
{
    public required string Host { get; init; }
    public int Port { get; init; } = 22;
    public required string Username { get; init; }
    public SshAuthMethod AuthMethod { get; init; }
    public string? PrivateKeyPath { get; init; }
    public string? SecretRef { get; init; }             // password or key passphrase
    public string? RemoteHostOverride { get; init; }    // defaults to profile Host
    public int? RemotePortOverride { get; init; }
}

public enum SshAuthMethod { Password, PrivateKey, Agent }
```

### Environment colour mapping

| Environment | Colour token | Default hex (light / dark) |
|---|---|---|
| Local | `env.local` | `#16A34A` / `#22C55E` |
| Development | `env.dev` | `#2563EB` / `#3B82F6` |
| Staging | `env.staging` | `#D97706` / `#F59E0B` |
| Production | `env.production` | `#DC2626` / `#EF4444` |

`ColorHex` overrides the default. The colour appears in the connection card, the tab
strip, the window title bar accent and the status bar — see `07-design-system.md`.

### Connection URI parsing

`postgresql://user:password@host:5432/database?sslmode=require` must parse into a
`ConnectionProfile` draft, with the password extracted to the secret store immediately
and never retained in the profile. Also accept the `postgres://` scheme and libpq
key/value strings (`host=... port=... dbname=...`). Owner: `T-C02`.

---

## 2. Session

```csharp
public interface IDatabaseSession : IAsyncDisposable
{
    Guid SessionId { get; }
    ConnectionProfile Profile { get; }
    ServerCapabilities Capabilities { get; }
    SessionState State { get; }
    event EventHandler<SessionStateChangedEventArgs>? StateChanged;

    Task<Result> OpenAsync(CancellationToken ct);
    Task<Result> CloseAsync(CancellationToken ct);
    IQueryChannel Queries { get; }
    IMetadataChannel Metadata { get; }
    IMonitorChannel Monitor { get; }
}

public enum SessionState { Disconnected, Connecting, Connected, Reconnecting, Failed }

public sealed record ServerCapabilities
{
    public required Version ServerVersion { get; init; }
    public required string ServerVersionString { get; init; }
    public bool IsSuperuser { get; init; }
    public bool CanReadPgStatStatements { get; init; }
    public bool HasPgStatStatements { get; init; }
    public bool HasPgBuffercache { get; init; }
    public bool HasPgStatKcache { get; init; }
    public bool SupportsConcurrentRefresh { get; init; }   // >= 9.4
    public bool SupportsGeneratedColumns { get; init; }    // >= 12
    public bool SupportsExplainSettings { get; init; }     // >= 12
    public bool SupportsMergeStatement { get; init; }      // >= 15
    public IReadOnlyList<InstalledExtension> Extensions { get; init; } = [];
    public IReadOnlyList<string> RoleMemberships { get; init; } = [];
}
```

**Rule:** features gate on `Capabilities`, never on a version comparison scattered
through the codebase. If a capability is missing, the feature shows a specific,
actionable empty state.

---

## 3. Object tree

```csharp
public enum DbObjectKind
{
    Database, Schema, Table, PartitionedTable, ForeignTable, View, MaterializedView,
    Column, Index, Constraint, Trigger, Function, Procedure, Aggregate, Sequence,
    Type, Domain, Enum, Extension, ForeignDataWrapper, Server, Role, Policy, Publication,
    Subscription, Tablespace
}

public sealed record DbObjectRef(
    DbObjectKind Kind,
    string? Schema,
    string Name,
    uint? Oid = null,
    string? Parent = null)
{
    public string QualifiedName => Schema is null ? Name : $"{Schema}.{Name}";
}

public sealed record ObjectNode
{
    public required DbObjectRef Ref { get; init; }
    public required string DisplayName { get; init; }
    public string? Comment { get; init; }
    public bool HasChildren { get; init; }
    public long? EstimatedRows { get; init; }
    public long? SizeBytes { get; init; }
    public IReadOnlyList<string> Badges { get; init; } = [];  // "PK", "FK", "unlogged"
}
```

---

## 4. Table metadata

```csharp
public sealed record TableDescriptor
{
    public required DbObjectRef Ref { get; init; }
    public required IReadOnlyList<ColumnDescriptor> Columns { get; init; }
    public IReadOnlyList<IndexDescriptor> Indexes { get; init; } = [];
    public IReadOnlyList<ConstraintDescriptor> Constraints { get; init; } = [];
    public IReadOnlyList<ForeignKeyDescriptor> OutgoingForeignKeys { get; init; } = [];
    public IReadOnlyList<ForeignKeyDescriptor> IncomingForeignKeys { get; init; } = [];
    public IReadOnlyList<TriggerDescriptor> Triggers { get; init; } = [];
    public IReadOnlyList<string> PrimaryKeyColumns { get; init; } = [];
    public string? Owner { get; init; }
    public string? Tablespace { get; init; }
    public bool IsPartitioned { get; init; }
    public string? PartitionStrategy { get; init; }
    public bool IsUnlogged { get; init; }
    public string? Comment { get; init; }
    public TableStatistics? Statistics { get; init; }
}

public sealed record ColumnDescriptor
{
    public required string Name { get; init; }
    public required int OrdinalPosition { get; init; }
    public required PgType Type { get; init; }
    public bool IsNullable { get; init; }
    public string? DefaultExpression { get; init; }
    public IdentityKind Identity { get; init; }
    public string? GeneratedExpression { get; init; }
    public string? Collation { get; init; }
    public string? Comment { get; init; }
    public bool IsPrimaryKey { get; init; }
    public bool IsUnique { get; init; }
    public ForeignKeyDescriptor? References { get; init; }
}

public enum IdentityKind { None, Always, ByDefault, Serial }

public sealed record PgType
{
    public required string Name { get; init; }           // "varchar", "int4", "_text"
    public required string DisplayName { get; init; }    // "character varying(255)"
    public required PgTypeCategory Category { get; init; }
    public int? Length { get; init; }
    public int? Precision { get; init; }
    public int? Scale { get; init; }
    public bool IsArray { get; init; }
    public string? ElementType { get; init; }
    public IReadOnlyList<string>? EnumValues { get; init; }
    public uint Oid { get; init; }
}

public enum PgTypeCategory
{
    Numeric, Text, Boolean, DateTime, Uuid, Json, Binary, Array, Range, Composite,
    Enum, Domain, Geometric, Network, Vector, Unknown
}
```

`PgTypeCategory` drives grid cell rendering, editor selection, filter operator lists and
export formatting. Adding a category is a coordinated change — see the mapping table in
`05-postgresql-data-layer.md`.

```csharp
public sealed record IndexDescriptor
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> Columns { get; init; }
    public IReadOnlyList<string> IncludedColumns { get; init; } = [];
    public required string Method { get; init; }        // btree, gin, gist, brin, hash
    public bool IsUnique { get; init; }
    public bool IsPrimary { get; init; }
    public bool IsExclusion { get; init; }
    public bool IsValid { get; init; }
    public string? Predicate { get; init; }             // partial index WHERE
    public string? Definition { get; init; }            // full CREATE INDEX text
    public long? SizeBytes { get; init; }
    public long? ScanCount { get; init; }
    public long? TuplesRead { get; init; }
    public long? TuplesFetched { get; init; }
}

public sealed record ConstraintDescriptor(
    string Name, ConstraintKind Kind, IReadOnlyList<string> Columns,
    string? Expression, string? Definition, bool IsDeferrable, bool IsValidated);

public enum ConstraintKind { PrimaryKey, ForeignKey, Unique, Check, Exclusion, NotNull }

public sealed record ForeignKeyDescriptor
{
    public required string Name { get; init; }
    public required DbObjectRef SourceTable { get; init; }
    public required IReadOnlyList<string> SourceColumns { get; init; }
    public required DbObjectRef TargetTable { get; init; }
    public required IReadOnlyList<string> TargetColumns { get; init; }
    public ReferentialAction OnUpdate { get; init; }
    public ReferentialAction OnDelete { get; init; }
    public Cardinality Cardinality { get; init; }
}

public enum ReferentialAction { NoAction, Restrict, Cascade, SetNull, SetDefault }
public enum Cardinality { OneToOne, OneToMany, ManyToOne, ManyToMany }
```

---

## 5. Query execution

```csharp
public sealed record QueryRequest
{
    public required string Sql { get; init; }
    public IReadOnlyList<QueryParameter> Parameters { get; init; } = [];
    public int? MaxRows { get; init; }                  // safe-mode auto-limit
    public bool CaptureExplain { get; init; }
    public TimeSpan? Timeout { get; init; }
    public bool InTransaction { get; init; }
    public string? Origin { get; init; }                // "editor", "grid", "palette"
}

public sealed record QueryResultSet
{
    public required int Index { get; init; }            // for multi-statement batches
    public required string Sql { get; init; }
    public required IReadOnlyList<ResultColumn> Columns { get; init; }
    public required IReadOnlyList<object?[]> Rows { get; init; }
    public bool IsTruncated { get; init; }
    public long? AffectedRows { get; init; }
    public TimeSpan Duration { get; init; }
    public TimeSpan? ServerExecutionTime { get; init; }
    public ExplainPlan? Plan { get; init; }
    public IReadOnlyList<PostgresNotice> Notices { get; init; } = [];
    public DbObjectRef? UpdatableSource { get; init; }  // set => grid may edit in place
}

public sealed record ResultColumn(
    string Name, PgType Type, string? SourceTable, string? SourceColumn, bool IsKey);

public sealed record QueryParameter(string Name, PgType Type, object? Value);

public sealed record PostgresNotice(string Severity, string Message, string? Detail, string? Hint);
```

Streaming is expressed as `IAsyncEnumerable<ResultChunk>`:

```csharp
public sealed record ResultChunk(
    int ResultSetIndex,
    IReadOnlyList<ResultColumn>? Columns,   // non-null only on the first chunk
    IReadOnlyList<object?[]> Rows,
    long TotalRowsSoFar,
    bool IsFinal);
```

---

## 6. Filters

An AST, not a string. The AST is the single source for both the UI builder and the
generated `WHERE` clause, which is what makes the two provably consistent.

```csharp
public abstract record FilterNode;

public sealed record FilterGroup(
    LogicalOperator Operator,
    IReadOnlyList<FilterNode> Children) : FilterNode;

public sealed record FilterCondition(
    string Column,
    FilterOperator Operator,
    IReadOnlyList<object?> Values,
    bool CaseInsensitive = false) : FilterNode;

public enum LogicalOperator { And, Or }

public enum FilterOperator
{
    Equals, NotEquals, GreaterThan, LessThan, GreaterOrEqual, LessOrEqual,
    Like, NotLike, ILike, NotILike, In, NotIn, IsNull, IsNotNull, Between, NotBetween,
    Contains, StartsWith, EndsWith,                 // sugar over LIKE
    JsonContains, JsonHasKey, ArrayContains         // jsonb / array specific
}
```

Operator availability per `PgTypeCategory` is a table owned by `T-D04`. Values are
always bound as parameters — the filter builder never interpolates user text into SQL.

---

## 7. Pending changes

```csharp
public sealed record ChangeSet
{
    public required Guid Id { get; init; }
    public required DbObjectRef Table { get; init; }
    public required IReadOnlyList<PendingChange> Changes { get; init; }
    public bool WrapInTransaction { get; init; } = true;
}

public abstract record PendingChange
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed record RowInsert(
    IReadOnlyDictionary<string, object?> Values) : PendingChange;

public sealed record RowUpdate(
    IReadOnlyDictionary<string, object?> KeyValues,
    IReadOnlyDictionary<string, CellEdit> Changes) : PendingChange;

public sealed record RowDelete(
    IReadOnlyDictionary<string, object?> KeyValues) : PendingChange;

public sealed record CellEdit(object? OldValue, object? NewValue);
```

**Row identity rule.** A row is editable only if the result set has a usable key:
primary key, else a unique non-null index, else `ctid` with an explicit warning that
`ctid` is not stable across `VACUUM FULL`. If none applies, the grid is read-only and
says why. Owner: `T-D05`.

**Optimistic concurrency.** `RowUpdate`/`RowDelete` generate a `WHERE` clause over the
key plus the original values of edited columns, so a concurrent change surfaces as
"0 rows affected" rather than a silent overwrite.

---

## 8. History and favorites

```csharp
public sealed record QueryHistoryEntry
{
    public required Guid Id { get; init; }
    public required string Sql { get; init; }
    public required DateTimeOffset ExecutedAt { get; init; }
    public required Guid ConnectionId { get; init; }
    public required string ConnectionName { get; init; }
    public required string Database { get; init; }
    public DeploymentEnvironment Environment { get; init; }
    public TimeSpan? Duration { get; init; }
    public long? RowCount { get; init; }
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public string? SqlState { get; init; }
    public string SqlHash { get; init; } = "";   // dedupe identical repeats
}

public sealed record SavedQuery
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Sql { get; init; }
    public Guid? FolderId { get; init; }
    public Guid? ConnectionId { get; init; }      // null = available everywhere
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string? Description { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ModifiedAt { get; init; }
}
```

History is stored in a local SQLite file or an append-only JSONL log
(decision: `T-H01`), capped by count and age, with a per-connection opt-out and a
"never record statements matching X" redaction list.

---

## 9. EXPLAIN (MVP 2, modelled now)

```csharp
public sealed record ExplainPlan
{
    public required ExplainNode Root { get; init; }
    public double? PlanningTimeMs { get; init; }
    public double? ExecutionTimeMs { get; init; }
    public bool IsAnalyzed { get; init; }
    public string RawJson { get; init; } = "";
    public IReadOnlyList<PlanFinding> Findings { get; init; } = [];
}

public sealed record ExplainNode
{
    public required string NodeType { get; init; }       // "Seq Scan", "Nested Loop"
    public string? RelationName { get; init; }
    public string? IndexName { get; init; }
    public string? Alias { get; init; }
    public double StartupCost { get; init; }
    public double TotalCost { get; init; }
    public double PlanRows { get; init; }
    public double? ActualRows { get; init; }
    public double? ActualTotalTimeMs { get; init; }
    public int? ActualLoops { get; init; }
    public long? SharedHitBlocks { get; init; }
    public long? SharedReadBlocks { get; init; }
    public string? Filter { get; init; }
    public long? RowsRemovedByFilter { get; init; }
    public IReadOnlyList<ExplainNode> Children { get; init; } = [];
    public double SelfTimeMs { get; init; }              // computed, for heat mapping
}

public sealed record PlanFinding(
    FindingSeverity Severity, string Title, string Detail,
    string? SuggestedSql, DbObjectRef? Subject);

public enum FindingSeverity { Info, Warning, Critical }
```

---

## 10. Safety

```csharp
public sealed record SafetyVerdict
{
    public required RiskLevel Risk { get; init; }
    public required ConfirmationLevel RequiredConfirmation { get; init; }
    public required IReadOnlyList<SafetyFinding> Findings { get; init; }
    public string? TypeToConfirmPhrase { get; init; }
    public long? EstimatedAffectedRows { get; init; }
    public string? AcquiredLockLevel { get; init; }
}

public enum RiskLevel { None, Low, Medium, High, Critical }

public enum ConfirmationLevel { None, Confirm, TypeToConfirm, Blocked }

public sealed record SafetyFinding(
    string Code, FindingSeverity Severity, string Message, string? Remediation);

public enum StatementKind
{
    Select, Insert, Update, Delete, Merge, Truncate, Copy,
    CreateObject, AlterObject, DropObject, Grant, Revoke,
    Vacuum, Analyze, Reindex, Transaction, Set, Explain, Call, Unknown
}
```

Full rule catalogue in `10-safety-rules.md`.

---

## 11. Result type

```csharp
public readonly record struct Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }
    public static Result Ok();
    public static Result Fail(Error error);
}

public readonly record struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
    public static Result<T> Ok(T value);
    public static Result<T> Fail(Error error);
}

public sealed record Error(
    string Code,
    string Message,
    string? Detail = null,
    string? Hint = null,
    string? SqlState = null,
    Exception? Exception = null);
```

`Error.Code` is a stable Basora code (`basora.connection.refused`,
`basora.query.cancelled`, `basora.privilege.denied`) so the UI can key off it for
localisation and specific empty states. `SqlState` carries the raw PostgreSQL code when
one exists.

---

## 12. Ownership

| Section | Task | Files |
|---|---|---|
| 1, 2 | `T-F04` | `Core/Models/Connections/*` |
| 3, 4 | `T-F05` | `Core/Models/Metadata/*` |
| 5 | `T-F06` | `Core/Models/Query/*` |
| 6 | `T-F06` | `Core/Models/Filters/*` |
| 7 | `T-F06` | `Core/Models/Changes/*` |
| 8 | `T-H01` | `Core/Models/History/*` |
| 9 | `T-F07` | `Core/Models/Explain/*` |
| 10 | `T-F07` | `Core/Models/Safety/*` |
| 11 | `T-F03` | `Core/Results/*` |

These are disjoint folders on purpose: all of `T-F03` through `T-F07` run in parallel.
