# wf-03 — App Shell

> **Build this first.** Every document screen renders inside it. Task: `T-U03`.

---

## 1. Purpose

The frame that holds everything: connection tabs, sidebar, document area, bottom panel,
status bar. Its one job is to make the user's **current context unmistakable** — which
connection, which environment, which database — while consuming as little vertical
space as possible.

Not a document. It is the window.

---

## 2. Entry points

- Application launch (restores the persisted workspace).
- Reopening the main window from the tray/dock.

---

## 3. Layout

```text
+==============================================================================+
| (A) Basora   File  Edit  View  Query  Database  Tools  Help          _ [] X  |
+==============================================================================+
| (B) [|Production] [ Staging ] [ Local ] [+]                                  |   <- 3px top border = env colour
+---------+--------------------------------------------------------------------+
| (C)     | (D) [users x] [orders x] [Query 1 * x] [+]                          |
| SIDEBAR |----------------------------------------------------------------—---|
|         | (E)                                                                 |
| search  |                                                                     |
| ------  |                    DOCUMENT AREA                                    |
| v prod  |                                                                     |
|  v publ |                    (owned by the active document)                   |
|   > Tab |                                                                     |
|   > Vie |                                                                     |
|   > Fun |                                                                     |
|         |---------------------------------------------------------------------|
|         | (F) [ Results ] [ Messages ] [ History ] [ Notices ]        ^  x    |
|         |                                                                     |
|         |                    BOTTOM PANEL (collapsible)                       |
+---------+---------------------------------------------------------------------+
| (G) [*] Production - production@postgres.company.com:5432 - PG 16.2 | READ ONLY | 3 pending | tx 00:42 | 1,204 rows in 182 ms |
+==============================================================================+
```

---

## 4. Component inventory

### (A) Menu bar / title bar

Native menu on macOS; in-window menu on Windows/Linux. Menus: File, Edit, View, Query,
Database, Tools, Help. **Every item shows its shortcut.** When a Production connection is
active, the window gets a 2px `env.production` border on all edges — the strongest
ambient signal in the app, and the only place we draw a window-edge accent.

### (B) Connection tab strip

- One tab per open connection. 3px top border in the environment colour.
- Tab content: colour dot, connection name, database name in `text.tertiary`,
  connection-state glyph (connected / connecting / reconnecting / failed).
- `+` opens [wf-01 Connection Manager](wf-01-connection-manager.md).
- Middle-click closes (prompts if a transaction is open or changes are pending).
- Drag to reorder; drag out to detach into a new window (post-MVP-1, but the tab model
  must not preclude it).
- Overflow becomes a scrollable strip with a chevron menu — never shrinks tabs to
  illegibility.

### (C) Sidebar

Hosts [wf-04 Object Explorer](wf-04-object-explorer.md) by default; switchable to
Favorites or History via a thin icon rail on its left edge. Width is draggable
(min 180, default 260) and persisted per connection. `Ctrl+B` toggles.

### (D) Document tab strip

- One tab per open document within the active connection.
- Icon per document kind, title, dirty dot for unsaved SQL, badge for pending changes.
- `Ctrl+W` closes, `Ctrl+Shift+T` reopens, `Ctrl+Tab` cycles (MRU order),
  `Ctrl+1..9` jumps.
