# wf-15 — Command Palette and Open Anything

> Task: `T-U06`. Overlay. Two modes of one control.

---

## 1. Purpose

Reach anything without the mouse. Two modes:

- **`Ctrl+P` Open Anything** — find a database object, saved query, or open document.
- **`Ctrl+K` Command Palette** — find and run a command.

The one job it must do well: **be faster than the tree**. That means fuzzy matching,
sensible ranking, and results that appear as you type with no perceptible delay.

---

## 2. Entry points

- `Ctrl+P`, `Ctrl+K`, `Ctrl+Shift+P`.
- Clicking the search affordance in the toolbar.
- Typing a prefix in either mode switches modes: `>` for commands, `@` for objects,
  `#` for columns, `:` for line number, `?` for help.

---

## 3. Layout

```text
                +--------------------------------------------------+
                | > users                                          |
                +--------------------------------------------------+
                |  TABLES                                          |
                |  #  users                      public   1.2M     |
                |  #  user_sessions              public    88K     |
                |  #  user_addresses             public   412K     |
                |                                                  |
                |  VIEWS                                           |
                |  =  active_users                public           |
                |                                                  |
                |  FUNCTIONS                                       |
                |  f  get_user(bigint)            public           |
                |                                                  |
                |  COLUMNS                                         |
                |  |  users.email                 varchar(255)     |
                |  |  orders.user_id              bigint      FK   |
                |                                                  |
                |  SAVED QUERIES                                   |
                |  *  Active Users (30d)          Users folder     |
                +--------------------------------------------------+
                |  Enter open - Ctrl+Enter new tab - Tab preview   |
                +--------------------------------------------------+
```

Command mode:

```text
                | > format                                         |
                |  Format SQL                        Ctrl+Shift+F  |
                |  Format selection                 Ctrl+K Ctrl+F  |
                |  Settings: SQL formatting                        |
```

---

## 4. Component inventory

### Input

Single line, focused on open, with the mode prefix visible. Backspacing past the prefix
returns to the default mode. The previous query is retained but fully selected, so typing
replaces it and `Down` reuses it.

### Result list

Grouped by category with sticky group headers. Each row: kind icon, name with **matched
characters highlighted**, qualifier (schema, folder), and metadata (row estimate, type,
shortcut). Maximum ~50 rows rendered, virtualised, with a "more results" hint.

### Ranking

In order of weight:

1. Exact name match
2. Prefix match
3. Fuzzy subsequence match, scored by match density and word-boundary hits
4. Recency of use (a per-user MRU list)
5. Frequency of use
6. Objects in the current schema over other schemas
7. Tables over views over functions over columns

Recency and frequency matter enormously in practice: a developer opens the same six
tables all day, and after a week the palette should need one keystroke for each.

### Preview

`Tab` opens a side preview: for a table, its columns and row estimate; for a query, its
SQL; for a command, its description and shortcut. Does not commit to opening.

### Search scope (Open Anything)

Searches the metadata cache first for instant results, then issues a server-side search
(`search_objects.sql`) for anything not cached, merging results as they arrive without
disturbing the user's selection. On a database with 50,000 objects, we do not preload
everything.

### Command source

Every command in the app registers with `ICommandRegistry`, which is also what drives
menus and the keymap. There is exactly one list of commands in the product.

---

## 5. States

| State | Rendering |
|---|---|
| **Open, empty query** | Recent objects and recent commands, most-recent first |
| **Typing** | Cached results instantly; a subtle inline indicator while the server search runs |
| **No results** | "No matches for 'x'" plus mode-switch hints and, in object mode, a suggestion to check the schema filter |
| **Loading (server search)** | Cached results shown with a thin progress line; the selection is never disturbed by late arrivals |
| **Disconnected** | Commands still work; object search says a connection is required |
| **Error** | Cached results plus an inline note that the server search failed; never blocks the palette |
| **Unavailable command** | Listed but dimmed with a reason ("requires an active connection") rather than hidden |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Ctrl+P` / `Ctrl+K` | Open in object / command mode |
| Type | Filter |
| `Up` / `Down` | Move selection |
| `Enter` | Open or run |
| `Ctrl+Enter` | Open in a new tab |
| `Tab` | Toggle preview |
| `Escape` | Close (restores the previous focus exactly) |
| `>` `@` `#` `:` `?` | Switch mode |
| `Ctrl+Backspace` | Clear |

Opening the palette never steals focus permanently: closing it returns focus to the exact
element and caret position that had it before.

---

## 7. Data contract

```csharp
public interface ICommandRegistry
{
    IReadOnlyList<AppCommand> Commands { get; }
    void Register(AppCommand command);
    Task<Result> ExecuteAsync(string commandId, object? parameter, CancellationToken ct);
}

public sealed record AppCommand(
    string Id, string Title, string Category, string? Description,
    KeyGestureSpec? Shortcut, Func<bool> CanExecute, string? UnavailableReason);

public interface IObjectSearchService
{
    Task<IReadOnlyList<SearchHit>> SearchCachedAsync(
        IDatabaseSession session, string query, CancellationToken ct);

    Task<IReadOnlyList<SearchHit>> SearchServerAsync(
        IDatabaseSession session, string query, int limit, CancellationToken ct);
}

public sealed record SearchHit(
    DbObjectRef Ref, string DisplayName, string? Qualifier, string? Metadata,
    IReadOnlyList<int> MatchedIndices, double Score);

public interface IFuzzyMatcher
{
    bool TryMatch(string candidate, string pattern, out double score, out int[] indices);
}
```

`IFuzzyMatcher` and the ranking function are pure, and are the parts worth testing
heavily.

---

## 8. Acceptance criteria

- [ ] Palette opens in under 50 ms and results update as you type with no visible lag.
- [ ] Fuzzy matching works (`ordit` matches `order_items`) with matched characters
      highlighted.
- [ ] Ranking follows the documented order, and recency/frequency demonstrably promote
      frequently used objects.
- [ ] Cached results appear instantly; server results merge without disturbing the
      selection.
- [ ] Columns are searchable and show their table.
- [ ] All mode prefixes work and backspacing past a prefix returns to the default mode.
- [ ] Every registered command is findable, shows its shortcut, and unavailable commands
      are dimmed with a reason.
- [ ] `Tab` preview works for tables, queries and commands.
- [ ] Closing restores the previous focus and caret position exactly.
- [ ] Works with 50,000 objects without preloading them.
- [ ] All seven states render in both themes.
