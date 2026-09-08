# Epic 02 — Explorer and Schema Objects

`T-E*` (catalog access and the tree) and `T-M*` (object descriptors, structure, indexes).

Spec: [05-postgresql-data-layer](../05-postgresql-data-layer.md) section 3,
[wf-04](../wireframes/wf-04-object-explorer.md), [wf-11](../wireframes/wf-11-structure-editor.md),
[wf-12](../wireframes/wf-12-indexes-constraints.md).

---

### T-E01 — Catalog query library

**Wave:** 3 | **Size:** L | **Spec:** [05-postgresql](../05-postgresql-data-layer.md) section 3

**Depends on:** T-C01, T-F05
**Blocks:** T-E02, T-E04, T-M01

**Owns:** `src/Basora.PostgreSQL/Metadata/Sql/**`,
`src/Basora.PostgreSQL/Metadata/Readers/**`,
`tests/Basora.PostgreSQL.Tests/Metadata/**`

**Deliverables**
- All 16 embedded `.sql` files from the query inventory, each with a header comment
  stating purpose and minimum server version.
- A reader per query mapping rows to `Basora.Core` descriptors.
- Server-side filtering by schema privilege (`has_schema_privilege`) in every query.
- Result bounding and pagination for catalogs on very large databases.

**Acceptance**
- [ ] Every query runs correctly on PostgreSQL 13, 15, 17 and latest.
- [ ] All queries use `pg_catalog`, schema-qualified, never relying on `search_path`.
- [ ] The nasty seed schema from `11-testing-strategy.md` section 4 is fully and
      correctly described — partitions, generated columns, identity, enums, domains,
      composites, arrays, ranges, partial/expression/INCLUDE indexes, deferrable and
      NOT VALID constraints, self- and multi-column FKs.
- [ ] A role with limited privileges gets empty lists, not exceptions.
- [ ] A 5,000-relation schema lists in under 500 ms.
- [ ] `reltuples` of `-1` is surfaced as unknown, never as "-1 rows".

---

### T-E02 — Metadata service and cache

**Wave:** 3 | **Size:** M | **Spec:** [05-postgresql](../05-postgresql-data-layer.md) section 3

**Depends on:** T-E01
**Blocks:** T-E03, T-Q02, T-M03

**Owns:** `src/Basora.PostgreSQL/Metadata/MetadataService*`,
`src/Basora.PostgreSQL/Metadata/MetadataCache*`,
`tests/Basora.PostgreSQL.Tests/MetadataCache*`

**Deliverables**
- `IMetadataService` over the readers, always using the metadata channel.
- `IMetadataCache` keyed by `(SessionId, DbObjectRef, Aspect)` with age tracking.
- Invalidation on: explicit refresh, any DDL executed through Basora, and a background
  revalidation of the expanded subtree only.
- Request coalescing so ten simultaneous expansions of one node issue one query.

**Acceptance**
- [ ] Cache never crosses sessions.
- [ ] A DDL statement executed through Basora invalidates the affected scope.
- [ ] Concurrent identical requests are coalesced into one server round trip.
- [ ] Cache age is reported accurately to the UI.
- [ ] Metadata queries never run on the query channel.

---

### T-E03 — Object explorer tree

**Wave:** 4 | **Size:** L | **Spec:** [wf-04](../wireframes/wf-04-object-explorer.md)

**Depends on:** T-E02, T-U01, T-U03, T-F08
**Blocks:** nothing

**Owns:** `src/Basora.UI/Views/Explorer/**`, `src/Basora.UI/ViewModels/Explorer/**`,
`tests/Basora.UI.Tests/Explorer/**`

**Deliverables** — the tree exactly as specified in wf-04: virtualised, lazily loaded,
filterable, with per-kind context menus and persisted expansion state.

**Acceptance** — the full checklist in [wf-04 section 8](../wireframes/wf-04-object-explorer.md).
Non-negotiable: 5,000 tables at 60 fps against `FakeMetadataService`.

---

### T-E04 — Server-side object search

**Wave:** 4 | **Size:** M | **Spec:** [wf-15](../wireframes/wf-15-command-palette.md) section 4

**Depends on:** T-E01
**Blocks:** T-U06

**Owns:** `src/Basora.PostgreSQL/Metadata/ObjectSearch*`,
`src/Basora.Core/Services/Search/**`, `tests/Basora.Core.Tests/Search/**`

**Deliverables**
- `search_objects.sql` — one ranked query across relations, columns and functions.
- `IFuzzyMatcher` (pure) with subsequence matching and match-index reporting.
- The ranking function from wf-15 section 4, with an MRU/frequency store.

**Acceptance**
- [ ] `ordit` matches `order_items` with correct match indices for highlighting.
- [ ] Ranking follows the documented order, verified by fixture tests.
- [ ] Server search on 50,000 objects returns the top 50 in under 300 ms.
- [ ] Recency and frequency demonstrably promote repeatedly opened objects.

---

### T-M01 — Table descriptor assembly

**Wave:** 3 | **Size:** M | **Spec:** [04-domain-model](../04-domain-model.md) section 4

**Depends on:** T-E01
**Blocks:** T-M03, T-M05, T-D02, T-D07

**Owns:** `src/Basora.PostgreSQL/Metadata/TableDescriptorBuilder*`,
`tests/Basora.PostgreSQL.Tests/TableDescriptor*`

