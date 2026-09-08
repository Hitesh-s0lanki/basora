# wf-09 — Query Result Viewer

> Task: `T-Q07`. Panel inside [wf-03](wf-03-app-shell.md) region F, or inside a split
> [wf-08](wf-08-sql-editor.md).

---

## 1. Purpose

Display query results as they stream in, in whichever shape the user needs: grid, JSON,
text, or chart. The one job it must do well: **show the first rows immediately and never
freeze**, no matter how large the result.

Shares the grid control with [wf-05](wf-05-table-document.md); this document specifies
what is different for query results.

---

## 2. Entry points

- Any query execution from [wf-08](wf-08-sql-editor.md).
- Running a saved query from [wf-10](wf-10-history-favorites.md).
- "Run" from a command-palette action or a plan finding.

---

## 3. Layout

```text
+-----------------------------------------------------------------------------+
| [ Result 1 ] [ Result 2 ] [ Messages ] [ Plan ]        [Grid|JSON|Text|Chart]|
+-----------------------------------------------------------------------------+
| [ search results     ] [ filter ] [ columns v ]     [ Export v ] [ Copy v ]  |
+---+----------+---------------------+-------------+---------------------------+
|   | id       | email               | order_count | created_at                |
|   | # int8   | T text              | # int8      | clock timestamptz         |
+---+----------+---------------------+-------------+---------------------------+
| 1 | 1        | a@example.com       | 14          | 2026-01-01 09:12:44+00    |
| 2 | 2        | b@example.com       | 3           | 2026-01-02 11:04:02+00    |
| 3 | 3        | c@example.com       | NULL        | 2026-01-02 15:41:19+00    |
+---+----------+---------------------+-------------+---------------------------+
| 1,204 rows in 182 ms (server 174 ms) - streaming complete                   |
| ~ Seq Scan on orders - 4.2M rows scanned to return 1,204   [Explain]        |   <- MVP 2
+-----------------------------------------------------------------------------+
```

While streaming, the footer reads: `receiving... 24,000 rows` with a Cancel button and a
live elapsed timer.

---

## 4. Component inventory

### Result set tabs

One tab per result set in a multi-statement batch, labelled with the statement's kind and
target (`SELECT users`, `UPDATE orders`). Non-row results (`UPDATE`, `INSERT`, DDL) show
an affected-row summary card rather than an empty grid.

### View modes

| Mode | Behaviour |
|---|---|
| **Grid** | Default. Same virtualised control as the table document, but **read-only unless** the result set resolves to a single updatable table with a key — in which case editing is enabled and pending changes work exactly as in [wf-05](wf-05-table-document.md) |
| **JSON** | Rows as a JSON array, streamed, with folding and copy. Respects the raw `jsonb` text of json columns |
| **Text** | Fixed-width aligned text, `psql`-like. Useful for pasting into tickets |
| **Chart** | Pick X, Y and series columns; line, bar, area, scatter, pie. Chart type suggestions are based on column types. MVP 2 |

Mode is remembered per document.

### Toolbar

- **Search** — highlights matches across loaded rows, `Enter` cycles.
- **Filter** — client-side filter over loaded rows for quick narrowing; opens
  [wf-06](wf-06-filter-builder.md) in client mode. Distinguished clearly in the UI from a
  server-side `WHERE`, because the difference matters on a truncated result.
- **Columns** — visibility and order.
- **Export** — opens [wf-14](wf-14-export-dialog.md), scoped to this result.
- **Copy** — cells, with headers, as JSON, as `INSERT` statements, as Markdown table.

### Value inspector

`Space` on a focused cell opens a side panel with the full value: pretty-printed for
JSON, hex+ASCII for `bytea`, full text with wrapping for long strings, and the value's
type and byte length. Essential for `jsonb` and text columns that will never fit a cell.

### Footer

Row count, elapsed time, server execution time when available, streaming state, and the
truncation notice. In MVP 2 it also carries the one-line plan verdict linking to
[wf-19](wf-19-visual-explain.md).

---

## 5. States

| State | Rendering |
|---|---|
| **Idle** | "Run a query to see results" |
| **Executing (no rows yet)** | Progress strip with elapsed time and Cancel; grid area shows a skeleton once columns arrive |
| **Streaming** | Rows append live; the row counter updates; scrolling and searching work on what has arrived |
| **Complete** | Final counts and timings |
| **Truncated** | Amber banner: "Showing the first 50,000 of an unknown total" with **Load more** and **Export full result** (which streams to file without materialising) |
| **Cancelled** | "Cancelled after 4.2 s - 12,400 rows received"; retained rows stay usable |
| **Empty** | "Query returned no rows" plus the elapsed time — not an error |
| **Non-row result** | Card: "UPDATE - 47 rows affected in 12 ms" |
| **Error** | Error card with SQLSTATE, message, detail, hint, a Copy details action, and a link back to the offending statement position |
| **Multiple results** | Tabs, with a summary tab showing all statements and their outcomes |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Ctrl+F` | Search results |
| `Space` | Toggle value inspector |
| `Ctrl+C` / `Ctrl+Shift+C` | Copy / copy with headers |
| `Ctrl+Alt+C` | Copy as INSERT |
| `Ctrl+E` | Export |
| `Escape` | Cancel a running query |
| `Ctrl+Home` / `Ctrl+End` | First / last loaded row |
| `Alt+1..9` | Switch result set tab |

Sorting a **complete** result sorts client-side instantly. Sorting a **truncated** result
warns that it sorts only loaded rows and offers to re-run with `ORDER BY` instead — a
silent client-side sort on partial data is actively misleading.

---

## 7. Data contract

```csharp
public interface IResultBuffer
{
    IReadOnlyList<ResultColumn> Columns { get; }
    int LoadedRowCount { get; }
    bool IsComplete { get; }
    bool IsTruncated { get; }
    object?[] GetRow(int index);
    void Append(ResultChunk chunk);
    event EventHandler? RowsAppended;
}

public interface IResultExporter
{
    Task<Result> ExportAsync(ExportRequest request, IProgress<ExportProgress> progress,
                             CancellationToken ct);
}

public interface IResultUpdatabilityResolver
{
    Result<UpdatableResult> Resolve(QueryResultSet result, IDatabaseSession session);
}

public sealed record UpdatableResult(
    DbObjectRef Table, RowIdentityStrategy Identity, IReadOnlyList<string> KeyColumns);
```

`IResultBuffer` is a windowed store: rows above a configurable count spill to a temp
file rather than staying resident, keeping memory flat. Consumes the streaming contract
from `../05-postgresql-data-layer.md` section 4.

---

## 8. Acceptance criteria

- [ ] First rows render within 500 ms of the server responding.
- [ ] The UI stays responsive while 1M rows stream; scrolling and searching work during
      streaming.
- [ ] Memory for 50,000 rows x 20 columns stays under 250 MB.
- [ ] Cancel is acknowledged in the UI within 100 ms and retained rows remain usable.
- [ ] Truncation is stated explicitly with Load more and Export full result.
- [ ] Sorting a truncated result warns and offers a server-side re-run.
- [ ] Multi-statement batches produce one tab per result plus a summary tab.
- [ ] Non-row results show an affected-row card, not an empty grid.
- [ ] Results from a single keyed table are editable and reuse the pending-change flow.
- [ ] JSON mode preserves the raw `jsonb` text.
- [ ] Value inspector handles long text, JSON and `bytea`.
- [ ] Errors show SQLSTATE, detail and hint, and link back to the statement position.
- [ ] All ten states render in both themes.
