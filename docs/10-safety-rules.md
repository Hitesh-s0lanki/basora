# 10 — Safety Rule Engine

Owner: `Basora.Core/Services/Safety/`. Pure logic, no I/O, fully unit-testable.
Row estimation and lock probing are supplied by `Basora.PostgreSQL` through an
interface, so the engine itself never touches a database.

This is a headline feature, not a guardrail bolted on late. It ships in MVP 1.

---

## 1. Contract

```csharp
public interface ISafetyEngine
{
    Task<SafetyVerdict> EvaluateAsync(SafetyRequest request, CancellationToken ct);
}

public sealed record SafetyRequest
{
    public required string Sql { get; init; }
    public required DeploymentEnvironment Environment { get; init; }
    public required string Database { get; init; }
    public bool SafeModeEnabled { get; init; }
    public bool InsideTransaction { get; init; }
    public ISafetyProbe? Probe { get; init; }   // optional row/size/lock estimation
}

public interface ISafetyProbe
{
    Task<long?> EstimateAffectedRowsAsync(string sql, CancellationToken ct);
    Task<TableFacts?> GetTableFactsAsync(DbObjectRef table, CancellationToken ct);
}

public sealed record TableFacts(
    long EstimatedRows, long TotalSizeBytes, int IndexCount,
    bool HasDependentViews, bool IsPartitioned, bool IsReplicated);
```

Evaluation is always **best-effort and fail-safe**: if the probe fails or times out
(budget: 500 ms), the engine proceeds with the static findings and *escalates* rather
than de-escalates. Not knowing how many rows a `DELETE` touches is a reason for more
caution, not less.

---

## 2. Pipeline

```text
SQL text
   |
   1. Lex + split into statements          (Basora.Core/Sql lexer)
   |
   2. Classify each statement              -> StatementKind + subject objects
   |
   3. Apply static rules                   -> SafetyFinding[]
   |
   4. Probe (bounded, optional)            -> row estimates, table facts, lock level
   |
   5. Score                                -> RiskLevel per statement, max across batch
   |
   6. Apply environment policy             -> ConfirmationLevel
   |
   7. Build verdict                        -> phrase, remediations, safer alternatives
```

Classification uses the shallow parser, not regex. `DELETE FROM users` inside a string
literal or a comment is not a delete, and `-- DROP TABLE x` must not trigger anything.

---

## 3. Rule catalogue

Each rule has a stable code so the UI, tests and settings can reference it.

### Data modification

| Code | Condition | Base severity |
|---|---|---|
| `unbounded.delete` | `DELETE` with no `WHERE` | Critical |
| `unbounded.update` | `UPDATE` with no `WHERE` | Critical |
| `always.true.predicate` | `WHERE 1=1`, `WHERE true`, or a predicate over no column | High |
| `truncate` | `TRUNCATE` | Critical |
| `truncate.cascade` | `TRUNCATE ... CASCADE` | Critical |
| `large.delete` | Estimated affected rows above threshold (default 10,000) | High |
| `large.update` | Same, for `UPDATE` | High |
| `delete.no.transaction` | Large delete outside an explicit transaction | Medium |
| `update.primary.key` | `UPDATE` targets a primary-key column | Medium |
| `insert.no.columns` | `INSERT` without a column list | Low |

### Schema modification

| Code | Condition | Base severity |
|---|---|---|
| `drop.table` | `DROP TABLE` | Critical |
| `drop.column` | `ALTER TABLE ... DROP COLUMN` | Critical |
| `drop.cascade` | Any `DROP ... CASCADE` | Critical |
| `drop.database` | `DROP DATABASE` | Critical |
| `drop.schema` | `DROP SCHEMA` | Critical |
| `drop.index.used` | Dropping an index with a non-zero scan count | High |
| `alter.type.rewrite` | Type change that rewrites the table | High |
| `add.notnull.column` | `ADD COLUMN ... NOT NULL` with a volatile default | High |
| `add.column.default` | `ADD COLUMN ... DEFAULT` on PG < 11 (rewrites) | High |
| `create.index.blocking` | `CREATE INDEX` without `CONCURRENTLY` on a large table | High |
| `add.fk.validating` | Adding a validated FK (takes `SHARE ROW EXCLUSIVE`) | Medium |
| `rename.object` | `ALTER ... RENAME` — breaks dependent application code | Medium |
| `drop.dependent` | Object has dependent views/functions | High |
| `set.notnull` | `SET NOT NULL` requires a full scan | Medium |

### Access control

