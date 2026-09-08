# Epic 05 — History, Favorites, Import and Export

`T-H*` and `T-X*`. Grouped because both are local-storage and file-I/O work with no
overlap on any other epic's files — this epic is the easiest to run fully in parallel.

Spec: [wf-10](../wireframes/wf-10-history-favorites.md), [wf-13](../wireframes/wf-13-import-wizard.md),
[wf-14](../wireframes/wf-14-export-dialog.md), [05-postgresql-data-layer](../05-postgresql-data-layer.md) section 8.

---

### T-H01 — Query history store

**Wave:** 3 | **Size:** M | **Spec:** [wf-10](../wireframes/wf-10-history-favorites.md) section 7

**Depends on:** T-F10
**Blocks:** T-H03

**Owns:** `src/Basora.Infrastructure/History/**`,
`tests/Basora.Infrastructure.Tests/History/**`

**Deliverables**
- `IHistoryStore` over SQLite (decision to be recorded in an ADR by this task — SQLite is
  the recommendation, for indexed search over 100k+ entries).
- Full-text search plus filters by connection, environment, outcome, duration and date.
- Deduplication of identical consecutive statements with a run count.
- Retention by age and count; per-connection opt-out; pattern-based redaction.
- Fire-and-forget recording that can never delay or fail a query.

**Acceptance**
- [ ] Search over 100,000 entries returns in under 100 ms.
- [ ] Result values are never stored — asserted by schema inspection and a test.
- [ ] Consecutive identical statements collapse with a correct run count.
- [ ] A store failure logs and surfaces in the panel but never propagates to the query.
- [ ] Redaction patterns suppress matching statements entirely.
- [ ] Retention pruning is correct and bounded in runtime.

---

### T-H02 — Favorites store

**Wave:** 3 | **Size:** S | **Spec:** [wf-10](../wireframes/wf-10-history-favorites.md) section 7

**Depends on:** T-F10
**Blocks:** T-H03

**Owns:** `src/Basora.Infrastructure/Favorites/**`,
`tests/Basora.Infrastructure.Tests/Favorites/**`

**Deliverables**
- `IFavoritesStore` with JSON persistence: folders, tags, descriptions, optional
  connection binding.
- Atomic write with defaults on corruption.
- Import and export of a favorites file for sharing between machines.

**Acceptance**
- [ ] Folders nest and reorder correctly and survive a restart.
- [ ] A connection-bound favorite appears only for that connection; unbound ones appear
      everywhere.
- [ ] A corrupt file loads as empty without overwriting.
- [ ] Exported favorites contain no connection secrets.

---

### T-H03 — History and favorites panels

**Wave:** 4 | **Size:** M | **Spec:** [wf-10](../wireframes/wf-10-history-favorites.md)

**Depends on:** T-H01, T-H02, T-U01, T-U03, T-F08
**Blocks:** nothing

**Owns:** `src/Basora.UI/Views/History/**`, `src/Basora.UI/ViewModels/History/**`,
`tests/Basora.UI.Tests/History/**`

**Deliverables** — both panels exactly as specified in wf-10, plus the in-editor
`Ctrl+Up` / `Ctrl+Down` history cycling and the `Ctrl+D` save popover.

**Acceptance** — the full checklist in [wf-10 section 8](../wireframes/wf-10-history-favorites.md).

---

### T-X01 — Export service and format writers

**Wave:** 3 | **Size:** L | **Spec:** [wf-14](../wireframes/wf-14-export-dialog.md) section 7, [05-postgresql](../05-postgresql-data-layer.md) section 8

**Depends on:** T-Q04
**Blocks:** T-X05

**Owns:** `src/Basora.Core/Services/Export/**`,
`src/Basora.PostgreSQL/Copy/CopyExporter*`,
`tests/Basora.Core.Tests/Export/**`

**Deliverables**
- `IExportFormat` implementations: CSV, TSV, JSON (array and JSONL), SQL (`INSERT` and
  `COPY`), Excel (streaming writer), Markdown, Text.
- Server-side streaming for whole-table export via `COPY (SELECT ...) TO STDOUT`.
- CSV formula-injection neutralisation for leading `=`, `+`, `-`, `@`.
- DI registration so formats are pluggable.

