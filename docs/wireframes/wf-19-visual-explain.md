# wf-19 — Visual EXPLAIN

> Task: `T-V05` (plan parsing + findings), `T-V06` (visualisation). MVP 2. Document or
> a tab in the results panel.

---

## 1. Purpose

Turn a PostgreSQL execution plan into an answer. The one job it must do well: **point at
the problem**. A faithful rendering of the plan tree is table stakes; saying "this Seq
Scan discarded 4.2M rows because there is no index on `orders.customer_id`" is the
product.

---

## 2. Entry points

- `Ctrl+Shift+E` (EXPLAIN) or `Ctrl+Alt+Enter` (EXPLAIN ANALYZE) in the editor.
- Clicking the plan verdict line in [wf-09](wf-09-result-viewer.md).
- "Explain" on an active query in [wf-21](wf-21-activity-locks.md) or a slow query in
  [wf-22](wf-22-storage-index-intel.md).
- Pasting plan JSON or text into an "Analyze plan" input — useful for plans from
  production that the user cannot re-run.

---

## 3. Layout

```text
+-----------------------------------------------------------------------------+
| [Tree] [Flame] [Raw JSON] [Text]  | Colour by [ Self time v ]  [Export]      |
| Planning 0.42 ms - Execution 2,412 ms - 3 findings                          |
+-----------------------------------------------------------------------------+
|                                                                             |
|                    +---------------------------+                            |
|                    | Nested Loop               |  2,412 ms  100%            |
|                    | rows 14 (est 1)  loops 1  |                            |
|                    +------------+--------------+                            |
|                                 |                                           |
|              +------------------+------------------+                        |
|              |                                     |                        |
|   +----------v-----------+           +-------------v-------------+          |
|   | Index Scan           |           | Seq Scan on orders        | #  <- red|
|   | users_pkey           |           | Filter: customer_id = $1  |          |
|   | rows 1  0.03 ms  0%  |           | rows 14  2,398 ms   99%   |          |
|   +----------------------+           | Rows removed: 4,199,986   |          |
|                                      +---------------------------+          |
|                                                                             |
+-----------------------------------------------------------------------------+
| FINDINGS                                                                    |
| x  Sequential scan on orders discarded 4,199,986 rows to return 14.         |
|    No index exists on orders.customer_id.                                   |
|    CREATE INDEX CONCURRENTLY idx_orders_customer_id ON orders (customer_id);|
|    [ Copy ]  [ Open in editor ]  [ Check migration safety ]                 |
|                                                                             |
| ~  Row estimate off by 14x on the Nested Loop (est 1, actual 14).           |
|    Statistics on orders were last collected 18 days ago.  [ Run ANALYZE ]   |
+-----------------------------------------------------------------------------+
```

---

## 4. Component inventory

### Views

| View | Purpose |
|---|---|
| **Tree** (default) | The plan as a node graph, top-down, with heat colouring |
| **Flame** | Horizontal time-proportional bars — the fastest way to see where time went |
| **Raw JSON** | The unmodified `EXPLAIN (FORMAT JSON)` output, copyable |
| **Text** | Standard `EXPLAIN` text output, for pasting into issues |

### Node

Header: node type and target relation/index. Body: rows (actual vs estimated), time,
loops, and — when `BUFFERS` was requested — shared hit/read. A heat bar shows the node's
**self time** as a percentage of total.

**Self time, not total time**, is the default colouring. Total time on a root node is
always 100% and tells you nothing; self time is what identifies the culprit.

Nodes are collapsible; a collapsed subtree shows its aggregate time and row count.

### Findings engine (`Basora.Analytics`)

Pure rules over the parsed plan:

| Finding | Condition |
|---|---|
| Expensive sequential scan | Seq Scan with high `Rows Removed by Filter` on a large relation |
| Missing index | The filtered column has no supporting index |
| Bad row estimate | Actual/estimated ratio beyond a threshold, with the relation's last-analyze time |
| Nested loop with a large inner side | Loop count times inner rows is large |
| External sort / hash spill | `Sort Method: external merge` or a hash batch count above 1 — suggests `work_mem` |
| Index scan not used | An index exists but the plan chose a scan; explains why (low selectivity, type mismatch, function on the column) |
| Lossy bitmap heap scan | Recheck cost from `work_mem` pressure |
| Slow function call | High per-row cost in a filter |
| Parallelism not used | Table large enough to benefit but no parallel nodes |
| Trigger overhead | Trigger time reported separately and material |

