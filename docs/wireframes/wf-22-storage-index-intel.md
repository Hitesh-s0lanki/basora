# wf-22 — Storage, Table Health, Index Intelligence and Slow Queries

> Task: `T-N05`. MVP 2. One document, four tabs — they share a data source and a
> findings pattern.

---

## 1. Purpose

Where is the space going, which tables are unhealthy, which indexes are dead weight, and
which queries cost the most. The one job it must do well: **rank by impact, not by
alphabet.** Every list here answers "what should I look at first?"

---

## 2. Entry points

- Findings on [wf-20](wf-20-health-dashboard.md) ("7 unused indexes", "4 bloated tables",
  "3 slow queries").
- Database menu > Storage / Slow queries.
- The Indexes tab of a table links into Index Intelligence scoped to that table.

---

## 3. Layout

### Storage tab

```text
+-----------------------------------------------------------------------------+
| Database: production - 182 GB                                    [ Refresh ]|
+-----------------------------------------------------------------------------+
|  Tables   110 GB  ############################......                        |
|  Indexes   61 GB  ###############.....                                      |
|  TOAST      8 GB  ##...                                                     |
|  Other      3 GB  #                                                         |
+-----------------------------------------------------------------------------+
| LARGEST OBJECTS                                    [ Tables | Indexes | All ]|
| Name                      Total     Table    Index    TOAST   Rows          |
| public.orders             48 GB     31 GB    16 GB    1 GB    428M          |
| public.events             31 GB     28 GB     2 GB    1 GB    1.2B          |
| public.users               9 GB      6 GB     3 GB     -      18M           |
+-----------------------------------------------------------------------------+
```

### Table Health tab

```text
| Table          | Rows | Size  | Dead  | Dead% | Last vacuum | Last analyze  |
| public.orders  | 428M | 48 GB | 61M   | 14% ~ | 6 days ago  | 18 days ago # |
| public.events  | 1.2B | 31 GB | 240M  | 20% # | never     # | never       # |
| public.users   |  18M |  9 GB | 210K  | 1.2%  | 2 hours ago | 2 hours ago   |

FINDINGS
 #  public.events has never been vacuumed or analyzed.
    Planner estimates for this table are unreliable.   [ Run ANALYZE ]
 ~  public.orders: 14% dead tuples, autovacuum last ran 6 days ago.
    autovacuum_vacuum_scale_factor may be too high for a table this size.
```

### Index Intelligence tab

Same structure as the Indexes tab in [wf-12](wf-12-indexes-constraints.md), but scoped to
the whole database and ranked by reclaimable size:

```text
| Index                    | Table   | Size  | Scans | Finding                |
| idx_users_phone          | users   | 8 GB  |     0 | unused                 |
| idx_orders_created_at2   | orders  | 6 GB  |     0 | duplicate of ..._created|
| idx_events_type_created  | events  | 4 GB  |    12 | redundant prefix       |

 24 GB reclaimable across 7 indexes.
 Statistics reset 14 days ago - counts reflect that window only.
```

### Slow Queries tab

```text
| Query                          | Total time | Calls  | Mean   | Rows/call  |
| SELECT * FROM orders WHERE ... | 4h 12m 68% | 12,391 | 1.22 s | 14         |
| UPDATE sessions SET ...        | 1h 02m 17% | 892K   | 4.1 ms | 1          |
| SELECT count(*) FROM events    |   38m   9% |    412 | 5.5 s  | 1          |

 Ranked by total time. A fast query called often can cost more than a slow one.
 [ Explain ]  [ Open in editor ]  [ Reset statistics ]
```

---

## 4. Component inventory

### Shared pattern

Every tab: a summary header, a ranked table, and a findings strip with actionable
recommendations. Every row navigates to the object it names.

### Storage

Composition bar (tables / indexes / TOAST / other) and a ranked object list with a
tables/indexes/all filter. Sizes come from `pg_total_relation_size`, `pg_relation_size`
and `pg_indexes_size`; the breakdown arithmetic is shown so the numbers are checkable.

Growth over time requires historical snapshots (MVP 3); until then the tab shows current
size only and does not fabricate a trend.

### Table health

Live/dead tuples, dead percentage, sizes, last vacuum/autovacuum/analyze/autoanalyze,
and vacuum counts. Findings for: high dead-tuple ratio, never vacuumed, never analyzed,
autovacuum falling behind, table approaching transaction-ID wraparound, and bloat
estimate above threshold.

**Bloat is an estimate** produced by the standard `pg_stats`-based approximation. Every
place it appears is labelled as an estimate, because presenting an approximation as a
measurement is how tools lose credibility with DBAs.

### Index intelligence

Database-wide version of the analyzer from [wf-12](wf-12-indexes-constraints.md), ranked
by reclaimable bytes. Always shows the statistics-reset timestamp alongside scan counts.
Never offers automatic dropping — every recommendation produces reviewable SQL.

