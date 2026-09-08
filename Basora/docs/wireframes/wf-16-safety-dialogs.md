# wf-16 — Safety and Confirmation Dialogs

> Task: `T-P03`. Modal dialogs. Backed by `../10-safety-rules.md`.

---

## 1. Purpose

The last thing between a user and a mistake they cannot undo. The one job it must do
well: **make the blast radius obvious in under two seconds** — what database, what
object, how many rows, and what will be irreversible.

A confirmation nobody reads is worse than none, because it manufactures a habit of
clicking through. Every design choice here fights that.

---

## 2. Entry points

Automatically, whenever `ISafetyEngine` returns a `ConfirmationLevel` above `None`:

- Executing a statement in [wf-08](wf-08-sql-editor.md).
- Committing changes from [wf-05](wf-05-table-document.md) / [wf-07](wf-07-pending-changes.md).
- Applying DDL from [wf-11](wf-11-structure-editor.md) / [wf-12](wf-12-indexes-constraints.md).
- Drop / truncate from the object explorer.
- Terminating a backend from [wf-21](wf-21-activity-locks.md).
- Truncate-before-import in [wf-13](wf-13-import-wizard.md).

---

## 3. Layouts by level

### `Confirm` — a decision, not a speed bump

```text
+--------------------------------------------------------------+
|  #  Confirm operation                                        |
+--------------------------------------------------------------+
|  Environment   STAGING - staging@staging.company.com         |
|  Operation     DELETE                                        |
|  Table         public.orders                                 |
|  Affected      ~4,281 rows (planner estimate)                |
|                                                              |
|  DELETE FROM orders WHERE created_at < '2025-01-01';         |
|                                                              |
|  ~ This cannot be undone unless you run it in a transaction. |
+--------------------------------------------------------------+
|  [ Run in a transaction ]           [ Cancel ]  [ Continue ] |
+--------------------------------------------------------------+
```

### `TypeToConfirm` — production, or critical anywhere

```text
+--------------------------------------------------------------+
|  #  PRODUCTION DATABASE                                      |
+--------------------------------------------------------------+
|  Environment   PRODUCTION - production@postgres.company.com  |
|  Operation     DELETE (no WHERE clause)                      |
|  Table         public.users                                  |
|  Affected      ~18,421,932 rows - the entire table           |
|                                                              |
|  DELETE FROM users;                                          |
|                                                              |
|  FINDINGS                                                    |
|  x  No WHERE clause. Every row will be deleted.              |
|  ~  Not inside a transaction. This cannot be rolled back.    |
|  ~  4 tables reference users with ON DELETE CASCADE.         |
|                                                              |
|  SAFER ALTERNATIVES                                          |
|  - Add a WHERE clause                     [ Edit query ]     |
|  - Run inside a transaction               [ Use this ]       |
|  - Delete in batches with LIMIT            [ Use this ]      |
|                                                              |
|  Type  DELETE users  to continue:                            |
|  [                                                    ]      |
+--------------------------------------------------------------+
|                    [ Cancel ]        [ Continue anyway ]     |
|                     (default)          (disabled)            |
+--------------------------------------------------------------+
```

### `Blocked`

Only when the operation is impossible or forbidden by an explicit policy — for example
a write on a read-only session. Explains why and offers the specific action that would
unblock it ("disable read-only for this session").

---

## 4. Component inventory

### Context block — always first, always the same order

Environment (with its colour), connection target, operation, object, affected rows. This
consistency is the point: a user learns to read the same five lines every time.

### Findings

Each finding from the verdict, with a severity glyph and plain-language text. **No error
codes as the primary text** — `unbounded.delete` is for logs, "No WHERE clause. Every
row will be deleted." is for people.

Findings that come from cascading foreign keys must be listed, because deleting 18M rows
from `users` silently deleting 400M rows from `orders` is the exact surprise this dialog
exists to prevent.