| Code | Condition | Base severity |
|---|---|---|
| `grant.superuser` | Granting superuser or a high-privilege role | Critical |
| `revoke.public` | `REVOKE` from `PUBLIC` | High |
| `alter.role.password` | Password change | Medium |
| `drop.role` | Dropping a role that owns objects | High |
| `disable.rls` | `ALTER TABLE ... DISABLE ROW LEVEL SECURITY` | Critical |

### Operational

| Code | Condition | Base severity |
|---|---|---|
| `vacuum.full` | `VACUUM FULL` — takes `ACCESS EXCLUSIVE`, rewrites the table | Critical |
| `reindex.blocking` | `REINDEX` without `CONCURRENTLY` | High |
| `terminate.backend` | `pg_terminate_backend` | High |
| `lock.table` | Explicit `LOCK TABLE` | Medium |
| `set.global.config` | `ALTER SYSTEM SET` | High |
| `long.transaction` | Transaction open longer than the threshold | Medium |

### Query hygiene (informational, never blocking)

| Code | Condition | Severity |
|---|---|---|
| `select.star.large` | `SELECT *` on a table above the row threshold | Info |
| `no.limit` | Unbounded `SELECT` in the editor | Info |
| `cross.join` | Join with no join condition | Warning |

---

## 4. Lock level table

DDL is scored primarily by the lock it takes, because that is what determines whether
production traffic stalls. The engine carries this table and shows it in the UI.

| Operation | Lock | Blocks reads | Blocks writes |
|---|---|---|---|
| `SELECT` | ACCESS SHARE | no | no |
| `INSERT` / `UPDATE` / `DELETE` | ROW EXCLUSIVE | no | no |
| `CREATE INDEX` | SHARE | no | **yes** |
| `CREATE INDEX CONCURRENTLY` | SHARE UPDATE EXCLUSIVE | no | no |
| `ALTER TABLE ADD COLUMN` (nullable, no default / constant default on PG 11+) | ACCESS EXCLUSIVE (brief) | yes (brief) | yes (brief) |
| `ALTER TABLE ADD COLUMN` with volatile default | ACCESS EXCLUSIVE + rewrite | **yes, long** | **yes, long** |
| `ALTER TABLE ALTER TYPE` (rewriting) | ACCESS EXCLUSIVE + rewrite | **yes, long** | **yes, long** |
| `ALTER TABLE SET NOT NULL` | ACCESS EXCLUSIVE + full scan | yes | yes |
| `ADD CONSTRAINT ... NOT VALID` | ACCESS EXCLUSIVE (brief) | brief | brief |
| `VALIDATE CONSTRAINT` | SHARE UPDATE EXCLUSIVE | no | no |
| `DROP TABLE` / `TRUNCATE` | ACCESS EXCLUSIVE | yes | yes |
| `VACUUM` | SHARE UPDATE EXCLUSIVE | no | no |
| `VACUUM FULL` | ACCESS EXCLUSIVE + rewrite | **yes, long** | **yes, long** |
| `REINDEX` | ACCESS EXCLUSIVE | yes | yes |
| `REINDEX CONCURRENTLY` | SHARE UPDATE EXCLUSIVE | no | no |

**The brief-lock trap.** An `ACCESS EXCLUSIVE` lock that is *held* briefly can still
stall a busy table for minutes, because it must first *wait* for existing queries and
meanwhile queues every new one behind it. The UI must say this, and recommend
`lock_timeout` + retry for production DDL. This is the single most valuable piece of
advice the safety analyzer gives.

---

## 5. Scoring

```text
statementRisk = max(severity of its findings), then adjusted:

  +1 level  if Environment == Production
  +1 level  if estimated affected rows > 100,000
  +1 level  if the target table is > 10 GB
  +1 level  if the operation takes ACCESS EXCLUSIVE and the table is > 1 GB
  +1 level  if the probe failed (unknown blast radius)

  -1 level  if inside an explicit, still-open transaction (recoverable)
  -1 level  if Environment == Local

batchRisk = max(statementRisk) across the batch
```

Levels clamp to `[None, Critical]`.

---

## 6. Environment policy matrix

| Risk | Local | Development | Staging | Production |
|---|---|---|---|---|
| None | run | run | run | run |
| Low | run | run | run | Confirm |
| Medium | run | run | Confirm | Confirm |
| High | run | Confirm | Confirm | **TypeToConfirm** |
| Critical | Confirm | Confirm | **TypeToConfirm** | **TypeToConfirm** |

Safe mode (per connection, on by default for Production) raises every cell by one level
and additionally:

