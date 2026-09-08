# wf-05 — Table Document and Data Grid

> Task: `T-D03` (document + grid), `T-D04` (cell editors), `T-D05` (pending changes).
> Document, inside [wf-03](wf-03-app-shell.md) region E.

---

## 1. Purpose

The screen users spend the most time in. Browse and edit one table's rows, and reach its
structure, relations, indexes, constraints, triggers and DDL without leaving the tab.

The one job it must do well: **edit data safely and fast** — spreadsheet-quick to type
in, but nothing reaches the database until the user says so.

---

## 2. Entry points

- `Enter` / double-click a table in [wf-04](wf-04-object-explorer.md).
- `Ctrl+P` Open Anything.
- `F12` on a table name in the SQL editor.
- Clicking a foreign-key value in another table document.

---

## 3. Layout

```text
+-----------------------------------------------------------------------------+
| public.users                        1.2M rows (est) - 184 MB - 5 indexes    |
+-----------------------------------------------------------------------------+
| [ Data ] [ Structure ] [ Relations ] [ Indexes ] [ Constraints ] [ Triggers ] [ SQL ] |
+-----------------------------------------------------------------------------+
| [+] [-] [copy] | [ filter: status = 'active' AND created_at > ... ] [x] [v] |
|                | sort: created_at DESC              [ Refresh ] [ Export ]  |
+-----------------------------------------------------------------------------+
|   | id  | email             | name           | status  | created_at         |
|   | #   | T                 | T              | T enum  | clock              |
+---+-----+-------------------+----------------+---------+--------------------+
| 1 | 1   | a@example.com     | Hitesh         | active  | 2026-01-01 09:12   |
| 2 | 2   | b@example.com     | Rahul          | active  | 2026-01-02 11:04   |
|!3 | 3   | c@example.com     |[Priya Sharma  ]| active  | 2026-01-02 15:41   |   <- modified
|+4 |     | d@example.com     | New Person     | NULL    | now()              |   <- inserted
|-5 | 5   | e@example.com     | Deleted Row    | active  | 2026-01-03 08:20   |   <- deleted
+---+-----+-------------------+----------------+---------+--------------------+
| < 1-200 of ~1.2M >    3 pending changes  [Preview SQL] [Discard] [Commit]   |
+-----------------------------------------------------------------------------+
```

Sub-tabs Structure, Relations, Indexes, Constraints and Triggers are specified in
[wf-11](wf-11-structure-editor.md) and [wf-12](wf-12-indexes-constraints.md). The SQL
tab shows the object's `CREATE` statement, read-only, copyable.

---

## 4. Component inventory

### Header

Qualified name, row estimate (always `~`, with a click to run an exact `COUNT(*)`),
total size, index count, and a comment if one exists. On a partitioned table, a partition
count and a link to the partition list.

### Toolbar

Insert row, delete row(s), duplicate row, copy menu, filter bar, sort indicator,
Refresh, Export, and a column-visibility menu.

### Filter bar

Single-line expression display. Click or `Ctrl+Shift+F` opens
[wf-06 Filter Builder](wf-06-filter-builder.md). The `[v]` dropdown holds recent and
saved filters for this table. The generated `WHERE` clause is always viewable.

### The grid

| Aspect | Behaviour |
|---|---|
| Virtualisation | Row and column virtualised; constant memory regardless of row count |
| Column header | Type glyph, name, sort indicator, resize handle, drag to reorder |
| Row header | Row number, plus a state glyph: `!` modified, `+` inserted, `-` deleted |
| Alignment | Numbers right, booleans centre, everything else left |
| `NULL` | Literal dimmed italic `NULL`, always distinguishable from an empty string |
| Selection | Cell, range, row and column; `Ctrl`/`Shift` extend |
| Frozen columns | First N columns freezable, persisted per table |
| Row height | From `density.row.height`; never hard-coded |
| Inline expand | `Space` opens the value inspector for the focused cell |

### Cell editors by type (`T-D04`)

| Category | Editor |
|---|---|
| Text | Inline text box; `Shift+Enter` opens a multi-line popup |
| Numeric | Inline text box with validation and no spinner |
| Boolean | Tri-state toggle: true / false / NULL |
| DateTime | Text box + calendar popover; timezone shown for `timestamptz` |
| Enum | Dropdown of the actual `enumlabel` values |
| UUID | Text box with format validation and a Generate action |
| JSON / JSONB | Popup editor with validation, formatting, and a tree view. Raw text is preserved — never re-serialised |
| Array | Chip editor, one chip per element |
| Bytea | Read-only preview + save to file + load from file. Never inline text editing |
| Composite / vector / unknown | Read-only in MVP 1, with a note saying why |

