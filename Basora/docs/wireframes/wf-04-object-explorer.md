# wf-04 — Object Explorer

> Task: `T-E03`. Sidebar panel inside [wf-03](wf-03-app-shell.md) region C.

---

## 1. Purpose

The navigation tree for everything in a PostgreSQL server. Its one job: **stay fast and
responsive on a database with 5,000 tables**, which means lazy loading, virtualisation,
and a filter that beats scrolling.

---

## 2. Entry points

- Always present in the sidebar when a connection is active.
- `Ctrl+Shift+E` focuses it.
- "Reveal in explorer" from a document tab, a search result, or a query error.

---

## 3. Layout

```text
+----------------------------------+
| [ filter objects...        ] [v] |   <- [v] = filter options menu
+----------------------------------+
| v  production                    |
|    v  Schemas                    |
|       v  public            (142) |
|          v  Tables          (86) |
|             #  users        1.2M |
|             #  orders       428M |
|             #  order_items       |
|             >  events    [part.] |
|          >  Views           (12) |
|          >  Materialized V.  (3) |
|          >  Functions       (27) |
|          >  Procedures       (4) |
|          >  Sequences       (18) |
|          >  Types            (9) |
|          >  Triggers        (31) |
|       >  analytics          (18) |
|       >  audit               (6) |
|    >  Roles                  (7) |
|    >  Extensions             (5) |
|    >  Foreign Data Wrappers  (1) |
|    >  Policies               (3) |
+----------------------------------+
| 142 objects - refreshed 2 min ago|
+----------------------------------+
```

Node rows are one line: expander, kind icon, name, then right-aligned metadata (row
estimate or size) in `text.tertiary`. Badges (`part.`, `unlogged`, `matview`, `INVALID`)
sit inline after the name.

---

## 4. Component inventory

### Filter bar

- Types to filter across **all loaded levels**, and matches are shown with their
  ancestors auto-expanded. Non-matching branches collapse.
- Match is fuzzy on name; `schema.table` syntax scopes it.
- The options menu `[v]` toggles: show system schemas, show empty folders, show sizes,
  show row estimates, group by schema vs by kind.
- Filtering never triggers a server round trip — it filters what is loaded. For
  server-wide search, `Ctrl+P` ([wf-15](wf-15-command-palette.md)) is the right tool, and
  the filter bar says so when a filter yields nothing.

### Tree

- **Virtualised.** Only realised rows exist. A 5,000-table schema must scroll at 60 fps.
- **Lazily loaded.** Expanding a node fetches only that level, on the metadata channel,
  with a skeleton row while loading and an inline error row on failure.
- Counts on folder nodes are fetched with the parent level, not by expanding.
- Multi-select with `Ctrl`/`Shift` for bulk actions (export several tables, drop several
  objects through one safety dialog).
- Drag a table into the SQL editor to insert its qualified name; drag with `Shift` to
  insert `SELECT * FROM ...`.

### Context menu (table)

```text
Open Data                      Enter
Open Structure
Open in new tab                Ctrl+Enter
------
Copy name / qualified name     Ctrl+C
Copy CREATE statement          Ctrl+Shift+C
Copy SELECT / INSERT template
------
Export data...
Import data...
------
Truncate...            (safety ladder)
Drop...                (safety ladder)
------
Analyze / Vacuum
Refresh                        F5
Properties                     Alt+Enter
```

Menus differ per object kind and **hide** actions the current role cannot perform,
rather than showing them disabled with no explanation.

### Footer

Object count for the current scope and the metadata cache age, with a click to refresh.

---

## 5. States

| State | Rendering |
|---|---|
| **Loading (root)** | Skeleton rows, cancellable after 2 s |
| **Loading (expand)** | A single skeleton child row under the expanding node |
| **Empty schema** | "No tables in this schema" with a Create table action |
| **Filter no match** | "No loaded objects match 'x'. Search the whole server with Ctrl+P" |
| **Permission denied** | The node renders with a lock glyph and an inline note naming the privilege needed |
| **Error** | Inline red row under the node with the mapped message and a Retry |
| **Stale** | Footer shows the cache age; above 5 minutes it dims and offers refresh |
| **Disconnected** | Tree greys out, keeps its shape and expansion state, shows "Reconnect" |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| Type letters | Incremental filter |
| `Up`/`Down` | Move |
| `Right`/`Left` | Expand / collapse |
| `Ctrl+Right` | Expand recursively (bounded — warns above 500 children) |
| `Enter` | Open default document for the object |
| `Alt+Enter` | Properties |
| `F5` | Refresh node |
| `Ctrl+C` / `Ctrl+Shift+C` | Copy qualified name / CREATE statement |
| `Delete` | Drop (safety ladder) |
| `Escape` | Clear filter, then return focus to the document |

Expansion state, scroll position and filter text persist per connection across restarts.

---

## 7. Data contract

```csharp
public interface IMetadataService
{
    Task<Result<IReadOnlyList<ObjectNode>>> GetChildrenAsync(
        IDatabaseSession session, DbObjectRef? parent, CancellationToken ct);

    Task<Result<TableDescriptor>> GetTableAsync(
        IDatabaseSession session, DbObjectRef table, CancellationToken ct);

    Task<Result<string>> GetDefinitionAsync(
        IDatabaseSession session, DbObjectRef obj, CancellationToken ct);

    Task InvalidateAsync(IDatabaseSession session, DbObjectRef? scope, CancellationToken ct);

    DateTimeOffset? GetCacheAge(IDatabaseSession session, DbObjectRef? scope);
}
```

Buildable against `FakeMetadataService` seeded with a synthetic 5,000-table tree — which
is also how the virtualisation performance test is written.

---

## 8. Acceptance criteria

- [ ] 5,000 tables in one schema scroll at 60 fps with constant realised-row count.
- [ ] Expanding a node loads only that level and only on the metadata channel.
- [ ] Expanding never blocks on a long-running user query.
- [ ] Filter matches across loaded levels with ancestors expanded, and offers `Ctrl+P`
      when nothing matches.
- [ ] Counts, row estimates and sizes render, with estimates marked `~`.
- [ ] Context menus differ per object kind and hide unavailable actions.
- [ ] Permission-denied nodes explain the missing privilege inline.
- [ ] Expansion state, scroll and filter persist per connection across restarts.
- [ ] Drag to editor inserts the qualified name (and `SELECT *` with Shift).
- [ ] All seven states render in both themes.
- [ ] Full keyboard operation with type-ahead.
