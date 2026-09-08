# wf-11 — Table Structure Editor

> Task: `T-M03` (viewer), `T-M04` (editor + DDL generation).
> Sub-tab of [wf-05](wf-05-table-document.md).

---

## 1. Purpose

View and modify a table's columns. The one job it must do well: **never surprise the
user with a table rewrite** — every structural change shows the SQL it will produce and
the lock it will take before anything runs.

Uses the same accumulate-then-review-then-apply model as the data grid.

---

## 2. Entry points

- Structure sub-tab of a table document.
- "Open Structure" from the object explorer context menu.
- "Create table" from a schema node (opens in new-table mode).

---

## 3. Layout

```text
+-----------------------------------------------------------------------------+
| public.users - Structure                    Owner: app - Tablespace: default|
+-----------------------------------------------------------------------------+
| [+ Column] [- Delete] [^] [v]                     [Preview SQL] [Apply] [x] |
+---+------------+---------------------+------+------------------+------------+
| # | Name       | Type                | Null | Default          | Comment    |
+---+------------+---------------------+------+------------------+------------+
| 1 | id         | bigint  [PK][ident] | no   | identity always  | surrogate  |
| 2 | email      | varchar(255)  [UQ]  | no   |                  | login      |
| 3 | name       | text                | yes  |                  |            |
| 4 | status     | user_status  [enum] | no   | 'active'         |            |
| 5 | created_at | timestamptz         | no   | now()            |            |
|!6 | phone      | varchar(20)         | yes  |                  |  <- added  |
+---+------------+---------------------+------+------------------+------------+
|  6 columns - 1 pending change                                               |
+-----------------------------------------------------------------------------+
| ~ ALTER TABLE public."users" ADD COLUMN "phone" varchar(20);                |
|   Lock: ACCESS EXCLUSIVE (brief) - no table rewrite - est. under 1s         |
+-----------------------------------------------------------------------------+
```

The bottom strip is always visible while changes are pending: the generated SQL plus the
**lock level, rewrite verdict and duration estimate** from the migration safety analyzer.

---

## 4. Component inventory

### Column grid

Editable in place. Columns of the grid: ordinal, name, type, nullable, default,
identity/generated, collation, comment. Badges: `PK`, `UQ`, `FK`, `ident`, `gen`,
`enum`, `arr`.

| Field | Editor |
|---|---|
| Name | Text; validates identifier rules and duplicates |
| Type | Searchable dropdown of built-in types, enums, domains and composites from this database, with length/precision/scale sub-fields appearing per type |
| Nullable | Toggle |
| Default | Text with an expression hint; common defaults offered (`now()`, `gen_random_uuid()`, literals) |
| Identity | None / Always / By default |
| Generated | Expression editor, stored only (PG has no virtual generated columns) |
| Comment | Text |

Reordering columns is offered but flagged honestly: PostgreSQL cannot reorder columns
in place, so the generated migration is a table rebuild. The UI says this clearly rather
than silently generating an expensive migration.

### Change accumulation

Additions, edits and deletions are staged, exactly like grid edits: coloured row states,
a pending count, Preview SQL, Apply and Discard. Nothing runs until Apply.

### Preview and safety

Preview opens [wf-07](wf-07-pending-changes.md) in DDL mode, showing the ordered
statements plus, per statement:

- the lock level acquired (from the table in `../10-safety-rules.md` section 4)
- whether the table is rewritten
- the table's current size and row estimate
- a duration estimate where one can be reasoned about
- a **safer alternative** when one exists (nullable + backfill + `SET NOT NULL`;
  `NOT VALID` then `VALIDATE`; new column + swap instead of a type rewrite)

### New-table mode

Same grid, empty, plus name, schema, tablespace, `UNLOGGED`, partitioning strategy and
comment. Generates a single `CREATE TABLE`.

---

## 5. States

