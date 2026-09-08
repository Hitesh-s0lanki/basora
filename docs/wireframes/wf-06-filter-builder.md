# wf-06 — Advanced Filter Builder

> Task: `T-D07`. Popover panel anchored to the filter bar in
> [wf-05](wf-05-table-document.md); also used by [wf-09](wf-09-result-viewer.md).

---

## 1. Purpose

Build a `WHERE` clause without writing SQL, and show the SQL it produces. The one job it
must do well: **stay honest** — the visual builder and the generated SQL are the same
AST, so what the user sees is exactly what runs.

---

## 2. Entry points

- Click the filter bar or `Ctrl+Shift+F` in a table document or result viewer.
- Right-click a cell > "Filter by this value" (creates a one-condition filter).
- Column header menu > Filter.

---

## 3. Layout

```text
+-------------------------------------------------------------------+
|  Filter: public.users                                        [X]  |
+-------------------------------------------------------------------+
|  Match  (o) All (AND)   ( ) Any (OR)                              |
|                                                                   |
|  +-------------------------------------------------------------+  |
|  | [ email      v] [ contains        v] [ @gmail.com    ] [ x ] |  |
|  | [ created_at v] [ is after        v] [ 2026-01-01 [c]] [ x ] |  |
|  | [ status     v] [ is              v] [ active      v] [ x ] |  |
|  |                                                             |  |
|  |  v GROUP  (Any)                                       [ x ] |  |
|  |    | [ plan  v] [ is        v] [ pro   v]            [ x ]  |  |
|  |    | [ plan  v] [ is        v] [ team  v]            [ x ]  |  |
|  |    [ + condition ]                                          |  |
|  +-------------------------------------------------------------+  |
|                                                                   |
|  [ + Condition ]  [ + Group ]                                     |
|                                                                   |
|  +-- Generated SQL ------------------------------------------- +  |
|  | WHERE email ILIKE '%' || $1 || '%'                          |  |
|  |   AND created_at > $2                                        |  |
|  |   AND status = $3                                            |  |
|  |   AND (plan = $4 OR plan = $5)                               |  |
|  +--------------------------------------------------------------+  |
|                                                                   |
|  [ Save as... ]  [ Recent v ]              [ Clear ] [ Apply ]    |
+-------------------------------------------------------------------+
```

---

## 4. Component inventory

### Condition row

Three controls plus a remove button:

1. **Column** — searchable dropdown of the table's columns, showing the type glyph and
   name. Grouped: indexed columns first, with an index badge, because filtering on an
   indexed column is the difference between instant and painful.
2. **Operator** — the list is filtered by the column's `PgTypeCategory` (section 5 of
   `../04-domain-model.md`). Showing `BETWEEN` for a boolean is how these builders lose
   trust.
3. **Value** — the editor matches the type: text box, number box, date picker, enum
   dropdown, boolean tri-state, chip list for `IN`, two fields for `BETWEEN`, and no
   value field at all for `IS NULL` / `IS NOT NULL`.

### Operator availability

| Category | Operators |
|---|---|
| Text | `=`, `!=`, contains, starts with, ends with, `LIKE`, `ILIKE`, `IN`, `IS NULL`, `IS NOT NULL` |
| Numeric | `=`, `!=`, `<`, `<=`, `>`, `>=`, `BETWEEN`, `IN`, `IS NULL`, `IS NOT NULL` |
| Boolean | is true, is false, `IS NULL`, `IS NOT NULL` |
| DateTime | `=`, before, after, `BETWEEN`, relative (last 7 days, today, this month), `IS NULL` |
| Enum | `=`, `!=`, `IN`, `IS NULL` |
| Uuid | `=`, `!=`, `IN`, `IS NULL` |
| Json | has key, contains, path equals, `IS NULL` |
| Array | contains, contained by, overlaps, length, `IS NULL` |
| Binary / Composite / Unknown | `IS NULL`, `IS NOT NULL` only |

### Groups

Nestable to three levels (deeper is a sign the user wants the SQL editor, and the UI says
so). A group has its own AND/OR toggle. Rows and groups are drag-reorderable.

### Generated SQL panel

Live-updates on every change. Read-only, monospace, syntax-highlighted, copyable.
**Values render as `$1`, `$2` placeholders with a hover showing the bound value** — this
makes it visible that we parameterise rather than interpolate.

An "Edit as SQL" action converts to a raw text predicate, which is one-way: once edited
as SQL, the visual builder shows the expression read-only with a "back to builder"
action that discards the manual edit. Round-tripping arbitrary SQL back into an AST is a
trap; we do not pretend to do it.

### Saved and recent filters

Save with a name, scoped to the table, listed in the `[v]` dropdown on the filter bar.
The last ten filters per table are kept automatically.

---

## 5. States

| State | Rendering |
|---|---|
| **Loading** | Column list skeleton while the table descriptor loads |
| **Empty** | One blank condition row, column dropdown focused |
| **Invalid condition** | Row border `data.invalid`, message beneath naming the problem ("'abc' is not a valid integer"); Apply disabled |
| **No columns available** | Only if the descriptor failed to load: error strip with Retry |
| **Manual SQL mode** | Builder collapses to a read-only summary; a banner explains the one-way switch |
| **Applied** | Panel closes; the filter bar shows a human-readable summary and a condition count |
| **Query error on apply** | Panel reopens with the server error attached to the offending condition where it can be attributed, otherwise at the top |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Ctrl+Shift+F` | Open / close |
| `Enter` | Apply |
| `Escape` | Close without applying |
| `Alt+N` | Add condition |
| `Alt+G` | Add group |
| `Alt+Delete` | Remove focused condition |
| `Tab` | Move through column, operator, value, remove |

Every control is reachable by keyboard, and the generated SQL panel is focusable and
screen-reader readable.

---

## 7. Data contract

```csharp
public interface IFilterSqlBuilder
{
    Result<ParameterisedSql> Build(FilterNode filter, TableDescriptor table);
    string Describe(FilterNode filter);              // human-readable summary
}

public sealed record ParameterisedSql(string WhereClause, IReadOnlyList<QueryParameter> Parameters);

public interface IFilterOperatorCatalog
{
    IReadOnlyList<FilterOperator> GetOperators(PgTypeCategory category);
    bool RequiresValue(FilterOperator op);
    int ValueCount(FilterOperator op);               // 0 for IS NULL, 2 for BETWEEN
}

public interface ISavedFilterStore
{
    Task<IReadOnlyList<SavedFilter>> GetForTableAsync(DbObjectRef table, CancellationToken ct);
    Task SaveAsync(SavedFilter filter, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
```

Consumes `TableDescriptor` for the column list; needs **no live database** to build or
test. The `IFilterSqlBuilder` golden-file tests are the primary correctness gate.

---

## 8. Acceptance criteria

- [ ] Operator list is filtered by column type; no nonsensical pairing is offerable.
- [ ] Value editor matches the column type, including enum dropdowns from real labels.
- [ ] Indexed columns are grouped first and badged.
- [ ] Groups nest to three levels with independent AND/OR.
- [ ] Generated SQL updates live and shows `$n` placeholders with hover values.
- [ ] **Every value is a bound parameter.** A test asserts that a value containing
      `'; DROP TABLE users; --` produces a parameter, not interpolated text.
- [ ] Invalid values block Apply with a specific inline message.
- [ ] "Edit as SQL" is explicitly one-way and says so.
- [ ] Filters save, reload and appear in the recent list per table.
- [ ] Golden-file tests cover every operator in every category.
- [ ] Full keyboard operation; all seven states render in both themes.
