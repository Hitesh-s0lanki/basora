# wf-10 — Query History and Favorites

> Task: `T-H03`. Sidebar panels (selectable in [wf-03](wf-03-app-shell.md) region C) and
> a bottom-panel tab in region F.

---

## 1. Purpose

Every query ever run, findable; and the good ones, kept. The one job it must do well:
**"what was that query I ran last Tuesday?"** answered in seconds.

---

## 2. Entry points

- `Ctrl+Shift+H` (history), `Ctrl+Shift+B` (favorites).
- The History tab in the bottom panel.
- `Ctrl+Up` / `Ctrl+Down` in the editor cycles history in place.
- `Ctrl+D` in the editor saves the current statement as a favorite.

---

## 3. Layout

### History

```text
+----------------------------------------+
| [ search history...            ] [v]   |
| [All] [Success] [Failed] [Slow]        |
+----------------------------------------+
| TODAY                                  |
|  10:32  SELECT * FROM users WHERE ...  |
|         Production - 1,204 rows - 182ms|
|  10:12  UPDATE orders SET status = ... |
|         Production - 47 rows - 1.2s    |
|  09:41  SELECT count(*) FROM events    |
|         Staging - 1 row - 4.8s   [slow]|
|                                        |
| YESTERDAY                              |
|  17:03  DELETE FROM sessions WHERE ... |
|         Local - 3,281 rows - 88ms      |
|  16:58  x ALTER TABLE users ADD ...    |
|         Production - 42501 denied      |
+----------------------------------------+
| 4,182 entries - 30 days retained       |
+----------------------------------------+
```

### Favorites

```text
+----------------------------------------+
| [ search favorites...          ] [ + ] |
+----------------------------------------+
| v FINANCE                              |
|   *  Customer Revenue                  |
|   *  Failed Payments                   |
| v USERS                                |
|   *  Active Users (30d)                |
|   *  Signup Funnel                     |
| v DEBUGGING                            |
|   *  Blocking Queries                  |
|   *  Table Sizes                       |
| > PRODUCTION (4)                       |
+----------------------------------------+
```

---

## 4. Component inventory

### History entry

Time, first line of SQL (truncated, monospace), and a metadata line: connection name with
its environment colour dot, row count, duration, and a status glyph. Failed entries show
the SQLSTATE. Slow entries (above a configurable threshold) carry a `slow` badge.

Identical consecutive statements collapse into one entry with a run count and the most
recent timestamp — otherwise a debugging session floods the list with the same query.

Hover/context actions: Open in editor, Open in new tab, Copy, Save as favorite, Delete,
Delete all from this connection.

### Search and filters

Full-text over SQL, plus filter chips for outcome and speed, and a `[v]` menu for
connection, environment, and date range. Search is instant on a local store.

### Favorites tree

Folders, drag to reorder and re-parent. An entry stores name, SQL, optional description,
tags, and an optional connection binding (a favorite bound to a connection only appears
when that connection is active; unbound favorites appear everywhere).

`Ctrl+D` in the editor opens a small save popover with the name pre-filled from the first
line of the query and a folder picker.

### Retention and privacy

Settings control: retention days, maximum entries, whether to record failed queries,
and **per-connection opt-out**. A redaction list suppresses statements matching given
patterns. History is local only and never leaves the machine.

**Rule:** history records statement text and metadata, never result values.

---

## 5. States

| State | Rendering |
|---|---|
| **Loading** | Skeleton rows (local store, so rarely visible) |
| **Empty (history)** | "No queries yet. Run one with Ctrl+Enter." |
| **Empty (search)** | "No queries match 'x'" + Clear |
| **Empty (favorites)** | "Save a query with Ctrl+D to keep it here" |
| **Recording disabled** | Banner: "History is off for this connection" + Enable |
| **Store error** | Banner naming the failure with Retry and "Reveal file"; the app keeps working without history |
| **Storage near limit** | Notice offering to prune, with the count that would be removed |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Ctrl+Shift+H` / `Ctrl+Shift+B` | Focus history / favorites |
| `Ctrl+F` | Search within the panel |
| `Enter` | Open in the active editor (replacing content prompts if dirty) |
| `Ctrl+Enter` | Open in a new editor tab |
| `Ctrl+C` | Copy SQL |
| `Ctrl+D` | Save the selected history entry as a favorite |
| `Delete` | Delete the entry (confirms for favorites, not for history) |
| `Ctrl+Up` / `Ctrl+Down` | In the editor: previous / next history entry in place |
| `F2` | Rename a favorite |

---

## 7. Data contract

```csharp
public interface IHistoryStore
{
    Task<Result> RecordAsync(QueryHistoryEntry entry, CancellationToken ct);
    Task<Result<IReadOnlyList<QueryHistoryEntry>>> SearchAsync(
        HistoryQuery query, CancellationToken ct);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
    Task<Result> PruneAsync(HistoryRetentionPolicy policy, CancellationToken ct);
    Task<Result<HistoryStats>> GetStatsAsync(CancellationToken ct);
}

public sealed record HistoryQuery(
    string? Text, Guid? ConnectionId, DeploymentEnvironment? Environment,
    bool? SucceededOnly, TimeSpan? MinDuration,
    DateTimeOffset? From, DateTimeOffset? To, int Skip, int Take);

public interface IFavoritesStore
{
    Task<Result<IReadOnlyList<SavedQuery>>> GetAllAsync(CancellationToken ct);
    Task<Result<SavedQuery>> SaveAsync(SavedQuery query, CancellationToken ct);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
    Task<Result<IReadOnlyList<QueryFolder>>> GetFoldersAsync(CancellationToken ct);
}
```

Both are local-only stores in `Basora.Infrastructure`. Recording is fire-and-forget from
the executor's perspective and **must never delay or fail a query** — a history write
error is logged and surfaced in the panel, never in the query path.

---

## 8. Acceptance criteria

- [ ] Every executed statement is recorded with connection, environment, duration, row
      count and outcome — including failures with their SQLSTATE.
- [ ] Result values are never recorded.
- [ ] Identical consecutive statements collapse with a run count.
- [ ] Search over 100,000 entries returns in under 100 ms.
- [ ] Filters for outcome, speed, connection, environment and date range all work.
- [ ] `Ctrl+Up`/`Ctrl+Down` cycles history in the editor without losing the current text.
- [ ] Favorites support folders, tags, descriptions and optional connection binding.
- [ ] `Ctrl+D` saves with the name pre-filled from the query.
- [ ] Per-connection opt-out and pattern redaction both work.
- [ ] Retention pruning runs on schedule and can be triggered manually.
- [ ] A history store failure never blocks or fails a query.
- [ ] All seven states render in both themes.
