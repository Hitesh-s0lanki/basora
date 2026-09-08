# Epic 03 — Data Grid and Editing

**This epic is the critical path.** `T-D01 -> T-D02 -> T-D03 -> T-D05 -> T-D06` is the
longest dependency chain in MVP 1. Staff it first.

Spec: [wf-05](../wireframes/wf-05-table-document.md), [wf-06](../wireframes/wf-06-filter-builder.md),
[wf-07](../wireframes/wf-07-pending-changes.md), [04-domain-model](../04-domain-model.md) sections 5–7.

---

### T-D01 — Virtualised data grid control

**Wave:** 2 | **Size:** L | **Spec:** [wf-05](../wireframes/wf-05-table-document.md) section 4, [07-design-system](../07-design-system.md) section 8

**Depends on:** T-F06, T-U01
**Blocks:** T-D03, T-Q07

> Start this as early as possible. It is built against a fake row source and does not
> need any database work to exist.

**Owns:** `src/Basora.UI/Controls/DataGrid/**`, `tests/Basora.UI.Tests/DataGrid/**`

**Deliverables**
- Row and column virtualisation with constant realised-container count.
- Sticky, resizable, reorderable headers with type glyphs and sort indicators.
- Cell, range, row and column selection with full keyboard navigation.
- Frozen leading columns.
- Row-state rendering (modified / inserted / deleted / invalid) by colour **and** glyph.
- `NULL` rendered as a dimmed italic badge, distinct from an empty string.
- Row height driven by `density.row.height`.
- An `IRowSource` abstraction so the control never knows about databases.

**Acceptance**
- [ ] 1,000,000 fake rows scroll at 60 fps with a constant realised-row count.
- [ ] Memory is flat regardless of row count.
- [ ] `NULL` and empty string are visually distinguishable at a glance in every type.
- [ ] All selection modes work by keyboard alone.
- [ ] Column resize, reorder and freeze persist through the host's state.
- [ ] Renders correctly in both themes at all three densities.

---

### T-D02 — Table data service and pagination

**Wave:** 3 | **Size:** L | **Spec:** [wf-05](../wireframes/wf-05-table-document.md) section 7, [05-postgresql](../05-postgresql-data-layer.md) section 4

**Depends on:** T-M01, T-Q04
**Blocks:** T-D03, T-D05

**Owns:** `src/Basora.PostgreSQL/Queries/TableDataService*`,
`tests/Basora.PostgreSQL.Tests/TableData/**`

**Deliverables**
- `ITableDataService` with offset and keyset pagination, selecting keyset automatically
  when sorting on a unique index.
- `ResolveIdentityAsync` implementing the row-identity ladder: primary key, then unique
  non-null index, then `ctid`, then none.
- Exact-count on demand, separate from the estimate.
- Filter and sort applied server-side through `IFilterSqlBuilder`.

**Acceptance**
- [ ] Keyset pagination engages on a unique-index sort and is dramatically faster than
      offset at page 5,000 — asserted by a timing test.
- [ ] Identity resolution returns the correct strategy for: keyed table, unique-index-only
      table, no-key table, and a multi-table join result.
- [ ] Row estimates are labelled as estimates; exact count is opt-in.
- [ ] All filters are parameterised — asserted with an injection-shaped value.

---

### T-D03 — Table document

**Wave:** 4 | **Size:** L | **Spec:** [wf-05](../wireframes/wf-05-table-document.md)

**Depends on:** T-D01, T-D02, T-U03, T-U04, T-F08
**Blocks:** T-D04, T-D05, T-D08

**Owns:** `src/Basora.UI/Views/Table/**`, `src/Basora.UI/ViewModels/Table/**`,
`tests/Basora.UI.Tests/Table/**`

**Deliverables**
- The document shell: header with size and row estimate, sub-tab strip, toolbar, filter
  bar, grid host, pagination footer.
- All eight states from wf-05 section 5.
- Column headers rendered before data arrives.

**Acceptance** — the full checklist in [wf-05 section 8](../wireframes/wf-05-table-document.md),
excluding the editing criteria owned by T-D04/T-D05.

---

### T-D04 — Typed cell editors

**Wave:** 4 | **Size:** L | **Spec:** [wf-05](../wireframes/wf-05-table-document.md) section 4

**Depends on:** T-D03, T-M02
**Blocks:** T-D05

**Owns:** `src/Basora.UI/Controls/CellEditors/**`,
`tests/Basora.UI.Tests/CellEditors/**`

**Deliverables**
- An editor per `PgTypeCategory` per the wf-05 table: text, numeric, boolean tri-state,
  datetime with timezone display, enum dropdown, uuid, JSON popup, array chips, bytea
  read-only with save/load, and explicit read-only for composite/vector/unknown.
- `Ctrl+Shift+N` sets NULL in every editor; `Escape` abandons.
- Per-editor validation with inline messages.