Every editor supports `Ctrl+Shift+N` to set NULL, and `Escape` to abandon the edit.

### Pagination

Server-side `LIMIT`/`OFFSET` by default with a configurable page size; **keyset
pagination** when the sort is over a unique index, because `OFFSET` on page 5,000 of a
large table is pathological. The footer states which mode is active.

### Footer

Range and estimated total, pending-change count, and Preview SQL / Discard / Commit.
Commit routes through the safety engine.

---

## 5. States

| State | Rendering |
|---|---|
| **Loading** | Skeleton rows with real column headers (headers arrive first) |
| **Empty (table)** | "This table has no rows" + Insert row |
| **Empty (filter)** | "No rows match the filter" + Clear filter + Edit filter |
| **Not updatable** | Read-only banner naming the reason: no primary key, no unique index, or a multi-table result. Offers "edit by ctid" with an explicit stability warning |
| **Permission denied** | Names the privilege and shows the `GRANT` that would fix it |
| **Error** | Inline error strip with SQLSTATE, message and Retry; existing rows stay visible |
| **Truncated** | "Showing first 50,000 rows" with Load more and Export all |
| **Uncommitted changes** | Tab badge, footer count, coloured row states, and a prompt on close |

---

## 6. Interactions and keyboard

See `../09-keyboard-map.md` section 5 for the full grid map. Surface-specific rules:

- `Enter`/`F2` edits; `Tab` commits the cell and moves right; `Enter` commits and moves
  down; `Escape` abandons the cell edit without touching the pending set.
- `Ctrl+Z` undoes one **pending** change (never a database undo — the distinction is
  stated in the tooltip).
- Paste from a spreadsheet fills a range, matching by shape, and validates every cell
  before applying; conflicts are shown as `data.invalid` with per-cell reasons.
- Clicking an FK value opens the referenced row in a new document tab, filtered to it.
- Sorting or filtering with pending changes prompts, because both re-query.

---

## 7. Data contract

```csharp
public interface ITableDataService
{
    Task<Result<TablePage>> GetPageAsync(
        IDatabaseSession session, TablePageRequest request, CancellationToken ct);

    Task<Result<long>> CountExactAsync(
        IDatabaseSession session, DbObjectRef table, FilterNode? filter, CancellationToken ct);

    Task<Result<RowIdentityStrategy>> ResolveIdentityAsync(
        IDatabaseSession session, DbObjectRef table, CancellationToken ct);
}

public sealed record TablePageRequest(
    DbObjectRef Table, FilterNode? Filter, IReadOnlyList<SortSpec> Sort,
    int Offset, int Limit, object?[]? KeysetCursor);

public sealed record TablePage(
    IReadOnlyList<ResultColumn> Columns, IReadOnlyList<object?[]> Rows,
    long? EstimatedTotal, bool HasMore, object?[]? NextCursor,
    RowIdentityStrategy Identity, PaginationMode Mode);

public enum RowIdentityStrategy { PrimaryKey, UniqueIndex, Ctid, None }
public enum PaginationMode { Offset, Keyset }

public interface IChangeSetService
{
    ChangeSet Current { get; }
    void Stage(PendingChange change);
    void Unstage(Guid changeId);
    void Clear();
    Result<IReadOnlyList<string>> GenerateSql(ChangeSet set);
    Task<Result<CommitReport>> CommitAsync(
        IDatabaseSession session, ChangeSet set, CancellationToken ct);
}
```

Buildable against `FakeTableDataService` producing synthetic rows of every
`PgTypeCategory`.

---

## 8. Acceptance criteria

- [ ] 1M-row table scrolls at 60 fps with constant memory.
- [ ] Column headers render before data; the grid never blocks on the row fetch.
- [ ] `NULL` is visually distinct from an empty string in every cell type.
- [ ] Every `PgTypeCategory` has a working editor or an explicit read-only reason.
- [ ] JSONB round-trips byte-identical when untouched.
- [ ] Modified, inserted and deleted rows are distinguished by colour **and** glyph.
- [ ] Nothing is written to the database until Commit.
- [ ] Commit runs in one transaction; failure rolls back and preserves all pending edits.
- [ ] Optimistic concurrency: a row changed by someone else reports 0 rows affected and
      does not overwrite.
- [ ] Tables without a key are read-only with a clear reason and a ctid opt-in.
- [ ] Keyset pagination engages when sorting on a unique index; the mode is stated.
- [ ] Paste from a spreadsheet validates before applying and reports per-cell failures.
- [ ] Commit on a Production connection routes through the safety ladder.
- [ ] All eight states in section 5 render in both themes.
