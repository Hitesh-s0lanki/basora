# wf-01 — Connection Manager

> Task: `T-C05`. Panel/dialog, not a document.

---

## 1. Purpose

Browse, organise, search and launch saved connections. This is the first screen a new
user meets and the screen an existing user hits dozens of times a day, so it optimises
for two different things at once: **an empty state that gets you connected in under two
minutes**, and **a populated state where the right connection is one keystroke away**.

The single job it must do well: make it impossible to connect to Production when you
meant Staging.

---

## 2. Entry points

- App launch with no active connection.
- `+` on the connection tab strip ([wf-03](wf-03-app-shell.md) region B).
- `Ctrl+Shift+N`, or File > New Connection.
- Command palette: "Manage connections".

---

## 3. Layout

### Populated

```text
+--------------------------------------------------------------------------+
|  Connections                                    [ + New ]  [ Import v ]   |
+--------------------------------------------------------------------------+
|  [ search connections...                                    Ctrl+F ]      |
|  [All] [Local] [Dev] [Staging] [Prod]                     [Grid|List]     |
+--------------------------------------------------------------------------+
|                                                                           |
|  RECENT                                                                   |
|  +---------------------------+  +---------------------------+            |
|  |# Production               |  |# Staging                  |            |
|  |  production@              |  |  staging@                 |            |
|  |  postgres.company.com     |  |  staging.company.com      |            |
|  |  PROD - SSH - VerifyFull  |  |  STAGING - SSL Require    |            |
|  |  2 hours ago              |  |  yesterday                |            |
|  +---------------------------+  +---------------------------+            |
|                                                                           |
|  v  WORK  (4)                                                             |
|  +---------------------------+  +---------------------------+            |
|  |# analytics                |  |# local                    |            |
|  |  analytics@10.0.4.12      |  |  app_dev@localhost        |            |
|  |  DEV - SSL Prefer         |  |  LOCAL                    |            |
|  +---------------------------+  +---------------------------+            |
|                                                                           |
|  >  SIDE PROJECTS  (2)                                                    |
+--------------------------------------------------------------------------+
|  8 connections - Secrets: Windows Credential Manager (OS-backed)          |
+--------------------------------------------------------------------------+
```

### Empty (first run)

```text
+--------------------------------------------------------------------------+
|                                                                           |
|                        Connect to PostgreSQL                              |
|                                                                           |
|   +-------------------------------------------------------------------+   |
|   |  Paste a connection URI                                           |   |
|   |  postgresql://user:password@host:5432/database                    |   |
|   |  [                                                    ] [Connect] |   |
|   +-------------------------------------------------------------------+   |
|                                                                           |
|                  [ New connection ]    [ Import from... ]                 |
|                                                                           |
|   Import supports: ~/.pgpass, ~/.pg_service.conf, TablePlus, DBeaver,     |
|   pgAdmin, DATABASE_URL from a .env file                                  |
|                                                                           |
+--------------------------------------------------------------------------+
```

The URI field is **focused on open**, so a user who already has a URI in the clipboard
is one `Ctrl+V Enter` from a database.

---

## 4. Component inventory

### Toolbar

- **New** — opens [wf-02 Connection Editor](wf-02-connection-editor.md) blank.
- **Import** — dropdown: pgpass, pg_service.conf, `.env` `DATABASE_URL`, TablePlus,
  DBeaver, pgAdmin. Each shows a preview list with checkboxes before importing anything.
- **View toggle** — grid (cards) or list (dense table, sortable columns). Persisted.

### Search and filters

- Search matches name, host, database, username and tags, fuzzily, as you type.
- Environment filter chips act as an OR filter; the active chip carries its env colour.
- `Ctrl+F` focuses search; `Escape` clears it; `Down` moves into results.

### Connection card

| Element | Detail |
|---|---|
| Accent bar | 4px left bar in the environment colour |
| Name | `type.heading` |
| Target | `database@host:port`, `type.caption`, `text.secondary` |
| Badges | Environment, `SSH`, SSL mode, `READ ONLY` if defaulted |
| Warning badge | `status.warning` when Production + weak SSL, or secret store unavailable |
| Last connected | Relative time, `text.tertiary` |
| Hover actions | Connect, Edit, Duplicate, More (…) |

