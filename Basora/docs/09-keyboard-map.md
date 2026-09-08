# 09 — Keyboard Map

Basora is a keyboard-first tool. **Every action reachable by mouse must have either a
shortcut or a command-palette entry.** This is checked in review: a new command that is
not registered with `ICommandRegistry` is not done.

---

## 1. Platform modifiers

Shortcuts are authored once with a logical modifier and resolved per platform.

| Logical | Windows / Linux | macOS |
|---|---|---|
| `Mod` | `Ctrl` | `Cmd` |
| `Alt` | `Alt` | `Option` |
| `Shift` | `Shift` | `Shift` |
| `Ctrl` (literal) | `Ctrl` | `Control` |

Use `Mod` for everything except where a literal `Ctrl` is required on macOS by
convention. Tables below use the Windows/Linux form; substitute `Cmd` on macOS.

---

## 2. Global

| Shortcut | Action |
|---|---|
| `Ctrl+P` | Open Anything (objects, files, queries) |
| `Ctrl+K` | Command Palette |
| `Ctrl+Shift+P` | Command Palette (alias, for VS Code muscle memory) |
| `Ctrl+,` | Settings |
| `Ctrl+N` | New SQL document |
| `Ctrl+Shift+N` | New connection |
| `Ctrl+O` | Open SQL file |
| `Ctrl+W` | Close document tab |
| `Ctrl+Shift+T` | Reopen last closed tab |
| `Ctrl+Tab` / `Ctrl+Shift+Tab` | Next / previous document tab |
| `Ctrl+1..9` | Go to document tab N |
| `Ctrl+Alt+1..9` | Go to connection tab N |
| `Ctrl+Shift+E` | Focus Object Explorer |
| `Ctrl+Shift+H` | Query History |
| `Ctrl+Shift+B` | Favorites |
| `Ctrl+\` | Split editor |
| `Ctrl+B` | Toggle sidebar |
| `Ctrl+J` | Toggle bottom panel (results / messages / history) |
| `F11` | Toggle fullscreen |
| `Ctrl+Shift+D` | Toggle dark / light theme |
| `F1` | Help / shortcut cheat sheet |
| `Escape` | Cancel innermost: popup, then dialog, then running query |

---

## 3. Connections

| Shortcut | Action |
|---|---|
| `Ctrl+Shift+N` | New connection |
| `Ctrl+Shift+C` | Connect / reconnect current |
| `Ctrl+Shift+X` | Disconnect current |
| `Ctrl+Shift+R` | Refresh metadata for current connection |
| `Ctrl+Shift+K` | Switch database on this connection |
| `Alt+Enter` | Edit selected connection (in Connection Manager) |
| `Ctrl+D` | Duplicate selected connection (in Connection Manager) |

---

## 4. SQL editor

| Shortcut | Action |
|---|---|
| `Ctrl+Enter` | Execute statement under cursor |
| `Ctrl+Shift+Enter` | Execute entire document |
| `Alt+Enter` | Execute selection |
| `Ctrl+Shift+E` in editor context | Execute and EXPLAIN |
| `Ctrl+Alt+Enter` | Execute and EXPLAIN ANALYZE (guarded on Production) |
| `Escape` | Cancel running query |
| `Ctrl+Space` | Trigger autocomplete |
| `Ctrl+Shift+Space` | Parameter / function signature hint |
| `Ctrl+Shift+F` | Format document |
| `Ctrl+K Ctrl+F` | Format selection |
| `Ctrl+/` | Toggle line comment |
| `Ctrl+Shift+/` | Toggle block comment |
| `Ctrl+D` | Save as favorite |
| `Ctrl+S` | Save to file |
| `Ctrl+F` / `Ctrl+H` | Find / replace |
| `Ctrl+G` | Go to line |
| `Alt+Up` / `Alt+Down` | Move line up / down |
| `Ctrl+Shift+K` (editor) | Delete line |
| `Ctrl+L` | Select line |
| `Alt+Click` | Add cursor (multi-cursor) |
| `Ctrl+Alt+Up/Down` | Add cursor above / below |
| `F12` | Go to definition of object under cursor (opens its document) |
| `Ctrl+Shift+M` | Toggle Messages pane (NOTICE / RAISE output) |
| `Ctrl+Up` / `Ctrl+Down` | Previous / next query in history |

---

## 5. Data grid

| Shortcut | Action |
|---|---|
| `Enter` / `F2` | Begin editing the focused cell |
| `Escape` | Cancel the current cell edit |
| `Tab` / `Shift+Tab` | Next / previous cell |
| `Enter` (while editing) | Commit cell edit, move down |
| `Ctrl+Enter` | Refresh data |
| `Ctrl+S` | Commit pending changes |
| `Ctrl+Z` / `Ctrl+Y` | Undo / redo a pending change (not a database undo) |
| `Ctrl+Shift+Z` | Discard all pending changes (confirms) |
| `Ctrl+Shift+P` | Preview pending SQL |
| `Ctrl+I` | Insert row |
| `Ctrl+Delete` | Mark row(s) for deletion |
| `Ctrl+Shift+D` (grid) | Duplicate row |
| `Ctrl+C` / `Ctrl+V` | Copy / paste cells |
| `Ctrl+Shift+C` | Copy with headers |
| `Ctrl+Alt+C` | Copy as INSERT statements |
| `Ctrl+A` | Select all rows on the current page |
| `Ctrl+Shift+N` (grid) | Set focused cell to NULL |
| `Ctrl+F` | Find in results |
| `Ctrl+Shift+F` (grid) | Open filter builder |
| `Ctrl+E` | Export current result |
| `Space` | Toggle the value inspector for the focused cell |
| `Alt+Right` / `Alt+Left` | Next / previous page |
| `Ctrl+Home` / `Ctrl+End` | First / last row |
| `F5` | Refresh |

---

## 6. Object explorer

| Shortcut | Action |
|---|---|
| Type any letters | Incremental filter |
| `Enter` | Open the selected object's default document |
| `Alt+Enter` | Open properties |
| `Right` / `Left` | Expand / collapse |
| `Ctrl+Right` | Expand recursively |
| `F5` | Refresh node |
| `Ctrl+C` | Copy qualified name |
| `Ctrl+Shift+C` | Copy CREATE statement |
| `Delete` | Drop object (always goes through the safety ladder) |
| `Ctrl+Shift+S` | Open SQL definition |

---

## 7. Dialogs and wizards

| Shortcut | Action |
|---|---|
| `Enter` | Default (never-destructive) action |
| `Escape` | Cancel |
| `Ctrl+Enter` | Confirm in multi-line contexts |
| `Alt+Right` / `Alt+Left` | Next / back in a wizard |

**Rule:** in any dialog containing a destructive action, `Enter` never triggers it, the
destructive button is never focused on open, and where `TypeToConfirm` applies the
button stays disabled until the phrase matches exactly.

---

## 8. Conflicts and customisation

- The shortcut table is data (`Basora.Core/Input/DefaultKeymap.cs`), not scattered
  `KeyGesture` literals in XAML. This is what makes remapping and conflict detection
  possible at all.
- Settings offers a keymap editor with live conflict detection; a conflicting binding
  cannot be saved without resolving it.
- Presets: Basora default, TablePlus-like, DataGrip-like, pgAdmin-like. Migration from
  a competitor is a real adoption lever and costs us only a data file per preset.
- Chords (`Ctrl+K Ctrl+F`) are supported; the pending-chord state is shown in the status
  bar so the user is never stuck wondering why keys stopped working.
- Context matters: the same gesture may mean different things in the editor and the
  grid. Bindings declare their context (`Global`, `Editor`, `Grid`, `Tree`, `Dialog`)
  and the innermost focused context wins.

---

## 9. Discoverability

- `F1` opens a searchable cheat sheet grouped by context.
- Every menu item, context-menu item and tooltip shows its shortcut.
- The command palette shows the shortcut beside each command, and running a command
  from the palette that *has* a shortcut briefly surfaces it — the cheapest possible
  way to teach the keyboard map.