### Slow queries

From `pg_stat_statements`, **ranked by total execution time by default**, with sortable
columns for calls, mean, min, max, stddev, and rows. The tab states its ranking rationale
inline, because "mean time" ranking hides the 2 ms query called 40 million times that is
actually the problem.

Query text is normalised by the extension (constants replaced by `$n`); the UI says so,
so nobody wonders why their literals are gone. Actions: Explain (opens
[wf-19](wf-19-visual-explain.md)), open in editor, and reset statistics (which confirms,
since it discards a shared measurement window everyone depends on).

---

## 5. States

| State | Rendering |
|---|---|
| **Loading** | Skeleton rows; size queries can be slow on large databases and are cancellable |
| **Empty** | Per tab: "No objects", "No tables need attention", "No index findings", "No queries recorded yet" |
| **`pg_stat_statements` missing** | The Slow Queries tab shows the exact remediation: `CREATE EXTENSION pg_stat_statements;` plus the `shared_preload_libraries` requirement and the fact that it needs a restart |
| **`pg_stat_statements` unreadable** | Explains that the role lacks `SELECT` and shows the `GRANT` |
| **Statistics never collected** | Table health suppresses tuple-based findings and offers `ANALYZE` |
| **Stats recently reset** | Banner: counts cover only N hours, so unused-index findings are unreliable — findings are downgraded to informational |
| **Permission denied** | Objects not visible to the role are reported as a count |
| **Error** | Per-tab inline strip with Retry |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Tab` | Switch tabs |
| `Enter` | Open the object's document |
| `Ctrl+Shift+E` | Explain (Slow Queries tab) |
| `Ctrl+C` | Copy the object name or query text |
| `F5` | Refresh |
| `Ctrl+E` | Export the current list |

Sorting is by any column; the default ranking per tab is documented in the UI so the user
knows what they are looking at.

---

## 7. Data contract

```csharp
public interface IStorageAnalyzer
{
    Task<Result<StorageSnapshot>> AnalyzeAsync(IDatabaseSession session, CancellationToken ct);
}

public sealed record StorageSnapshot(
    long DatabaseBytes, long TableBytes, long IndexBytes, long ToastBytes, long OtherBytes,
    IReadOnlyList<RelationSize> Relations);

public interface ITableHealthAnalyzer
{
    IReadOnlyList<TableHealthFinding> Analyze(IReadOnlyList<TableStatistics> stats,
                                              TableHealthThresholds thresholds);
}

public sealed record TableStatistics(
    DbObjectRef Ref, long LiveTuples, long DeadTuples, long TotalBytes, long IndexBytes,
    DateTimeOffset? LastVacuum, DateTimeOffset? LastAutoVacuum,
    DateTimeOffset? LastAnalyze, DateTimeOffset? LastAutoAnalyze,
    long VacuumCount, long AnalyzeCount, long? EstimatedBloatBytes,
    long? TransactionIdAge);

public interface ISlowQueryService
{
    Task<Result<IReadOnlyList<SlowQueryEntry>>> GetTopAsync(
        IDatabaseSession session, SlowQueryRanking ranking, int limit, CancellationToken ct);
    Task<Result> ResetStatisticsAsync(IDatabaseSession session, CancellationToken ct);
}

public sealed record SlowQueryEntry(
    long QueryId, string NormalisedQuery, long Calls, TimeSpan TotalTime,
    TimeSpan MeanTime, TimeSpan MinTime, TimeSpan MaxTime, TimeSpan StdDev,
    long Rows, double? CacheHitPercent, double PercentOfTotal);

public enum SlowQueryRanking { TotalTime, MeanTime, Calls, Rows }
```

All analyzers are pure and live in `Basora.Analytics`, tested against fixture statistics.

---

## 8. Acceptance criteria

- [ ] Storage breakdown arithmetic is correct and the components sum to the total.
- [ ] Largest-object lists are ranked by total size and filterable by kind.
- [ ] Table health shows all vacuum/analyze timestamps including never-run as "never".
- [ ] Bloat is labelled an estimate everywhere it appears.
- [ ] Transaction-ID wraparound risk is detected and surfaced.
- [ ] Index findings rank by reclaimable bytes and always show the stats-reset time.
- [ ] A recent stats reset downgrades unused-index findings to informational.
- [ ] No index or table is ever modified automatically.
- [ ] Slow queries rank by total time by default, with the rationale stated in the UI.
- [ ] Query normalisation is explained in the UI.
- [ ] Missing or unreadable `pg_stat_statements` shows exact, complete remediation.
- [ ] Reset statistics confirms and explains the shared consequence.
- [ ] Long-running size queries are cancellable.
- [ ] All eight states render in both themes.
