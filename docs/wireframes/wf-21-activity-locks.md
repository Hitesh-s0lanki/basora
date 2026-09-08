# wf-21 — Activity and Lock Analyzer

> Task: `T-N03`. MVP 2. Document with two tabs.

---

## 1. Purpose

See what the database is doing right now, and untangle what is blocking what. The one job
it must do well: **show the blocking chain as a chain** — a flat list of locks is a
puzzle; a tree that names the root blocker is an answer.

---

## 2. Entry points

- "Investigate" on a blocking or activity finding in [wf-20](wf-20-health-dashboard.md).
- Database menu > Activity.
- Command palette: "Show active queries" / "Show locks".
- A deadlock error (`40P01`) links here.

---

## 3. Layout

### Activity tab

```text
+-----------------------------------------------------------------------------+
| [ Activity ] [ Locks ]        [x] Hide idle  [x] Hide Basora  [ Auto 2s v ] |
+-----------------------------------------------------------------------------+
| PID  | User    | App        | State               | Duration | Wait event   |
+------+---------+------------+---------------------+----------+--------------+
| 8211 | app     | api-server | idle in transaction |   41m 8s | Client:Read #|
| 8221 | app     | api-server | active              |   2m 14s | Lock:tuple  ~|
| 8232 | app     | worker     | active              |     58s  | Lock:tuple  ~|
| 8299 | analyst | psql       | active              |     12s  | -            |
| 8301 | app     | api-server | idle                |      3s  | -            |
+------+---------+------------+---------------------+----------+--------------+
| SELECTED: PID 8211                                                          |
|  BEGIN; UPDATE orders SET status = 'shipped' WHERE id = 4821;               |
|  Started 14:02:11 - client 10.0.4.22:51422 - blocking 2 sessions            |
|  [ Explain ] [ Copy ] [ Cancel query ] [ Terminate session ]                |
+-----------------------------------------------------------------------------+
| 8 sessions - 3 active - 1 idle in transaction - 2 waiting                   |
+-----------------------------------------------------------------------------+
```

### Locks tab

```text
+-----------------------------------------------------------------------------+
| BLOCKING CHAINS (1)                                                         |
|                                                                             |
|  # PID 8211  app  idle in transaction  41m 8s                              |
|    holds RowExclusiveLock on public.orders                                  |
|    BEGIN; UPDATE orders SET status = 'shipped' WHERE id = 4821;             |
|    |                                                                        |
|    +-- blocks --> PID 8221  app  active  2m 14s                            |
|    |              waiting for ShareLock on transaction 918422              |
|    |              UPDATE orders SET total = 99 WHERE id = 4821;             |
|    |              |                                                         |
|    |              +-- blocks --> PID 8232  app  active  58s                |
|    |                             SELECT ... FOR UPDATE ...                  |
|                                                                             |
|  Root blocker: PID 8211 - idle in transaction for 41 minutes                |
|  [ Terminate 8211 ]  (releases 2 blocked sessions)                          |
+-----------------------------------------------------------------------------+
```

---

## 4. Component inventory

### Activity list

From `pg_stat_activity`. Columns: PID, user, application, client address, database,
state, state duration, query duration, wait event type and name, backend type. Sortable,
filterable, with quick filters for hide-idle, hide-own-connections and per-database.

Row emphasis:
- **`idle in transaction` above a threshold** — the single most important row state in
  this view, because it blocks vacuum and holds locks. Highlighted in `status.danger`
  with its duration.
- Waiting on a lock — `status.warning`.
- Long-running active queries — duration in warning colour past a threshold.

Basora's own connections are hidden by default (including our monitor channel) and
labelled when shown.

### Detail panel

Full statement text (monospace, wrapped), start time, client address and port, backend
type, and — critically — **what this session is blocking and what is blocking it**.

Actions:

| Action | Behaviour |
|---|---|
| Explain | Opens [wf-19](wf-19-visual-explain.md) with `EXPLAIN` (never `ANALYZE`) of the captured statement |
| Copy | Statement to clipboard |
| Cancel query | `pg_cancel_backend` — polite, cancels the statement, keeps the session |
| Terminate session | `pg_terminate_backend` — kills the connection and rolls back its transaction |