Each finding carries a severity, plain-language explanation, and where possible a
concrete SQL suggestion linking straight into [wf-18](wf-18-schema-diff.md)'s safety
analysis.

### Comparison mode

Two plans side by side (before/after an index, or query A vs query B), with per-node
deltas and a headline improvement figure. This is the payoff for the whole optimisation
flow in `../08-ux-flows.md` Flow 6.

### The ANALYZE warning

`EXPLAIN ANALYZE` **executes the statement**. For any non-`SELECT` statement, or any
statement on a Production connection, the UI:

- states plainly that the statement will actually run,
- offers to wrap it in a transaction that is rolled back afterwards,
- and routes through the safety ladder for mutating statements.

Plain `EXPLAIN` (no ANALYZE) is always the default.

---

## 5. States

| State | Rendering |
|---|---|
| **No plan** | "Run EXPLAIN to see the plan" with the shortcut |
| **Explaining** | Progress; for `ANALYZE`, the elapsed execution time |
| **Estimate-only plan** | Clearly marked "estimates only — no actual timings"; findings that need actuals are suppressed rather than guessed |
| **Parse failure** | Raw output shown with a note that structured analysis is unavailable |
| **No findings** | "No problems detected in this plan" — a real, useful answer, not an empty panel |
| **Stale statistics** | Banner naming the relations whose stats are old, with Run ANALYZE |
| **Permission denied** | Explains that `EXPLAIN` requires access to the referenced objects |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Ctrl+Shift+E` / `Ctrl+Alt+Enter` | EXPLAIN / EXPLAIN ANALYZE |
| Arrows | Navigate the tree |
| `Space` | Collapse/expand a node |
| `Enter` | Focus the node's detail |
| `1`/`2`/`3`/`4` | Switch view |
| `Ctrl+C` | Copy the plan |
| `Ctrl+E` | Export |

Clicking a relation name in a node opens its table document; clicking an index name opens
its Indexes tab.

---

## 7. Data contract

```csharp
public interface IExplainService
{
    Task<Result<ExplainPlan>> ExplainAsync(
        IDatabaseSession session, string sql, ExplainOptions options, CancellationToken ct);

    Result<ExplainPlan> ParseJson(string json);
    Result<ExplainPlan> ParseText(string text);
}

public sealed record ExplainOptions(
    bool Analyze, bool Buffers, bool Verbose, bool Settings, bool Wal,
    bool WrapInRolledBackTransaction);

public interface IPlanAnalyzer
{
    IReadOnlyList<PlanFinding> Analyze(ExplainPlan plan, PlanAnalysisContext context);
}

public sealed record PlanAnalysisContext(
    IReadOnlyDictionary<string, TableDescriptor> Relations,
    IReadOnlyDictionary<string, DateTimeOffset?> LastAnalyzed,
    long WorkMemBytes);

public interface IPlanComparer
{
    PlanComparison Compare(ExplainPlan before, ExplainPlan after);
}
```

`IPlanAnalyzer` is pure and lives in `Basora.Analytics`. Its test suite is a corpus of
real plan JSON files with expected findings — the highest-value tests in MVP 2.

---

## 8. Acceptance criteria

- [ ] Plan JSON parses correctly for every supported server version, including CTEs,
      subplans, parallel workers, JIT, and trigger timings.
- [ ] Self time is computed correctly and is the default heat metric.
- [ ] All ten findings in section 4 are detected on a corpus of real plans.
- [ ] Every finding is in plain language and, where applicable, carries runnable SQL.
- [ ] Suggested indexes link into the migration safety analysis.
- [ ] Estimate-only plans suppress actual-dependent findings rather than guessing.
- [ ] `EXPLAIN ANALYZE` on a mutating statement warns that it executes, offers a
      rolled-back transaction, and routes through the safety ladder.
- [ ] Pasted plan JSON or text can be analysed without a connection.
- [ ] Comparison mode shows per-node deltas and a headline improvement figure.
- [ ] Flame view is time-proportional and matches the tree's numbers.
- [ ] "No findings" is a real state with a real message.
- [ ] All seven states render in both themes.