**Acceptance**
- [ ] Every `PgTypeCategory` has an editor or a stated read-only reason.
- [ ] JSONB round-trips byte-identical when opened and closed without edits.
- [ ] Enum dropdowns show real `enumlabel` values in catalog order.
- [ ] `timestamptz` editing respects the timezone-display setting and stores correctly.
- [ ] Invalid input blocks the cell commit with a specific message.
- [ ] Every editor is fully keyboard-operable.

---

### T-D05 — Change set service and row identity

**Wave:** 5 | **Size:** L | **Spec:** [04-domain-model](../04-domain-model.md) section 7, [wf-05](../wireframes/wf-05-table-document.md)

**Depends on:** T-D03, T-D04, T-D02
**Blocks:** T-D06

**Owns:** `src/Basora.Core/Services/Changes/**`,
`tests/Basora.Core.Tests/Changes/**`

**Deliverables**
- `IChangeSetService`: stage, unstage, clear, undo/redo of pending changes.
- Optimistic concurrency: original values captured at edit time and included in the
  `WHERE` clause of updates and deletes.
- The read-only ladder when no row identity exists, with the `ctid` opt-in and its
  stability warning.
- Paste-range staging with per-cell validation before anything is staged.

**Acceptance**
- [ ] Nothing reaches the database from this service — it only stages.
- [ ] Undo/redo of pending changes is correct and is clearly not a database undo.
- [ ] A row modified concurrently produces 0 affected rows rather than an overwrite —
      asserted by a two-session integration test.
- [ ] A table with no key stages nothing and reports the reason.
- [ ] Pasting a range validates every cell before staging any of them.

---

### T-D06 — Change SQL generator and preview screen

**Wave:** 5 | **Size:** L | **Spec:** [wf-07](../wireframes/wf-07-pending-changes.md)

**Depends on:** T-D05, T-P02, T-U07
**Blocks:** T-M04

**Owns:** `src/Basora.Core/Services/Changes/ChangeSqlGenerator*`,
`src/Basora.UI/Views/Changes/**`, `src/Basora.UI/ViewModels/Changes/**`,
`tests/Basora.Core.Tests/Changes/Generator/**`

**Deliverables**
- Pure `IChangeSqlGenerator` producing parameterised statements in safe order (inserts,
  updates, deletes; FK-aware among deletes).
- The preview panel with bidirectional change-to-statement highlighting.
- Transactional commit with full rollback and preserved changes on failure.
- Copy SQL / Save as `.sql` with correctly escaped literal values and a note that the
  saved form is literal-valued.

**Acceptance** — the full checklist in [wf-07 section 8](../wireframes/wf-07-pending-changes.md).
Golden-file coverage of every `PgTypeCategory`, NULL handling, quoted identifiers and
multi-column keys is the primary gate.

---

### T-D07 — Filter AST, SQL builder and builder screen

**Wave:** 5 | **Size:** L | **Spec:** [wf-06](../wireframes/wf-06-filter-builder.md)

**Depends on:** T-M01, T-M02, T-U01, T-F08
**Blocks:** nothing

**Owns:** `src/Basora.Core/Services/Filters/**`, `src/Basora.UI/Views/Filters/**`,
`src/Basora.UI/ViewModels/Filters/**`, `tests/Basora.Core.Tests/Filters/**`

**Deliverables**
- `IFilterSqlBuilder` producing a parameterised `WHERE` clause plus a human-readable
  description from the same AST.
- `IFilterOperatorCatalog` enforcing the operator-per-category table.
- The builder UI with nested groups, typed value editors, live SQL preview with `$n`
  placeholders, saved and recent filters, and the one-way "Edit as SQL" mode.

**Acceptance** — the full checklist in [wf-06 section 8](../wireframes/wf-06-filter-builder.md).
The injection test (a value containing `'; DROP TABLE users; --` must become a parameter)
is a hard gate.

---

### T-D08 — Value inspector, copy and paste

**Wave:** 5 | **Size:** M | **Spec:** [wf-05](../wireframes/wf-05-table-document.md), [wf-09](../wireframes/wf-09-result-viewer.md) section 4

**Depends on:** T-D03
**Blocks:** nothing

**Owns:** `src/Basora.UI/Controls/ValueInspector/**`,
`src/Basora.UI/Services/Clipboard/**`, `tests/Basora.UI.Tests/Clipboard/**`

**Deliverables**
- Value inspector panel: pretty-printed JSON, hex+ASCII for `bytea`, wrapped long text,
  with type and byte length.
- Copy modes: cells, with headers, as JSON, as `INSERT`, as Markdown table.
- Paste from spreadsheet into a range, shape-matched.

**Acceptance**
- [ ] JSON pretty-printing does not alter the stored raw text.
- [ ] `bytea` shows a correct hex+ASCII dump and offers save-to-file.
- [ ] Copy-as-INSERT produces runnable SQL with correct quoting and escaping.
- [ ] Copy-as-Markdown handles pipes and newlines in values.
- [ ] Pasting a shape mismatch reports it rather than silently truncating.