### Safer alternatives

From `../10-safety-rules.md` section 9. Each is a real button that rewrites the statement
in the editor rather than executing anything. Offering the safe path is what turns a
warning into help.

### Type-to-confirm field

- The phrase names the actual object.
- Rendered as **non-selectable** text so it cannot be copy-pasted.
- Case-sensitive exact match after trimming.
- The Continue button is disabled until it matches, and never becomes the focused
  default.

### Buttons

`Cancel` is focused on open and is the `Enter` and `Escape` action. The destructive
button is danger-styled, positioned last, and is never activated by `Enter`.

### Estimate honesty

Row counts are planner estimates and are always shown with `~`. If the estimate failed,
the dialog says **"unknown — the estimate could not be produced"** and the risk is
escalated, never silently omitted.

---

## 5. States

| State | Rendering |
|---|---|
| **Estimating** | Affected-row line shows a spinner for up to 500 ms, then falls back to "unknown"; the dialog is usable throughout |
| **Estimate unavailable** | "Unknown" with a note on why, and an escalated risk level |
| **Awaiting phrase** | Continue disabled; a hint appears after a wrong attempt |
| **Phrase mismatch** | Field border `data.invalid` with "does not match" — no automatic correction |
| **Executing** | Buttons disabled, progress shown, Cancel attempts a server-side cancel |
| **Executed** | Dialog closes; a persistent (non-auto-dismissing) result banner reports affected rows and offers ROLLBACK if a transaction is open |
| **Failed** | Error shown in place with SQLSTATE; the dialog stays open so the user can adjust |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Escape` | Cancel |
| `Enter` | Cancel (**deliberately** — the safe action is the default) |
| `Tab` | Cycle: alternatives, phrase field, Cancel, Continue |
| `Ctrl+C` | Copy the statement |

Explicit anti-patterns, all forbidden:

- No "don't show this again" checkbox.
- No auto-focus on the destructive button.
- No dismissal by clicking the backdrop.
- No timed auto-proceed.
- No global "disable safety checks" switch (safe mode is per connection and visible).

---

## 7. Data contract

```csharp
public interface ISafetyDialogService
{
    Task<SafetyDecision> RequestConfirmationAsync(
        SafetyVerdict verdict, SafetyContext context, CancellationToken ct);
}

public sealed record SafetyContext(
    string ConnectionName, string Database, DeploymentEnvironment Environment,
    string Sql, StatementKind Kind, DbObjectRef? Subject, bool InsideTransaction);

public sealed record SafetyDecision(
    SafetyOutcome Outcome, string? ReplacementSql, bool WrapInTransaction);

public enum SafetyOutcome { Cancelled, Proceed, UseAlternative, EditStatement }
```

Consumes `SafetyVerdict` from `ISafetyEngine`. Every outcome is written to the audit log
with the assessed risk and the findings that were displayed.

---

## 8. Acceptance criteria

- [ ] Environment, connection, operation, object and affected rows appear in that order,
      every time.
- [ ] Row counts are shown as estimates with `~`; failure shows "unknown" and escalates.
- [ ] Cascading foreign-key impact is listed when it applies.
- [ ] Findings are in plain language, not error codes.
- [ ] Safer alternatives are offered where the engine has one, and choosing one rewrites
      the statement without executing.
- [ ] The type-to-confirm phrase names the object, is not selectable, and matches exactly.
- [ ] Continue is disabled until the phrase matches and is never the `Enter` action.
- [ ] Cancel is focused on open and bound to both `Enter` and `Escape`.
- [ ] The backdrop does not dismiss the dialog.
- [ ] There is no "don't show again" option anywhere in the product.
- [ ] Every decision, including cancellation, is audited.
- [ ] The bypass counter in the status bar increments on Continue.
- [ ] All seven states render in both themes.
- [ ] Full keyboard operation; focus is trapped in the dialog and restored on close.