Double-click or `Enter` connects. `Alt+Enter` edits. Right-click menu: Connect, Connect
in new window, Edit, Duplicate, Copy URI (**without password** — always, no option to
include it), Export, Move to folder, Delete.

### Folders

Drag cards between folders; folders collapse and their state persists. Group headers
show a count.

### Footer

Connection count, and **which secret backend is in use** with an OS-backed/not-OS-backed
indicator. If the backend is unavailable, this becomes a `status.warning` row with a fix
action — the user must never discover a broken keystore at connect time.

---

## 5. States

| State | Rendering |
|---|---|
| **Loading** | Skeleton cards. Under 100 ms in practice; still implemented |
| **Empty (first run)** | The panel in section 3 |
| **Empty (search)** | "No connections match 'xyz'" + Clear search + New connection |
| **Secret store unavailable** | Banner naming the backend and the failure, with "Use encrypted vault instead" and "Retry". Cards remain usable; connecting prompts for the password |
| **Connecting** | Card shows an inline progress row with the current step and a Cancel |
| **Connection failed** | Card border turns `status.danger`; inline error text with the mapped message; Retry and Edit |
| **Import preview** | List of discovered connections with checkboxes, conflict markers for duplicate names, and a per-row note on whether a password was found |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Ctrl+F` | Focus search |
| Arrows | Move between cards (2D navigation in grid view) |
| `Enter` | Connect to the selected connection |
| `Alt+Enter` | Edit |
| `Ctrl+D` | Duplicate |
| `Delete` | Delete (always confirms, naming the connection) |
| `Ctrl+Shift+N` | New connection |
| `Ctrl+V` in empty state | Paste URI and parse |
| `Escape` | Close the manager |

**Safety detail:** deleting a Production-tagged connection requires typing its name.
Duplicating a Production connection resets the new copy's environment to `Development`
and clears its `SecretRef`, so a duplicate is never silently a second production door.

---

## 7. Data contract

```csharp
public interface IConnectionRegistry
{
    IReadOnlyList<ConnectionProfile> Profiles { get; }
    IReadOnlyList<ConnectionFolder> Folders { get; }
    Task<Result<ConnectionProfile>> SaveAsync(ConnectionProfile p, CancellationToken ct);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
    Task<Result<ConnectionProfile>> DuplicateAsync(Guid id, CancellationToken ct);
    Task<Result> ReorderAsync(IReadOnlyList<Guid> order, CancellationToken ct);
    event EventHandler? Changed;
}

public interface IConnectionImporter
{
    string SourceName { get; }
    bool IsAvailable { get; }
    Task<Result<IReadOnlyList<ImportedConnection>>> DiscoverAsync(CancellationToken ct);
}

public sealed record ImportedConnection(
    ConnectionProfile Profile, bool HasPassword, string? SourcePath, string? Warning);

public sealed record ConnectionFolder(Guid Id, string Name, Guid? ParentId, int SortOrder);
```

Also consumes `ISecretStore.BackendName` / `IsHardwareBacked` / `HealthCheckAsync` for
the footer, and `ISessionManager.OpenAsync` to connect.

Buildable against `InMemoryConnectionRegistry` — **no database required.**

---

## 8. Acceptance criteria

- [ ] Empty state focuses the URI field; paste + `Enter` reaches the editor pre-filled.
- [ ] Search filters across name, host, database, username and tags as you type.
- [ ] Environment filter chips work and carry their colour.
- [ ] Cards show the environment accent, badges, and last-connected time.
- [ ] A Production connection with `sslmode` below `VerifyCa` shows a warning badge.
- [ ] "Copy URI" never includes a password, and there is no option to include one.
- [ ] Deleting a Production connection requires typing its name.
- [ ] Duplicating a Production connection resets environment and clears the secret ref.
- [ ] Footer names the secret backend and whether it is OS-backed.
- [ ] Secret-store failure shows the banner and connecting still works via prompting.
- [ ] Import previews before writing anything and flags name conflicts.
- [ ] Full keyboard operation including 2D arrow navigation in grid view.
- [ ] All seven states render in both themes.
