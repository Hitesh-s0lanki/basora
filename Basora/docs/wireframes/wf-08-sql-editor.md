# wf-08 — SQL Editor

> Task: `T-Q06` (editor), `T-Q02` (completion), `T-Q03` (formatter).
> Document, inside [wf-03](wf-03-app-shell.md) region E.

---

## 1. Purpose

Write and run SQL. This is the component that decides whether a developer keeps the app
open. The one job it must do well: **autocomplete that actually knows the schema** —
tables from this database, columns from the tables in *this* `FROM` clause, aliases
resolved, in under 100 ms.

---

## 2. Entry points

- `Ctrl+N`, or `+` on the document tab strip.
- "Open SQL file" (`Ctrl+O`), drag a `.sql` file onto the window.
- "Edit as SQL" from the filter builder, "Suggest fix" from a plan finding, "Open" from
  history or favorites.
- Right-click an object > "Generate SELECT / INSERT / UPDATE".

---

## 3. Layout

```text
+-----------------------------------------------------------------------------+
| [Run ^Enter] [Run All] [Explain] [Format] [x Cancel] | tx: [Begin][Commit][Rollback] |
+-----------------------------------------------------------------------------+
|  1 | SELECT u.id, u.email, count(o.id) AS order_count                        |
|  2 |   FROM users u                                                          |
|  3 |   JOIN orders o ON o.customer_id = u.id                                 |
|  4 |  WHERE u.created_at > $1                                                |
|  5 |    AND u.st|                                                            |
|    |            +----------------------------------+                        |
|    |            | # status        text             |                        |
|    |            | # started_at    timestamptz      |                        |
|    |            | # state         user_state       |                        |
|    |            +----------------------------------+                        |
|  6 |  GROUP BY u.id, u.email                                                 |
|                                                                             |
| ~ statement 1 of 1 - line 5, col 14 - 6 lines                              |
+-----------------------------------------------------------------------------+
| [ Results ] [ Messages 2 ] [ Plan ] [ History ]                      ^  x   |
|  ... result grid (wf-09) ...                                                |
+-----------------------------------------------------------------------------+
```

The **current statement** (the one `Ctrl+Enter` would run) is marked by a subtle left
gutter bar spanning its lines. Ambiguity about what will run is the most common source
of accidents in a multi-statement editor.

---

## 4. Component inventory

### Editor surface (AvaloniaEdit)

| Feature | Detail |
|---|---|
| Highlighting | PostgreSQL-specific: keywords, types, functions, strings, dollar-quoted bodies, comments, parameters. Colours derive from design tokens so it never looks foreign |
| Line numbers | Gutter with the current-statement bar and error markers |
| Folding | Statements, parenthesised blocks, CTEs, function bodies |
| Multi-cursor | `Alt+Click`, `Ctrl+Alt+Up/Down` |
| Find/replace | `Ctrl+F` / `Ctrl+H`, regex supported, in-selection scope |
| Bracket matching | Including dollar-quote pairing |
| Auto-indent | Continuation indent for clauses |
| Word wrap | Off by default, toggleable |
| Minimap | Off by default, toggleable |

### Autocomplete (`T-Q02`) — the differentiator

Triggered by `Ctrl+Space` or automatically after 2 characters, and after `.`.

Context is derived from the shallow parser, not a keyword guess:

| Cursor context | Suggests |
|---|---|
| After `FROM` / `JOIN` | Tables, views, matviews; schema-qualified if the schema was typed |
| After `SELECT`, `WHERE`, `ON`, `GROUP BY`, `ORDER BY`, `HAVING` | Columns of the relations in the statement's `FROM`, **with aliases resolved**, plus functions and keywords |
| After `alias.` | Only that relation's columns |
| After `schema.` | Only that schema's objects |
| Inside a function call | Parameter hints with types |
| After `::` | Types |
| Start of a statement | Statement keywords and snippets |

Each item shows: kind icon, name, type or signature, and the source relation. Ranking:
exact prefix, then relations already in the statement, then recently used, then
alphabetical. Fuzzy subsequence matching (`ordit` matches `order_items`).

The completion list is served from the metadata cache; a cache miss triggers a background
fetch and the list updates in place rather than blocking typing.

### Toolbar

Run (statement under cursor), Run All, Explain, Format, Cancel (only while running), and
transaction controls showing live state and elapsed time.