- Pinned tabs sort first and are not closed by "close others".
- Split: `Ctrl+\` splits the document area vertically; tabs are draggable between panes.

### (E) Document area

Owned entirely by the active document ViewModel. The shell provides only the container,
the split behaviour, and the empty state.

### (F) Bottom panel

Tabs: **Results** (the active document's result set), **Messages** (`RAISE NOTICE`,
warnings), **History** (scoped to this connection), **Notices** (server notices with
severity). Collapsible via `Ctrl+J`; height persisted. A badge on Messages/Notices shows
unread counts and clears when the tab is viewed.

### (G) Status bar — the most information-dense strip in the app

Left to right:

| Segment | Content | Click action |
|---|---|---|
| Environment pill | Colour dot + `PRODUCTION` | Opens the connection editor |
| Connection | `database@host:port` | Switch database menu |
| Server | `PG 16.2` | Shows the capability report |
| Mode | `READ ONLY` / `SAFE MODE` badge | Toggles (with confirmation on Production) |
| Pending changes | `3 pending` | Opens [wf-07](wf-07-pending-changes.md) |
| Transaction | `tx 00:42` with a warning tint above 60 s | Opens transaction controls |
| Last query | `1,204 rows in 182 ms` | Focuses the result |
| Background work | Spinner + label + cancel | Cancels that operation |
| Bypass counter | `2 overrides` (only when > 0) | Opens the audit log |

---

## 5. States

| State | Shell behaviour |
|---|---|
| **No connections configured** | Full-area first-run panel: "Paste a connection URI", "New connection", "Import from...". No empty chrome. |
| **Connections exist, none open** | Connection picker list in the document area, most-recent first, keyboard-selectable. |
| **Connecting** | Tab shows a spinner; sidebar shows skeleton rows; document area shows what is being done ("authenticating", "probing capabilities"), each cancellable. |
| **Connection failed** | Tab turns `status.danger`; document area shows the mapped error, the exact next step, and Retry / Edit connection. |
| **Reconnecting** | Non-modal banner above the document area with attempt count and a Cancel. Documents keep their content. |
| **Read-only / safe mode** | Persistent status-bar badge; write actions in menus and context menus are visibly disabled with a tooltip explaining why. |
| **Restoring workspace** | Tabs appear immediately with skeleton content; Production connections show "click to reconnect" rather than auto-connecting. |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Ctrl+B` | Toggle sidebar |
| `Ctrl+J` | Toggle bottom panel |
| `Ctrl+\` | Split document area |
| `Ctrl+W` / `Ctrl+Shift+T` | Close / reopen document tab |
| `Ctrl+Tab` / `Ctrl+Shift+Tab` | Cycle documents (MRU) |
| `Ctrl+1..9` | Go to document N |
| `Ctrl+Alt+1..9` | Go to connection N |
| `Ctrl+N` | New SQL document |
| `Ctrl+Shift+N` | New connection |
| `F11` | Fullscreen |
| `Escape` | Cancel innermost operation |

Window state (size, position, maximised, monitor), sidebar width, panel height, split
layout and tab order are all persisted and restored.

---

## 7. Data contract

```csharp
public interface IWorkspaceService
{
    WorkspaceState Current { get; }
    Task<Result> RestoreAsync(CancellationToken ct);
    Task SaveAsync(CancellationToken ct);                 // debounced, 1 s
    event EventHandler<WorkspaceChangedEventArgs>? Changed;
}

public interface ISessionManager
{
    IReadOnlyList<IDatabaseSession> Sessions { get; }
    IDatabaseSession? Active { get; }
    Task<Result<IDatabaseSession>> OpenAsync(ConnectionProfile p, CancellationToken ct);
    Task CloseAsync(Guid sessionId, CancellationToken ct);
    event EventHandler<SessionStateChangedEventArgs>? SessionStateChanged;
}

public interface IDocumentHost
{
    IReadOnlyList<IDocumentViewModel> Documents { get; }
    IDocumentViewModel? Active { get; }
    void Open(IDocumentViewModel document, bool pinned = false);
    Task<bool> CloseAsync(IDocumentViewModel document);
    void Reopen();                                        // Ctrl+Shift+T
}

public interface IDocumentViewModel : IDisposable
{
    string Title { get; }
    string IconKey { get; }
    bool IsDirty { get; }
    string? BadgeText { get; }
    Task<bool> CanCloseAsync();
}
```

Buildable against `InMemoryWorkspaceService` + `FakeSessionManager` from
`Basora.TestKit`. **No database is needed to build or test this screen.**

---

## 8. Acceptance criteria

- [ ] Connection tabs show environment colour; a Production connection draws the window
      border.
- [ ] Status bar shows all nine segments and each is clickable as specified.
- [ ] Sidebar and bottom panel toggle, resize, and persist their sizes.
- [ ] Document area splits and tabs drag between panes.
- [ ] Closing a tab with pending changes or an open transaction prompts; closing with
      unsaved SQL prompts; `Ctrl+Shift+T` restores the last closed tab with its content.
- [ ] Workspace restores tabs, sizes, split layout and unsaved SQL after a force-kill.
- [ ] Production connections do **not** auto-connect on restore.
- [ ] All seven states in section 5 render correctly in both themes.
- [ ] Full keyboard navigation with no mouse; focus is always visible.
- [ ] Headless test: shell renders in both themes with no missing resource.
