# wf-20 — Database Health Dashboard

> Task: `T-N01`. MVP 2. Document — opens automatically for Production connections.

---

## 1. Purpose

Answer "is my database OK right now, and if not, what is wrong?" in one screen. The one
job it must do well: **every number is a link to the thing that explains it.** A
dashboard of metrics with no drill-down is a screensaver.

---

## 2. Entry points

- Automatically on connecting to a Production-tagged connection (setting-controlled).
- Database menu > Health dashboard.
- Command palette: "Show database health".
- Clicking the connection name in the status bar.

---

## 3. Layout

```text
+-----------------------------------------------------------------------------+
| production@postgres.company.com          [ Refresh ] [ Auto 5s v ] [ Pause ]|
+-----------------------------------------------------------------------------+
|  +----------------+ +----------------+ +----------------+ +----------------+|
|  | CONNECTIONS    | | CACHE HIT      | | DATABASE SIZE  | | TRANSACTIONS   ||
|  |   72 / 200     | |    98.2%       | |    182 GB      | |   1,204 /s     ||
|  |   ###....      | |  ^^^^^^^^      | |  +2.1 GB/7d    | |  ^^^^^^^^^     ||
|  +----------------+ +----------------+ +----------------+ +----------------+|
|                                                                             |
|  +----------------+ +----------------+ +----------------+ +----------------+|
|  | ACTIVE QUERIES | | WAITING        | | LONGEST QUERY  | | REPLICATION LAG||
|  |      8         | |      3    ~    | |    4m 12s  #   | |     1.2 s      ||
|  +----------------+ +----------------+ +----------------+ +----------------+|
|                                                                             |
|  FINDINGS (4)                                              [ All | Critical]|
|  +-------------------------------------------------------------------------+|
|  | #  2 queries are blocking 6 others                       [ Investigate ]||
|  |    Longest blocker: PID 8211, idle in transaction 41 min                 ||
|  |-------------------------------------------------------------------------||
|  | ~  3 slow queries account for 68% of total execution time [ View ]      ||
|  |-------------------------------------------------------------------------||
|  | ~  7 unused indexes occupy 24 GB                          [ View ]      ||
|  |-------------------------------------------------------------------------||
|  | ~  4 tables have over 20% dead tuples; autovacuum is behind [ View ]    ||
|  +-------------------------------------------------------------------------+|
|                                                                             |
|  [ Storage ]  [ Activity ]  [ Locks ]  [ Slow queries ]  [ Indexes ]        |
+-----------------------------------------------------------------------------+
| Last updated 3 s ago - PostgreSQL 16.2 - uptime 42 d - stats reset 14 d ago |
+-----------------------------------------------------------------------------+
```

---

## 4. Component inventory

### Metric tiles

Each tile: label, current value, a sparkline of recent history, and a delta or threshold
indicator. Colour follows threshold state (`status.success` / `warning` / `danger`), and
**every tile is clickable**, navigating to the screen that explains it.

| Tile | Source | Threshold logic |
|---|---|---|
| Connections | `pg_stat_activity` count vs `max_connections` | warn above 70%, danger above 90% |
| Cache hit ratio | `pg_stat_database` | warn below 95%, danger below 90% — with the caveat that a low ratio on a small database is meaningless, so the tile suppresses the warning below a size floor |
| Database size | `pg_database_size` | growth rate over 7 days |
| Transactions/s | `xact_commit` + `xact_rollback` delta | informational |
| Active queries | non-idle backends | warn above a configurable count |
| Waiting | backends with a non-null `wait_event` | warn above 0 sustained |
| Longest query | max duration among active | warn above 1 min, danger above 5 |
| Replication lag | `pg_stat_replication` | warn above 10 s; hidden entirely if no replicas |

Tiles that are unavailable (no replicas, no `pg_stat_statements`) are **hidden or shown
as unconfigured with a fix**, never shown as zero. A zero that means "we could not
measure this" is a lie.

### Findings list

The heart of the screen. Each finding: severity glyph, plain-language summary, the
supporting number, and an action that navigates to the detail. Findings are ranked by
severity then by impact, and are **deduplicated over time** so a persistent condition
does not flood the list.

