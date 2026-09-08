# 13 — Glossary

Shared vocabulary. Where a word could mean two things, this file decides which one we
mean, so specs and task cards do not drift.

---

## Basora concepts

| Term | Meaning |
|---|---|
| **Connection Profile** | The saved, serialisable definition of how to reach a database. Contains no secret material — only a `SecretRef`. |
| **Session** | A live connection to a server, built from a profile. Owns the data source, the capability probe result, and the three channels. |
| **Channel** | A logically separate connection lane within a session: `Query`, `Metadata`, `Monitor`. Keeps tree loading and monitoring independent of user queries. |
| **Environment** | The `Local` / `Development` / `Staging` / `Production` tag on a profile. Drives colour and the safety ladder. |
| **Workspace** | The persisted arrangement of connection tabs, document tabs and their state. Restored on launch. |
| **Document** | A tab inside a connection: SQL editor, table browser, diagram, dashboard. |
| **Pending Change** | An uncommitted grid edit (insert, update, delete). Exists only client-side until committed. |
| **Change Set** | The full collection of pending changes for one table document, committed as one transaction. |
| **Safety Ladder** | The escalation from `None` to `Confirm` to `TypeToConfirm` to `Blocked`, derived from risk and environment. |
| **Safe Mode** | Per-connection setting that raises every safety level, auto-limits unbounded SELECTs, and sets the session read-only. Default on for Production. |
| **Type-to-Confirm** | A confirmation that requires typing an exact object-naming phrase. Not copy-pasteable. |
| **Capability** | A feature-availability fact probed once per session (server version, extension present, role privilege). Features gate on capabilities, never on version arithmetic. |
| **Finding** | A single diagnosis produced by the safety engine or an analyzer: code, severity, message, remediation. |
| **Verdict** | The aggregate outcome of evaluating a statement or batch: risk level, required confirmation, findings. |
| **Probe** | A bounded, best-effort database lookup used to enrich a verdict (row estimate, table size). Failure escalates risk, never reduces it. |
| **Descriptor** | An immutable metadata record for a database object (`TableDescriptor`, `IndexDescriptor`). |
| **Open Anything** | `Ctrl+P` — fuzzy search across every reachable database object and saved query. |
| **Command Palette** | `Ctrl+K` — fuzzy search across every registered command. |
| **Wave** | A group of tasks in the plan that can all start at the same time. See `tasks/00-task-board.md`. |
| **Ownership** | The exclusive set of files a task may modify. Two tasks with disjoint ownership are safe to run in parallel. |
| **TestKit** | `Basora.TestKit` — the shared project of in-memory fakes for every `Core` interface. The thing that makes parallel work possible. |

---

## PostgreSQL terms used precisely

| Term | Meaning as we use it |
|---|---|
| **Relation** | Any table-like object: table, partitioned table, foreign table, view, materialized view, index, sequence. `pg_class.relkind` distinguishes them. |
| **OID** | Object identifier. Stable within a database but **not** across databases or dumps. Never persist an OID in a workspace file. |
| **reltuples** | The planner's estimated row count. An estimate, and `-1` on PG 14+ when never analyzed. Always rendered with a `~`. |
| **Bloat** | Space occupied by dead tuples not yet reclaimed. Only ever *estimated* by us; we say so. |
| **Dead tuple** | A row version made obsolete by an update or delete, awaiting vacuum. |
| **Autovacuum** | Background process reclaiming dead tuples and refreshing statistics. |
| **TOAST** | Out-of-line storage for oversized values. Counted separately in the storage analyzer. |
| **ctid** | Physical row location. Usable as a last-resort row identity but **not stable** across `VACUUM FULL` or updates — always warn when used. |
| **MVCC** | Multi-version concurrency control. Why readers do not block writers, and why bloat exists. |
| **ACCESS EXCLUSIVE** | The strongest table lock. Blocks everything, including reads. The lock level that makes DDL dangerous in production. |
| **SHARE UPDATE EXCLUSIVE** | The lock taken by `CONCURRENTLY` operations and `VACUUM`. Does not block reads or writes. |
| **Lock queue** | New requests queue behind a waiting stronger lock, which is why a "brief" `ACCESS EXCLUSIVE` can stall a busy table for minutes. |
| **Sequential Scan** | Reading every page of a relation. Correct and fast for small tables; the usual culprit for slow queries on large ones. |
| **Index Only Scan** | A scan satisfied entirely from the index, requiring a good visibility map. |
| **Planning time / Execution time** | Reported separately by `EXPLAIN ANALYZE`. We show both; users routinely conflate them. |
| **Buffers** | Shared/local/temp block counts in an explain plan. `shared read` means it went to disk (or OS cache); `shared hit` means it was in `shared_buffers`. |
| **pg_stat_statements** | Extension aggregating query statistics. The basis of slow-query detection. Must be both installed *and* readable by the role. |
| **SQLSTATE** | The five-character PostgreSQL error code. Our error mapping keys off it. |
| **search_path** | The schema resolution order for unqualified names. We never rely on the user's; our own queries qualify explicitly. |
| **NOT VALID** | A constraint added without scanning existing rows. Paired with a later `VALIDATE CONSTRAINT`, it is the safe way to add constraints to a large table. |
| **CONCURRENTLY** | Index build/rebuild that avoids blocking writes, at the cost of being slower, non-transactional, and able to leave an `INVALID` index behind on failure. |
| **Idle in transaction** | A session holding an open transaction while doing nothing. Blocks vacuum and can hold locks indefinitely. A first-class finding in the activity view. |
| **Dollar quoting** | `$$ ... $$` or `$tag$ ... $tag$`. Why SQL cannot be split on semicolons with a regex. |

---

## Architecture terms

| Term | Meaning |
|---|---|
| **Core** | `Basora.Core`. Contracts and domain types. No framework dependencies, references nothing. |
| **Layer rule** | A build-enforced constraint on which project may reference which. See `02-architecture.md` section 3. |
| **Composition root** | `Basora.App/Composition/`. The single place where interfaces are bound to implementations. |
| **Semantic token** | A named design value with meaning (`status.danger`), as opposed to a primitive (`red.600`). UI consumes only semantic and component tokens. |
| **Required state** | One of the seven states every data surface must implement: loading, empty, unconfigured, permission-denied, error, truncated, stale. |
| **Golden file** | A checked-in snapshot of generated output (SQL, plan tree) that a test compares against. Changes are always deliberate. |