- forces an automatic `LIMIT` on unbounded `SELECT` in the editor (default 1,000, shown
  in the UI, never silent)
- sets the session read-only until the user explicitly disarms it for that session
- requires an explicit transaction for any data modification

**Read-only mode** is stronger: the server session is `READ ONLY`, so even a bug in our
classifier cannot mutate anything. Available on any profile, default for Production
until the user opts out per session.

---

## 7. Type-to-confirm phrases

The phrase must name the actual object, so muscle memory cannot carry the user through.

| Operation | Phrase |
|---|---|
| `DROP TABLE users` | `DROP users` |
| `TRUNCATE orders` | `TRUNCATE orders` |
| `DELETE FROM users` (no WHERE) | `DELETE users` |
| `UPDATE users` (no WHERE) | `UPDATE users` |
| `DROP DATABASE production` | `DROP DATABASE production` |
| `VACUUM FULL events` | `VACUUM FULL events` |

Rules: match is case-sensitive and exact after trimming; the phrase is **not**
copy-pasteable from the dialog (it is rendered as non-selectable text); the confirm
button stays disabled until it matches.

---

## 8. Row estimation

For `UPDATE`/`DELETE`, transform to the equivalent `SELECT` and run
`EXPLAIN` (**without** `ANALYZE` — it must not execute anything):

```sql
EXPLAIN (FORMAT JSON) SELECT 1 FROM users WHERE <predicate>;
```

Take the root node's `Plan Rows`. Present it as an estimate — `~18,421,932` — never as a
fact. If the planner estimate is unavailable, say "unknown" and escalate risk.

`EXPLAIN` on a `DELETE`/`UPDATE` directly is also valid and avoids transformation bugs;
prefer that where the statement parses cleanly, and fall back to the `SELECT` rewrite.
Both paths are safe as long as `ANALYZE` is never added — this must be enforced by a
test, since an accidental `EXPLAIN ANALYZE DELETE` would execute the delete.

---

## 9. Safer alternatives

The engine does not only warn; it proposes. This is what makes it a feature rather than
a nag.

| Detected | Offered alternative |
|---|---|
| `CREATE INDEX` on a large table | `CREATE INDEX CONCURRENTLY` |
| `REINDEX` | `REINDEX CONCURRENTLY` |
| `ADD COLUMN NOT NULL DEFAULT <volatile>` | Add nullable, backfill in batches, then `SET NOT NULL` |
| `ADD FOREIGN KEY` | `ADD ... NOT VALID` then `VALIDATE CONSTRAINT` |
| `SET NOT NULL` | Add a `CHECK ... NOT VALID`, validate, then `SET NOT NULL` (cheap in PG 12+) |
| Large `DELETE` | Batched delete loop with `LIMIT` and a key cursor |
| `ALTER TYPE` rewrite | New column, backfill, swap |
| Any production DDL | Wrap with `SET lock_timeout = '3s'` and retry guidance |
| Unbounded `SELECT` | Add `LIMIT` |

Each alternative is presented as ready-to-run SQL, and choosing it replaces the editor
content rather than executing anything.

---

## 10. Bypass, and honesty about it

`Continue anyway` always exists. A tool that cannot be overridden gets uninstalled, and
a locked-out engineer during an incident is a worse outcome than a risky statement.

But every bypass is:
- **audited** with the risk level that was assessed and the findings that were shown,
- **never the default focus** and never triggered by `Enter`,
- **rate-limit free but visible** — the status bar shows a count of bypasses this
  session, which is a gentle social nudge rather than an obstacle.

There is no global "disable all safety checks" setting. Safe mode can be turned off
per connection, which is a deliberate, visible, per-profile decision.

---

## 11. Testing

The safety engine gets the most thorough test suite in the codebase, because a false
negative here is the worst bug the product can have.

- A **corpus** of several hundred statements, each labelled with expected kind, findings
  and risk. Includes every rule in section 3, plus adversarial cases:
  keywords in strings, dollar-quoted function bodies containing `DROP TABLE`, comments
  containing statements, `DELETE` inside a CTE, `UPDATE ... FROM`, `WITH ... DELETE`,
  multi-statement batches mixing risk levels, and schema-qualified vs unqualified names.
- A test asserting **no code path can produce `EXPLAIN ANALYZE`** for a mutating
  statement during estimation.
- Property test: adding a `WHERE` clause never *increases* risk; changing environment
  from Local to Production never *decreases* the confirmation level.
- Integration tests against a real server asserting the lock-level table is accurate for
  each supported major version — because that table is a factual claim about
  PostgreSQL, and factual claims rot.