**Acceptance**
- [ ] A 10M-row export keeps memory flat and never materialises the result.
- [ ] SQL export produces runnable statements: correct identifier quoting, literal
      escaping, and correct handling of NULL, arrays, `bytea` and `jsonb`.
- [ ] Excel export streams; a 1M-row export does not build the workbook in memory.
- [ ] Formula neutralisation is applied by default and is verifiable.
- [ ] JSON export preserves raw `jsonb` text.
- [ ] Cancellation stops promptly and the partial file is deleted.

---

### T-X02 — Import detection and validation

**Wave:** 3 | **Size:** L | **Spec:** [wf-13](../wireframes/wf-13-import-wizard.md) steps 1 and 4

**Depends on:** T-M02
**Blocks:** T-X03, T-X04

**Owns:** `src/Basora.Core/Services/Import/**`,
`tests/Basora.Core.Tests/Import/**`

**Deliverables**
- Format detection: CSV/TSV/JSON/JSONL/SQL, encoding (with BOM check and a confidence
  score), delimiter, quote, escape, header row, line ending, null representation.
- Type inference for "create table from file".
- Full-file streaming validation producing `ImportProblem` entries with **file line
  number**, column, value and reason — without writing anything.
- Value transforms: trim, empty-to-NULL, date formats, boolean interpretations, number
  locale.

**Acceptance**
- [ ] Detection is correct across a corpus of real-world CSV dialects, including
      semicolon-delimited European files, CRLF, quoted newlines and BOM-prefixed UTF-8.
- [ ] Low-confidence detection is reported as uncertain rather than guessed silently.
- [ ] Validation streams a 500 MB file with flat memory and is cancellable.
- [ ] Every problem reports the correct file line number, including files with quoted
      embedded newlines.
- [ ] Type inference produces a sensible, editable `CREATE TABLE`.

---

### T-X03 — Import wizard screen

**Wave:** 5 | **Size:** L | **Spec:** [wf-13](../wireframes/wf-13-import-wizard.md)

**Depends on:** T-X02, T-X04, T-U01, T-U03, T-F08
**Blocks:** nothing

**Owns:** `src/Basora.UI/Views/Import/**`, `src/Basora.UI/ViewModels/Import/**`,
`tests/Basora.UI.Tests/Import/**`

**Deliverables** — all seven wizard steps exactly as specified in wf-13, with state
preserved when stepping back.

**Acceptance** — the full checklist in [wf-13 section 8](../wireframes/wf-13-import-wizard.md).

---

### T-X04 — COPY import execution

**Wave:** 5 | **Size:** M | **Spec:** [05-postgresql](../05-postgresql-data-layer.md) section 8

**Depends on:** T-X02, T-C01
**Blocks:** T-X03

**Owns:** `src/Basora.PostgreSQL/Copy/CopyImporter*`,
`tests/Basora.PostgreSQL.Tests/Copy/**`

**Deliverables**
- `BeginBinaryImportAsync` writer with the mapped column list.
- Transaction wrapping, batch progress reporting, and cancellation with rollback.
- `ON CONFLICT` handling, identity override, and optional trigger disabling.
- Error attribution back to the source file line.

**Acceptance**
- [ ] A 500 MB CSV imports with flat memory.
- [ ] Cancellation mid-import rolls back completely, leaving zero rows.
- [ ] Progress reporting is accurate to within one batch.
- [ ] A conversion failure at row N reports file line N with the column and value.
- [ ] Truncate-first routes through the safety ladder.
- [ ] The import is recorded in the audit log.

---

### T-X05 — Export dialog screen

**Wave:** 5 | **Size:** M | **Spec:** [wf-14](../wireframes/wf-14-export-dialog.md)

**Depends on:** T-X01, T-U01, T-U07, T-F08
**Blocks:** nothing

**Owns:** `src/Basora.UI/Views/Export/**`, `src/Basora.UI/ViewModels/Export/**`,
`tests/Basora.UI.Tests/Export/**`

**Deliverables** — the dialog exactly as specified in wf-14, including per-format option
panels, the sensitive-column default-off rule, and disk-space pre-check.

**Acceptance** — the full checklist in [wf-14 section 8](../wireframes/wf-14-export-dialog.md).