| State | Rendering |
|---|---|
| **Loading** | Skeleton rows with real headers |
| **Read-only (no privilege)** | Grid renders, editing disabled, banner naming the missing privilege and the `GRANT` that would fix it |
| **View / matview / foreign table** | Structure shown read-only with the definition, and a note that columns are not directly editable |
| **Pending changes** | Coloured rows, count, bottom SQL strip live-updating |
| **Applying** | Progress per statement; long DDL shows elapsed time and a warning if it exceeds the estimate |
| **Apply failed** | Statement highlighted with SQLSTATE and message; earlier statements in the transaction rolled back; pending changes preserved |
| **Rewrite warning** | A `status.warning` strip whenever a change rewrites the table, stating the table size and that the table will be locked for the duration |
| **Empty (new table)** | One blank column row, name field focused |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Ctrl+Shift+A` | Add column |
| `Delete` | Mark column for deletion |
| `Alt+Up` / `Alt+Down` | Move column (with the rebuild warning) |
| `Ctrl+Shift+P` | Preview SQL |
| `Ctrl+Enter` | Apply |
| `Ctrl+Z` | Undo one pending structural change |
| `Escape` | Cancel the current cell edit |
| `F2` | Rename the focused column |

Dropping a column always routes through the safety ladder; on Production it is
`TypeToConfirm`. Renaming shows an explicit warning that dependent application code and
views will break, and lists the dependent views it can find.

---

## 7. Data contract

```csharp
public interface IStructureEditorService
{
    Result<GeneratedStatements> GenerateDdl(
        TableDescriptor original, TableStructureDraft draft);

    Task<Result<StructureApplyReport>> ApplyAsync(
        IDatabaseSession session, GeneratedStatements statements, CancellationToken ct);
}

public sealed record TableStructureDraft(
    DbObjectRef Ref, IReadOnlyList<ColumnDraft> Columns,
    string? Comment, string? Tablespace, bool IsUnlogged);

public sealed record ColumnDraft(
    Guid Id, string Name, PgType Type, bool IsNullable, string? Default,
    IdentityKind Identity, string? GeneratedExpression, string? Comment,
    ColumnChangeKind Change);

public enum ColumnChangeKind { Unchanged, Added, Modified, Dropped, Reordered }

public interface ITypeCatalog
{
    Task<Result<IReadOnlyList<PgType>>> GetAvailableTypesAsync(
        IDatabaseSession session, CancellationToken ct);
}

public interface IMigrationSafetyAnalyzer
{
    Task<Result<MigrationImpact>> AnalyzeAsync(
        IDatabaseSession session, GeneratedStatement statement, CancellationToken ct);
}

public sealed record MigrationImpact(
    string LockLevel, bool RewritesTable, bool BlocksReads, bool BlocksWrites,
    long? TableSizeBytes, long? EstimatedRows, TimeSpan? EstimatedDuration,
    RiskLevel Risk, IReadOnlyList<SafetyFinding> Findings, string? SaferAlternativeSql);
```

DDL generation is pure and lives in `Basora.Core` with golden-file tests.

---

## 8. Acceptance criteria

- [ ] All column properties render correctly, including identity, generated, collation
      and comments.
- [ ] The type dropdown includes enums, domains and composites from the live database.
- [ ] Changes accumulate; nothing runs until Apply.
- [ ] Generated DDL is correct for add, drop, rename, type change, nullability change,
      default change, identity change and comment change.
- [ ] Every statement shows its lock level and rewrite verdict before Apply.
- [ ] A rewriting change shows a warning with the table size.
- [ ] Safer alternatives are offered for `NOT NULL` with default, FK addition, and type
      rewrites.
- [ ] Column reordering states plainly that it requires a table rebuild.
- [ ] Rename warns and lists dependent views.
- [ ] Drop column routes through the safety ladder (`TypeToConfirm` on Production).
- [ ] Failed apply rolls back and preserves pending changes.
- [ ] Read-only for views, matviews and foreign tables, with an explanation.
- [ ] Golden-file tests cover every DDL shape above.
- [ ] All eight states render in both themes.