Findings come from `Basora.Analytics` rules over the collected snapshot: blocking chains,
slow-query concentration, unused indexes, bloat, autovacuum lag, long-running
transactions, idle-in-transaction sessions, connection saturation, transaction-ID
wraparound risk, missing statistics, and unlogged tables in production.

### Polling

Configurable interval (default 5 s), pausable, and **automatically suspended when the tab
is not visible**. The dashboard uses the dedicated monitor channel so it never competes
with user queries. The footer always shows the age of the data.

### Navigation row

Direct links to [wf-21](wf-21-activity-locks.md), [wf-22](wf-22-storage-index-intel.md)
and the slow-query view.

---

## 5. States

| State | Rendering |
|---|---|
| **Loading** | Tile skeletons; the first snapshot typically lands in under a second |
| **Healthy** | "No findings — this database looks healthy" with the checks that were run listed, so the user knows what was actually verified |
| **Degraded permissions** | Tiles requiring privileges the role lacks show "requires `pg_monitor`" with the exact `GRANT`, rather than an error or a zero |
| **Extension missing** | Slow-query tiles show "`pg_stat_statements` is not installed" with the `CREATE EXTENSION` statement and a note about `shared_preload_libraries` |
| **Stale** | Above two intervals without an update, tiles dim and the footer shows the age in red |
| **Paused** | Banner with Resume; data shown with its timestamp |
| **Replica** | Banner stating this is a standby, with write-related tiles hidden and lag prominent |
| **Error** | Per-tile error state; one failing query never blanks the whole dashboard |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `F5` | Refresh now |
| `Space` | Pause / resume polling |
| `Enter` | Drill into the selected tile or finding |
| Arrows | Move between tiles and findings |
| `Ctrl+E` | Export the snapshot as JSON |

Export produces a support-shareable snapshot with metrics and findings but **no query
text and no data values** by default, with an explicit opt-in to include statement text.

---

## 7. Data contract

```csharp
public interface IHealthMonitor
{
    Task<Result<HealthSnapshot>> CollectAsync(IDatabaseSession session, CancellationToken ct);
    IObservable<HealthSnapshot> Stream(IDatabaseSession session, TimeSpan interval);
}

public sealed record HealthSnapshot(
    DateTimeOffset CollectedAt,
    IReadOnlyList<MetricValue> Metrics,
    ActivitySnapshot Activity,
    StorageSnapshot Storage,
    IReadOnlyList<string> UnavailableMetrics);   // with reasons

public sealed record MetricValue(
    string Key, string Label, double? Value, string? FormattedValue, string? Unit,
    ThresholdState State, IReadOnlyList<double> RecentHistory, string? UnavailableReason);

public enum ThresholdState { Unknown, Good, Warning, Critical, NotApplicable }

public interface IHealthAnalyzer
{
    IReadOnlyList<HealthFinding> Analyze(HealthSnapshot snapshot, HealthThresholds thresholds);
}

public sealed record HealthFinding(
    string Code, FindingSeverity Severity, string Summary, string Detail,
    string? NavigationTarget, object? NavigationParameter, double ImpactScore);
```

`IHealthAnalyzer` is pure — it takes a snapshot and returns findings, so every rule is
unit-tested against fixture snapshots with no database involved.

---

## 8. Acceptance criteria

- [ ] All eight tiles render with sparklines and threshold colouring.
- [ ] Every tile and every finding navigates to the screen that explains it.
- [ ] Unavailable metrics are hidden or marked unconfigured with a fix — **never zero**.
- [ ] Missing `pg_stat_statements` shows the exact remediation including the
      `shared_preload_libraries` requirement.
- [ ] Insufficient privileges show the exact `GRANT` (`pg_monitor`).
- [ ] Polling uses the monitor channel, suspends when hidden, and is pausable.
- [ ] Data age is always visible; stale data is visibly dimmed.
- [ ] A single failing collection query degrades one tile, not the dashboard.
- [ ] Replica connections show a standby banner and hide write-only metrics.
- [ ] The healthy state lists which checks were run.
- [ ] Cache-hit warnings are suppressed below a database-size floor.
- [ ] Snapshot export excludes query text and data values by default.
- [ ] All eight states render in both themes.