### Parameters

`$1`-style parameters and `:named` parameters are detected and prompt for values in a
compact bar above the results, with types inferred where possible and remembered per
document.

### Status strip

Statement index and count, cursor position, document line count, and the connection this
document is bound to (a document belongs to one connection; changing it is explicit).

### Messages tab

`RAISE NOTICE`, warnings and server notices with severity and timestamp. Most clients
swallow these; developers debugging `plpgsql` depend on them.

---

## 5. States

| State | Rendering |
|---|---|
| **Empty** | Blank document with a hint line showing the run shortcut; cursor focused |
| **Typing / idle** | Normal. No network activity for completion beyond cache refresh |
| **Running** | Toolbar shows Cancel; a progress strip shows elapsed time and rows received; **the editor stays fully editable** |
| **Cancelled** | Result area shows "Cancelled after 4.2 s"; not styled as an error |
| **Error** | The offending statement's line gutter is marked; the error strip shows SQLSTATE, message, hint, and a **caret placed at the reported position** |
| **Read-only connection** | Run is enabled for `SELECT`; write statements show an inline explanation before execution is attempted |
| **Disconnected** | Editor stays editable; Run offers Reconnect |
| **Unsaved** | Dirty dot on the tab; content survives crash and restart |

---

## 6. Interactions and keyboard

Full map in `../09-keyboard-map.md` section 4. Rules specific to this surface:

- `Ctrl+Enter` runs the **statement under the cursor**, determined by the lexer, not by
  splitting on semicolons.
- `Ctrl+Shift+Enter` runs the whole document as a batch, producing multiple result sets.
- `Alt+Enter` runs the selection verbatim.
- `Escape` closes the completion popup if open, otherwise cancels the running query.
- The editor is never disabled while a query runs. Users type the next query while
  waiting; blocking them is a common and infuriating failure in other tools.
- `F12` on an identifier opens that object's document.

---

## 7. Data contract

```csharp
public interface ISqlLexer
{
    IReadOnlyList<SqlToken> Tokenize(string sql);
    IReadOnlyList<StatementSpan> SplitStatements(string sql);
    StatementSpan? StatementAt(string sql, int caretOffset);
}

public sealed record StatementSpan(int Start, int Length, StatementKind Kind, string Text);

public interface ICompletionProvider
{
    Task<IReadOnlyList<CompletionItem>> GetCompletionsAsync(
        CompletionContext context, CancellationToken ct);
}

public sealed record CompletionContext(
    string DocumentText, int CaretOffset, IDatabaseSession Session, string? CurrentSchema);

public sealed record CompletionItem(
    string Label, string InsertText, CompletionKind Kind,
    string? Detail, string? Documentation, int SortPriority);

public interface ISqlFormatter
{
    Result<string> Format(string sql, FormatOptions options);
}

public interface IQueryExecutor
{
    IAsyncEnumerable<ResultChunk> ExecuteStreamingAsync(
        IDatabaseSession session, QueryRequest request, CancellationToken ct);
    Task<Result> CancelAsync(Guid executionId, CancellationToken ct);
}
```

The lexer, formatter and completion ranking are pure and unit-tested without a database.

---

## 8. Acceptance criteria

- [ ] Statement splitting is correct for dollar-quoted function bodies containing
      semicolons, nested block comments, `E'...'` strings and doubled quotes.
- [ ] The current statement is visually marked before running.
- [ ] Completion after `FROM` lists relations; after an alias `.` lists only that
      relation's columns, with aliases resolved.
- [ ] Completion popup appears in under 100 ms from the metadata cache.
- [ ] Fuzzy subsequence matching works (`ordit` matches `order_items`).
- [ ] Formatting is idempotent — formatting twice equals formatting once — and preserves
      comments and string contents exactly.
- [ ] Syntax error position maps to the correct caret offset in the document, accounting
      for the statement offset and UTF-8 vs UTF-16 indexing.
- [ ] The editor stays editable and responsive while a query runs.
- [ ] `Escape` closes completion first, then cancels the query.
- [ ] `RAISE NOTICE` output appears in the Messages tab.
- [ ] Document content survives a force-kill and restart.
- [ ] Running on a Production connection routes writes through the safety ladder.
- [ ] All eight states render in both themes.