**Deliverables**
- Assembly of a complete `TableDescriptor` from the column, index, constraint, FK,
  trigger and statistics readers, in a bounded number of round trips.
- Both FK directions populated.
- Internal FK triggers excluded from the trigger list.

**Acceptance**
- [ ] A full descriptor for a table with 200 columns and 20 indexes builds in under
      300 ms.
- [ ] Incoming and outgoing FKs are both correct, including self-references.
- [ ] Primary-key column order is preserved.
- [ ] Golden-file tests over the seed schema cover every descriptor field.

---

### T-M02 — Type catalog and PgType mapping

**Wave:** 2 | **Size:** M | **Spec:** [05-postgresql](../05-postgresql-data-layer.md) section 7

**Depends on:** T-F05
**Blocks:** T-D04, T-D07, T-M04

**Owns:** `src/Basora.PostgreSQL/Metadata/TypeCatalog*`,
`src/Basora.Core/Services/Types/**`, `tests/Basora.Core.Tests/Types/**`

**Deliverables**
- The full type-mapping table from the spec as data: PostgreSQL type to .NET type to
  `PgTypeCategory`.
- `ITypeCatalog` fetching enums, domains and composites from a live database.
- Display formatting per category, including the timezone-display setting for
  `timestamptz` and the `NULL` badge rule.

**Acceptance**
- [ ] Every type in the spec table maps to the correct category.
- [ ] An unmapped or custom type degrades to text without throwing.
- [ ] Enum values come from `enumlabel` in the correct sort order.
- [ ] Domain types resolve to their underlying type and carry the constraint hint.
- [ ] `timestamptz` formatting honours all three timezone-display modes.

---

### T-M03 — Structure viewer

**Wave:** 4 | **Size:** M | **Spec:** [wf-11](../wireframes/wf-11-structure-editor.md) (read-only half)

**Depends on:** T-M01, T-M02, T-U01, T-F08
**Blocks:** T-M04

**Owns:** `src/Basora.UI/Views/Structure/StructureView*`,
`src/Basora.UI/ViewModels/Structure/StructureViewModel*`,
`tests/Basora.UI.Tests/Structure/**`

**Deliverables** — the column grid, header and read-only states from wf-11, plus the SQL
tab showing the object's `CREATE` statement.

**Acceptance**
- [ ] All column properties render, including identity, generated, collation, comment.
- [ ] Views, matviews and foreign tables render read-only with an explanation.
- [ ] Permission-denied shows the missing privilege and the `GRANT`.
- [ ] Loading, empty, error and stale states render in both themes.

---

### T-M04 — Structure editor and DDL generator

**Wave:** 5 | **Size:** L | **Spec:** [wf-11](../wireframes/wf-11-structure-editor.md) (write half)

**Depends on:** T-M03, T-M02, T-P05, T-D06
**Blocks:** nothing

**Owns:** `src/Basora.Core/Services/Ddl/**`,
`src/Basora.UI/ViewModels/Structure/StructureEditor*`,
`tests/Basora.Core.Tests/Ddl/**`

**Deliverables**
- Pure DDL generation for add, drop, rename, type change, nullability, default, identity,
  generated and comment changes, plus `CREATE TABLE`.
- Change accumulation with the same staged model as the data grid.
- Integration with `IMigrationSafetyAnalyzer` so every statement shows its lock level and
  rewrite verdict before Apply.

**Acceptance** — the full checklist in [wf-11 section 8](../wireframes/wf-11-structure-editor.md).
Golden-file tests are the primary gate.

---

### T-M05 — Indexes, constraints, relations and triggers

**Wave:** 4 | **Size:** M | **Spec:** [wf-12](../wireframes/wf-12-indexes-constraints.md)

**Depends on:** T-M01, T-U01, T-F08
**Blocks:** T-M06

**Owns:** `src/Basora.UI/Views/Objects/**`, `src/Basora.UI/ViewModels/Objects/**`,
`tests/Basora.UI.Tests/Objects/**`

**Deliverables** — the four tabs from wf-12, plus the create-index and add-constraint
dialogs with `CONCURRENTLY` and `NOT VALID` defaults.

**Acceptance** — the full checklist in [wf-12 section 8](../wireframes/wf-12-indexes-constraints.md).

---

### T-M06 — Index analyzer rules

**Wave:** 5 | **Size:** M | **Spec:** [wf-12](../wireframes/wf-12-indexes-constraints.md) section 4

**Depends on:** T-M05
**Blocks:** T-N05 (MVP 2)

**Owns:** `src/Basora.Analytics/IndexAnalyzer/**`,
`tests/Basora.Analytics.Tests/IndexAnalyzer/**`

**Deliverables**
- Pure rules for unused, duplicate, redundant, invalid, bloated and never-analyzed
  indexes.
- Every finding carries the statistics-reset context, and findings are suppressed rather
  than guessed when statistics are unavailable.
- Reclaimable-size calculation for ranking.

**Acceptance**
- [ ] Each rule is unit-tested against fixture descriptors and statistics.
- [ ] A recent statistics reset downgrades unused findings to informational.
- [ ] Missing statistics suppress dependent findings entirely.
- [ ] Redundant-prefix detection handles multi-column and `INCLUDE` indexes correctly.
- [ ] No rule ever produces an auto-executable drop.
