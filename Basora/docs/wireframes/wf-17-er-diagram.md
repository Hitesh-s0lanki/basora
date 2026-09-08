# wf-17 — ER Diagram

> Task: `T-V01`. MVP 2. Document.

---

## 1. Purpose

Understand a schema's shape without reading 142 table definitions. The one job it must do
well: **stay legible on a real schema** — 150 tables must not render as a hairball. That
means auto-layout, focus modes, and aggressive default collapsing.

---

## 2. Entry points

- Right-click a schema > Show ER diagram (whole schema).
- "Show as diagram" from the Relations tab of a table ([wf-12](wf-12-indexes-constraints.md)) — opens
  scoped to that table's neighbourhood.
- Command palette: "Show ER diagram".

---

## 3. Layout

```text
+-----------------------------------------------------------------------------+
| [Fit] [100%] [+][-] | Layout [ Hierarchical v] | Show [ Keys only v]        |
| [ search tables... ] | Depth [ 2 v] from users  | [Export v] [ Refresh ]    |
+-----------------------------------------------------------------------------+
|                                                                             |
|     +---------------+              +------------------+                     |
|     | organizations |              | plans            |                     |
|     |---------------|              |------------------|                     |
|     | PK id         |<---+    +--->| PK id            |                     |
|     |    name       |    |    |    |    name          |                     |
|     +---------------+    |    |    +------------------+                     |
|                          |    |                                             |
|                     +----+----+----+                                        |
|                     | users        |                                        |
|                     |--------------|                                        |
|                     | PK id        |                                        |
|                     |    email  UQ |                                        |
|                     | FK org_id    |                                        |
|                     | FK plan_id   |                                        |
|                     +------+-------+                                        |
|                            | 1:N                                            |
|            +---------------+---------------+                                |
|            v               v               v                                |
|     +-----------+   +------------+   +-----------+                          |
|     | orders    |   | addresses  |   | sessions  |                          |
|     +-----------+   +------------+   +-----------+                          |
|                                                                             |
+-----------------------------------------------------------------------------+
| 12 of 142 tables shown - depth 2 from users - 18 relationships             |
+-----------------------------------------------------------------------------+
```

---

## 4. Component inventory

### Canvas

Skia-rendered, pan (drag / space-drag), zoom (`Ctrl`+wheel, pinch), fit-to-window,
zoom-to-selection. Positions are persisted per schema so a hand-arranged diagram survives
a restart.

### Table node

Header: table name and row estimate. Body: columns filtered by the Show mode.

| Show mode | Body |
|---|---|
| Keys only (default) | PK and FK columns only — the mode that keeps large schemas readable |
| All columns | Everything, with type |
| Names only | Column names, no types |
| Collapsed | Header only |

Badges: `PK`, `FK`, `UQ`, `NOT NULL`, partitioned, view. Views and matviews render with a
distinct border style so they are not mistaken for tables.

### Relationship edges

Routed orthogonally with collision avoidance. Cardinality notation at each end (crow's
foot). Hovering an edge highlights it, dims everything else, and shows the FK name, the
columns on both sides, and the `ON DELETE` / `ON UPDATE` actions.

Self-referencing FKs render as a loop; multiple FKs between the same pair render as
parallel edges, labelled.

### Focus and depth

The key to legibility. Select a table and set depth 1–3: only tables within that many FK
hops are shown. This is the default entry mode when arriving from a table's Relations
tab, and it is what makes a 150-table schema usable.

### Layouts

Hierarchical (default, follows FK direction), force-directed, grid, and manual. Switching
layouts animates positions so the user does not lose their place. Manual positions are
preserved when switching back.

### Search

Highlights matching tables and offers to focus one. Non-matching tables dim rather than
disappear, preserving spatial memory.

### Export

SVG (vector, with real text), PNG at a chosen scale, and DBML/Mermaid text for embedding
in documentation.

---

## 5. States

| State | Rendering |
|---|---|
| **Loading** | Progress with the count of tables and relationships loaded so far |
| **Empty schema** | "No tables in this schema" |
| **No relationships** | Tables laid out in a grid with a note that no foreign keys were found — which is itself a finding worth surfacing |
| **Too large** | Above 200 tables, prompt before rendering everything and default to focus mode on the largest hub table |
| **Permission denied** | Tables the role cannot see are omitted, with a note giving the count |
| **Error** | Inline strip with Retry |
| **Stale** | Notice that the schema changed since the diagram was drawn, with Refresh |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Ctrl+F` | Search tables |
| `+` / `-` / `Ctrl+0` | Zoom in / out / fit |
| Arrows | Pan |
| `Enter` | Open the selected table's document |
| `Space` | Toggle collapse on the selected table |
| `F` | Focus mode on the selected table |
| `1` / `2` / `3` | Set focus depth |
| `Ctrl+E` | Export |
| `Escape` | Clear focus and selection |

Double-clicking a table opens its document. Dragging a table to a new position persists
it and switches the layout to manual.

---

## 7. Data contract

```csharp
public interface ISchemaGraphService
{
    Task<Result<SchemaGraph>> BuildAsync(
        IDatabaseSession session, string schema, GraphOptions options, CancellationToken ct);

    SchemaGraph Focus(SchemaGraph graph, DbObjectRef root, int depth);
}

public sealed record SchemaGraph(
    IReadOnlyList<GraphNode> Nodes, IReadOnlyList<GraphEdge> Edges);

public sealed record GraphNode(
    DbObjectRef Ref, IReadOnlyList<ColumnDescriptor> Columns,
    long? EstimatedRows, DbObjectKind Kind);

public sealed record GraphEdge(
    string ForeignKeyName, DbObjectRef From, DbObjectRef To,
    IReadOnlyList<string> FromColumns, IReadOnlyList<string> ToColumns,
    Cardinality Cardinality, ReferentialAction OnDelete, ReferentialAction OnUpdate);

public interface IGraphLayoutEngine
{
    string Name { get; }
    Task<IReadOnlyDictionary<DbObjectRef, Point>> LayoutAsync(
        SchemaGraph graph, LayoutOptions options, CancellationToken ct);
}
```

Layout engines are DI-registered and pure — testable by asserting no node overlaps and
edge-crossing counts stay within bounds.

---

## 8. Acceptance criteria

- [ ] A 150-table schema renders and pans/zooms at 60 fps.
- [ ] Keys-only is the default show mode.
- [ ] Focus mode with depth 1–3 works from any table.
- [ ] Cardinality, FK names, columns and referential actions are correct on every edge.
- [ ] Self-referencing and multiple FKs between the same pair render correctly.
- [ ] Views and matviews are visually distinct from tables.
- [ ] Manual positions persist per schema and survive a layout switch and restart.
- [ ] SVG export contains real, selectable text.
- [ ] Above 200 tables, the user is prompted before a full render.
- [ ] Tables hidden by permissions are reported as a count, not silently dropped.
- [ ] All seven states render in both themes.
