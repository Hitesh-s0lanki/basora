# wf-13 — Import Wizard

> Task: `T-X03`. Modal wizard.

---

## 1. Purpose

Get a file into a table without a failed 40-minute import. The one job it must do well:
**validate before writing** — every type error, every unmappable column, every constraint
violation is reported with a file line number before a single row is inserted.

---

## 2. Entry points

- Object explorer: right-click a table > Import Data.
- File > Import, or `Ctrl+Shift+I`.
- Drag a `.csv` / `.json` / `.sql` file onto the window.
- "Create table from file" from a schema node.

---

## 3. Layout

```text
+---------------------------------------------------------------------------+
|  Import Data                                                         [X]  |
+---------------------------------------------------------------------------+
|  (1) Source > (2) Target > (3) Mapping > (4) Validate > (5) Options >     |
|  (6) Preview > (7) Import                                                 |
+---------------------------------------------------------------------------+
|                                                                           |
|  STEP 3 - MAPPING                                                         |
|                                                                           |
|  File column        Sample value          ->   Table column      Type      |
|  ----------------------------------------------------------------------- |
|  email              a@example.com         ->   [ email      v ]  varchar   |
|  full_name          Hitesh Solanki        ->   [ name       v ]  text      |
|  created            2026-01-01            ->   [ created_at v ]  timestamptz|
|  legacy_id          8821                  ->   [ (skip)     v ]            |
|                                                                           |
|  Unmapped table columns:                                                  |
|    id          identity - generated automatically                         |
|    status      not null, default 'active' - will use the default          |
|    org_id      NOT NULL with no default   ~ must be mapped or given a value|
|                                    [ constant value: ______ ]             |
|                                                                           |
+---------------------------------------------------------------------------+
|  [ Back ]                                                    [ Next ]     |
+---------------------------------------------------------------------------+
```

---

## 4. Step-by-step

### Step 1 — Source

File picker plus detection: format (CSV/TSV/JSON/JSONL/SQL), encoding (with a BOM check
and a confidence note), delimiter, quote character, escape character, header row,
line ending, and null representation. Every detected value is shown **as an editable
field**, with the **raw first 20 lines** rendered below so the user can verify the
detection rather than trust it.

### Step 2 — Target

Existing table (searchable picker, pre-filled if launched from a table) or **create new
table from file**, which infers column types from a sample, shows the inferred
`CREATE TABLE` for editing, and lets the user override any type.

### Step 3 — Mapping

Auto-match by name, case- and underscore-insensitive (`full_name` matches `fullName`,
`FULL NAME`). Each file column maps to a table column or `(skip)`. Unmapped **table**
columns are listed with why they are fine (identity, has a default, nullable) or why they
are not (`NOT NULL` with no default), and the latter offers a constant value or blocks
progression.

Per-column transforms: trim, empty-string-to-NULL, date format, boolean interpretation
(`t/f`, `Y/N`, `1/0`), and number locale.

### Step 4 — Validate

Streams the **whole file** through type conversion and constraint checks without writing
anything. Cancellable, with progress. Produces:

```text
  Validated 128,402 rows - 14 problems found

  line 402    created     "01/13/2026"    not a valid timestamptz for the chosen format
  line 1,208  email       ""              violates NOT NULL on email
  line 9,113  email       a@example.com   duplicate of line 22 (unique constraint)
  ...

  On error:  (o) Stop and import nothing
             ( ) Skip bad rows and report them
             ( ) Import valid rows, write bad rows to errors.csv
```

This step is the reason the wizard exists.

### Step 5 — Options

Transaction (default on), truncate target first (off; requires confirmation and routes
through the safety ladder), `ON CONFLICT` behaviour (error / do nothing / update on a
chosen key), batch size, identity handling (respect / override), and disable triggers
during import (off by default; explains the consequence).

### Step 6 — Preview

The first 100 rows exactly as they will land, post-transform, plus the generated `COPY`
statement.

### Step 7 — Import

Progress by rows and bytes, elapsed and estimated remaining, throughput. Cancel triggers
a rollback when in a transaction (and says so). On completion: rows imported, rows
skipped, duration, throughput, a link to the error file if one was written, and a link to
the audit entry.

---

## 5. States

| State | Rendering |
|---|---|
| **No file** | Drop zone plus Browse |
| **Detecting** | Spinner over the raw preview, cancellable |
| **Detection uncertain** | Warning naming the ambiguity (for example an encoding guess below confidence) and asking the user to confirm |
| **Validating** | Progress with a live problem count; problems stream into the list |
| **Validation failed (blocking)** | Next disabled until an on-error policy is chosen |
| **Importing** | Progress; Cancel available; the wizard cannot be closed |
| **Import failed** | Full rollback (when transactional) stated explicitly, with the failing row and reason; the wizard stays open on the Options step |
| **Import partial** | Only possible without a transaction: exact counts of imported, skipped and not-attempted |
| **Complete** | Summary with counts, duration and links |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Alt+Right` / `Alt+Left` | Next / Back |
| `Enter` | Next (when the step is valid) |
| `Escape` | Cancel (confirms during import) |
| `Ctrl+O` | Browse for a file |

Wizard state is preserved when stepping back; changing the source resets mapping with a
confirmation.

---

## 7. Data contract

```csharp
public interface IImportService
{
    Task<Result<FileFormatDetection>> DetectAsync(string path, CancellationToken ct);

    Task<Result<ImportValidationReport>> ValidateAsync(
        ImportPlan plan, IProgress<ImportProgress> progress, CancellationToken ct);

    Task<Result<ImportReport>> ImportAsync(
        IDatabaseSession session, ImportPlan plan,
        IProgress<ImportProgress> progress, CancellationToken ct);
}

public sealed record ImportPlan(
    string FilePath, FileFormatDetection Format, DbObjectRef Target,
    IReadOnlyList<ColumnMapping> Mappings, ImportOptions Options);

public sealed record ColumnMapping(
    string? FileColumn, string TableColumn, PgType TargetType,
    string? ConstantValue, IReadOnlyList<ValueTransform> Transforms);

public sealed record ImportValidationReport(
    long RowsValidated, IReadOnlyList<ImportProblem> Problems, bool Truncated);

public sealed record ImportProblem(
    long LineNumber, string? Column, string? Value, string Reason, ProblemSeverity Severity);

public sealed record ImportProgress(
    long RowsProcessed, long BytesProcessed, long? TotalBytes,
    TimeSpan Elapsed, double RowsPerSecond);
```

Uses `COPY ... FROM STDIN (FORMAT BINARY)` per `../05-postgresql-data-layer.md`
section 8.

---

## 8. Acceptance criteria

- [ ] Format, encoding, delimiter, quoting and header detection work on common CSV
      dialects, and every detected value is user-editable.
- [ ] The raw first 20 lines are shown so detection can be verified.
- [ ] Auto-mapping matches case- and underscore-insensitively.
- [ ] Unmapped `NOT NULL` columns without defaults block progression or take a constant.
- [ ] Validation streams the whole file without writing and is cancellable.
- [ ] Problems report **file line number**, column, value and reason.
- [ ] All three on-error policies work, including writing an errors file.
- [ ] Import uses `COPY`, not row-by-row `INSERT`, and memory stays flat on a 500 MB file.
- [ ] Cancel during a transactional import rolls back completely and says so.
- [ ] Truncate-first routes through the safety ladder.
- [ ] "Create table from file" infers types and shows an editable `CREATE TABLE`.
- [ ] The import is recorded in the audit log.
- [ ] All nine states render in both themes.
