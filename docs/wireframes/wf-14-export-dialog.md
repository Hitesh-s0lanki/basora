# wf-14 — Export Dialog

> Task: `T-X05`. Modal dialog.

---

## 1. Purpose

Get data out, in the format the destination needs, without loading it all into memory.
The one job it must do well: **export a 40 GB table to a file with flat memory usage and
a working cancel.**

---

## 2. Entry points

- `Ctrl+E` in a table document or result viewer.
- Export button in either toolbar.
- Object explorer: right-click a table (or a multi-selection) > Export.
- Command palette: "Export".

---

## 3. Layout

```text
+---------------------------------------------------------------------------+
|  Export                                                              [X]  |
+---------------------------------------------------------------------------+
|  Source     (o) Current result (1,204 rows)                               |
|             ( ) Selected rows (18)                                        |
|             ( ) Entire table public.users (~1.2M rows, 184 MB)            |
|             ( ) Custom query                                              |
|                                                                           |
|  Format     [ CSV                                        v ]              |
|                                                                           |
|  +-- CSV options -------------------------------------------------------+ |
|  |  Delimiter [ , v]   Quote [ " v]   Line ending [ LF v]               | |
|  |  [x] Include header row                                              | |
|  |  NULL as [        ]  (empty means an empty field)                    | |
|  |  Encoding [ UTF-8 v]   [x] Write a BOM                               | |
|  |  [x] Neutralise leading = + - @ (prevents spreadsheet formula        | |
|  |      injection)                                                      | |
|  +----------------------------------------------------------------------+ |
|                                                                           |
|  Columns    [x] id  [x] email  [x] name  [ ] password_hash  [x] created   |
|             [ All ] [ None ] [ Visible only ]                             |
|                                                                           |
|  Destination [ C:\exports\users_2026-09-08.csv          ] [ Browse ]      |
|              ( ) File   ( ) Clipboard   ( ) New SQL editor tab            |
|                                                                           |
|  ~ password_hash is excluded. Review before sharing this file.            |
+---------------------------------------------------------------------------+
|                                                  [ Cancel ]  [ Export ]   |
+---------------------------------------------------------------------------+
```

---

## 4. Component inventory

### Source

Current result, selected rows, entire table, or a custom query. **Entire table streams
server-side via `COPY (SELECT ...) TO STDOUT`** — it does not first load into the grid.
That distinction is stated in the UI, because "export all" on a 40 GB table is otherwise
a memory bomb.

Multi-table export (from a tree multi-selection) writes one file per table, or one SQL
file containing all of them.

### Formats and their options

| Format | Options |
|---|---|
| **CSV / TSV** | Delimiter, quote, escape, line ending, header, NULL representation, encoding, BOM, formula-injection neutralisation |
| **JSON** | Array vs JSONL, pretty vs compact, date format, whether to preserve raw `jsonb` |
| **SQL** | `INSERT` vs `COPY`, include `CREATE TABLE`, `ON CONFLICT` clause, batch size, schema-qualified names, transaction wrapper |
| **Excel (.xlsx)** | Sheet name, header styling, freeze header, column widths, type mapping. Streamed writer — never build the whole workbook in memory |
| **Markdown** | Table format, alignment |
| **Text** | Fixed-width aligned |

### Columns

Checkbox list with All / None / Visible only. **Columns whose names match a sensitive
pattern** (`password`, `secret`, `token`, `ssn`, `card`) are **unchecked by default** and
the dialog says so. This is a small default that prevents a real class of accident.

### Destination

File, clipboard (disabled above a row threshold with an explanation), or a new SQL editor
tab (for the SQL format). Filename is templated with table name and date, and the last
directory is remembered.

### Progress

Rows written, bytes written, throughput, elapsed and estimated remaining. Cancel deletes
the partial file, and says that it did.

---

## 5. States

| State | Rendering |
|---|---|
| **Ready** | Export enabled once a destination is set |
| **Estimating** | Row count and size for the "entire table" option load asynchronously; the option is usable before they arrive |
| **Exporting** | Progress; the dialog cannot be closed; Cancel available |
| **Cancelled** | Partial file deleted; stated explicitly |
| **Failed** | Error with the cause (disk full, permission denied, query error); partial file deleted |
| **Complete** | Summary: rows, file size, duration, with "Open folder" and "Copy path" |
| **Destination exists** | Overwrite confirmation naming the file |
| **Insufficient disk space** | Blocked before starting, with the estimated size and available space |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Ctrl+E` | Open |
| `Enter` | Export |
| `Escape` | Cancel (confirms while exporting) |
| `Ctrl+O` | Browse for destination |

Format choice, options and destination directory persist between sessions.

---

## 7. Data contract

```csharp
public interface IExportService
{
    IReadOnlyList<IExportFormat> Formats { get; }
    Task<Result<ExportEstimate>> EstimateAsync(ExportRequest request, CancellationToken ct);
    Task<Result<ExportReport>> ExportAsync(
        ExportRequest request, IProgress<ExportProgress> progress, CancellationToken ct);
}

public interface IExportFormat
{
    string Key { get; }                 // "csv", "json", "sql", "xlsx", "md", "txt"
    string DisplayName { get; }
    string FileExtension { get; }
    bool SupportsStreaming { get; }
    Task WriteAsync(IAsyncEnumerable<ResultChunk> rows, IReadOnlyList<ResultColumn> columns,
                    Stream destination, ExportOptions options, CancellationToken ct);
}

public sealed record ExportRequest(
    ExportSource Source, string FormatKey, ExportOptions Options,
    IReadOnlyList<string> Columns, ExportDestination Destination);

public sealed record ExportProgress(
    long RowsWritten, long BytesWritten, long? EstimatedTotalRows,
    TimeSpan Elapsed, double RowsPerSecond);
```

Formats are DI-registered, so adding one is a new class and a registration — the seam
that later becomes a plugin point.

---

## 8. Acceptance criteria

- [ ] Exporting a 10M-row table keeps memory flat and completes without loading into the
      grid.
- [ ] Cancel stops promptly and deletes the partial file, stating that it did.
- [ ] CSV honours delimiter, quote, escape, line ending, NULL representation, encoding
      and BOM options.
- [ ] Leading `=`, `+`, `-`, `@` are neutralised by default in CSV, with the option
      visible and explainable.
- [ ] JSON preserves raw `jsonb` text.
- [ ] SQL export produces runnable statements with correctly quoted identifiers and
      correctly escaped literals, including NULLs, arrays and `bytea`.
- [ ] Excel export streams and does not build the workbook in memory.
- [ ] Sensitive-looking columns are unchecked by default and the reason is stated.
- [ ] Overwrite confirms; insufficient disk space blocks before starting.
- [ ] Every export is recorded in the audit log.
- [ ] Format options and last directory persist.
- [ ] All eight states render in both themes.
