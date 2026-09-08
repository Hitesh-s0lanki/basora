# wf-07 — Pending Changes and SQL Preview

> Task: `T-D06`. Panel (dockable or modal) launched from
> [wf-05](wf-05-table-document.md) and [wf-11](wf-11-structure-editor.md).

---

## 1. Purpose

Show exactly what will run before it runs, and let the user drop individual changes. The
one job it must do well: **make GUI editing auditable** — every click in the grid becomes
a reviewable SQL statement traceable back to the row that produced it.

This is the feature that makes a GUI editor trustworthy on a production database.

---

## 2. Entry points

- `Ctrl+Shift+P` in a table document.
- "Preview SQL" in the table footer.
- Clicking the pending-change count in the status bar.
- Commit attempt: the preview is shown first whenever the change set is above a
  configurable size (default 10 statements) or the environment is Staging/Production.

---

## 3. Layout

```text
+---------------------------------------------------------------------------+
|  Pending Changes - public.users                                      [X]  |
+---------------------------------------------------------------------------+
|  3 changes: 1 insert - 1 update - 1 delete       [ Group by row | order ]  |
+-------------------------------+-------------------------------------------+
| CHANGES                       | SQL PREVIEW                               |
|                               |                                           |
| [x] + Insert row              | BEGIN;                                    |
|       email = d@example.com   |                                           |
|       name  = New Person      | INSERT INTO public."users"                |
|                               |   ("email", "name")                       |
| [x] ! Update row id = 3       | VALUES ($1, $2);                          |
|       name                    |                                           |
|       "Priya" -> "Priya S."   | UPDATE public."users"                     |
|                               |    SET "name" = $3                        |
| [x] - Delete row id = 5       |  WHERE "id" = $4                          |
|       e@example.com           |    AND "name" = $5;   -- original value   |
|                               |                                           |
|                               | DELETE FROM public."users"                |
|                               |  WHERE "id" = $6;                         |
|                               |                                           |
|                               | COMMIT;                                   |
+-------------------------------+-------------------------------------------+
|  [x] Wrap in a transaction    | Parameters:  $1 'd@example.com'  ...      |
|  [ ] Stop on first error      |                                           |
+---------------------------------------------------------------------------+
|  # PRODUCTION - this will modify 3 rows                                   |
|  [ Copy SQL ]  [ Save as .sql ]        [ Discard all ] [Cancel] [ Commit ] |
+---------------------------------------------------------------------------+
```

---

## 4. Component inventory

### Change list (left)

One entry per pending change, each with:

- A checkbox — **unchecking excludes it from this commit** without discarding it.
- A kind glyph and colour matching the grid (`+` inserted, `!` modified, `-` deleted).
- The row's identity (primary key values), so it is recognisable.
- For updates, the changed columns with `old -> new`, truncated with a hover for long
  values.
- Hover actions: **Reveal in grid** (scrolls to and highlights the row) and **Discard**.

Grouping toggle: by row (default, matches the user's mental model) or by execution order
(matches the SQL panel).

### SQL preview (right)

- Syntax-highlighted, read-only, monospace.
- **Selecting a change on the left highlights its statement on the right, and vice
  versa.** This bidirectional link is the core of the screen.
- Values are `$n` placeholders with a parameter table beneath; hovering a placeholder
  shows the bound value. This makes the parameterisation visible rather than asserted.
- Statements are generated in a safe order: inserts, then updates, then deletes — and
  within deletes, child rows before parents when FK relationships are known.

### Options

- **Wrap in a transaction** (default on). Off is allowed only on Local/Development and
  carries a warning.
- **Stop on first error** (default on). Off means "continue and report" and is only
  meaningful without a transaction.

### Footer

Environment banner with the total affected-row count. Actions: Copy SQL, Save as `.sql`,
Discard all (confirms), Cancel, Commit. **Commit is primary; Discard all is not
danger-styled but does confirm.**

---

## 5. States

| State | Rendering |
|---|---|
| **Empty** | "No pending changes" with the panel's other controls disabled |
| **Generating** | Skeleton in the SQL panel; the list is already populated |
| **Not generatable** | A change whose SQL cannot be produced (no row identity) is flagged inline with the reason and excluded from commit |
| **Committing** | Progress per statement; Cancel attempts a rollback |
| **Commit succeeded** | Per-statement affected rows; panel closes after a moment; a toast summarises |
| **Commit failed** | The failing statement is highlighted with its SQLSTATE and message; the transaction was rolled back; **all changes are preserved** and the panel stays open |
| **Partial (no transaction)** | Each statement shows succeeded/failed/not-run; the user is told precisely what did and did not apply |
| **Safety escalation** | Before executing, the safety verdict is shown inline — findings, risk, and any safer alternative |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Ctrl+Shift+P` | Open / close |
| `Space` | Toggle the focused change's checkbox |
| `Delete` | Discard the focused change |
| `Enter` | Reveal the focused change in the grid |
| `Ctrl+Enter` | Commit |
| `Ctrl+C` | Copy the SQL |
| `Escape` | Close without committing |

Closing the panel never discards anything. Closing the **document** with pending changes
prompts, and offers to copy the SQL before discarding — losing twenty minutes of grid
edits with no escape hatch is unacceptable.

---

## 7. Data contract

```csharp
public interface IChangeSqlGenerator
{
    Result<GeneratedStatements> Generate(ChangeSet set, TableDescriptor table,
                                         RowIdentityStrategy identity);
}

public sealed record GeneratedStatements(
    IReadOnlyList<GeneratedStatement> Statements, bool WrappedInTransaction);

public sealed record GeneratedStatement(
    Guid ChangeId, string Sql, IReadOnlyList<QueryParameter> Parameters,
    StatementKind Kind, string Description);

public sealed record CommitReport(
    bool Succeeded, IReadOnlyList<StatementOutcome> Outcomes,
    TimeSpan Duration, Error? Error);

public sealed record StatementOutcome(
    Guid ChangeId, bool Executed, long? AffectedRows, Error? Error);
```

Plus `IChangeSetService` from [wf-05](wf-05-table-document.md) and `ISafetyEngine`.

The generator is **pure** and lives in `Basora.Core` — its golden-file tests are the
primary correctness gate for the whole editing feature.

---

## 8. Acceptance criteria

- [ ] Every pending change maps to exactly one visible statement, and selecting one
      highlights the other in both directions.
- [ ] Values render as `$n` placeholders with a parameter table; nothing is interpolated.
- [ ] `UPDATE` and `DELETE` include original values in the `WHERE` clause for optimistic
      concurrency.
- [ ] Identifiers are always quoted through the single quoting utility.
- [ ] Statement order is insert, update, delete, with FK-aware ordering among deletes.
- [ ] Unchecking a change excludes it from the commit without discarding it.
- [ ] A change with no resolvable row identity is flagged and excluded, with a reason.
- [ ] Failed commit rolls back, highlights the failing statement, and preserves every
      pending change.
- [ ] Safety verdict is shown before execution on Staging and Production.
- [ ] Copy SQL and Save as .sql produce runnable text with parameters inlined **and
      correctly escaped**, with a note that the saved form is literal-valued.
- [ ] Closing the document with pending changes prompts and offers to copy the SQL.
- [ ] Golden-file tests cover inserts, updates, deletes, NULL handling, every
      `PgTypeCategory`, quoted identifiers, and multi-column keys.
- [ ] All eight states render in both themes.