Both cancel and terminate route through the safety ladder, name the target session and
its statement, and are audited. The UI explains the difference between them in one line,
because most users do not know it.

### Blocking chains

Built from `pg_blocking_pids()` plus `pg_locks`. Rendered as a tree from root blocker
down. Each node shows PID, user, state, duration, the lock held or awaited, and the
statement. The root blocker is called out explicitly, and terminating it states how many
sessions it would release.

Cycles (true deadlocks that PostgreSQL has not yet broken) are detected and rendered as a
cycle rather than an infinite tree.

### Polling

Default 2 s on this screen (faster than the dashboard, because this is a live
firefighting view), pausable, suspended when hidden. **Selection and scroll position are
preserved across refreshes** — a list that jumps while you are trying to read a PID is
unusable.

---

## 5. States

| State | Rendering |
|---|---|
| **Loading** | Skeleton rows |
| **No blocking** | "No sessions are blocked" — the good answer, stated plainly |
| **Only idle sessions** | "8 idle connections, nothing running" |
| **Insufficient privilege** | Non-superusers see only their own sessions' statements; the banner explains this and shows the `GRANT pg_monitor` that would fix it. Other sessions still appear, with the statement column showing "insufficient privilege" — never blank |
| **Paused** | Banner with Resume and the data timestamp |
| **Stale** | Dimmed with age |
| **Error** | Inline strip with Retry; the last good snapshot stays visible |

That privilege state matters: a non-superuser seeing an empty statement column with no
explanation concludes the tool is broken.

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `F5` | Refresh now |
| `Space` | Pause / resume |
| `Enter` | Show detail for the selected session |
| `Ctrl+C` | Copy the statement |
| `Ctrl+Shift+E` | Explain the selected statement |
| `Delete` | Cancel query (confirms) |
| `Shift+Delete` | Terminate session (confirms, type-to-confirm on Production) |
| `Tab` | Switch between Activity and Locks |

---

## 7. Data contract

```csharp
public interface IActivityMonitor
{
    Task<Result<ActivitySnapshot>> GetActivityAsync(
        IDatabaseSession session, ActivityFilter filter, CancellationToken ct);

    Task<Result<IReadOnlyList<BlockingChain>>> GetBlockingChainsAsync(
        IDatabaseSession session, CancellationToken ct);

    Task<Result> CancelBackendAsync(IDatabaseSession session, int pid, CancellationToken ct);
    Task<Result> TerminateBackendAsync(IDatabaseSession session, int pid, CancellationToken ct);
}

public sealed record BackendActivity(
    int Pid, string? Username, string? ApplicationName, string? ClientAddress,
    string? Database, string State, TimeSpan? StateDuration, TimeSpan? QueryDuration,
    string? WaitEventType, string? WaitEvent, string? Query, bool QueryVisible,
    string BackendType, DateTimeOffset? TransactionStart, bool IsOwnConnection);

public sealed record BlockingChain(
    BackendActivity Root, IReadOnlyList<BlockingNode> Blocked, bool IsCycle);

public sealed record BlockingNode(
    BackendActivity Backend, string LockType, string? LockedObject,
    string LockMode, IReadOnlyList<BlockingNode> Children);
```

Chain construction is pure over the raw rows and is unit-tested against fixtures,
including cycles, multi-root chains and self-blocking cases.

---

## 8. Acceptance criteria

- [ ] Activity shows all documented columns and Basora's own connections are hidden by
      default.
- [ ] `idle in transaction` sessions past the threshold are prominently highlighted with
      their duration.
- [ ] Blocking chains render as a tree with the root blocker named.
- [ ] Terminating the root states how many sessions it would release.
- [ ] Lock cycles are detected and rendered as cycles, not infinite trees.
- [ ] Cancel and terminate are distinguished in the UI, both route through the safety
      ladder, and both are audited.
- [ ] Non-superusers see a clear privilege explanation and "insufficient privilege"
      rather than blank statements.
- [ ] Polling preserves selection and scroll position across refreshes.
- [ ] Polling suspends when the tab is hidden and uses the monitor channel.
- [ ] "No sessions are blocked" is a real, stated result.
- [ ] All seven states render in both themes.
