# Wireframes — Index and Conventions

Each file in this folder specifies **one screen or surface** and is deliberately
**self-contained**: it can be picked up and built without reading any other wireframe.
That is what lets these be worked in parallel.

---

## Index

### MVP 1

| # | Screen | Task | Depends on |
|---|---|---|---|
| [01](wf-01-connection-manager.md) | Connection Manager | `T-C05` | Shell, `IConnectionRegistry` |
| [02](wf-02-connection-editor.md) | Connection Editor | `T-C06` | `ISecretStore`, `ISshTunnelManager` |
| [03](wf-03-app-shell.md) | App Shell (tabs, panes, status bar) | `T-U03` | Nothing — build first |
| [04](wf-04-object-explorer.md) | Object Explorer sidebar | `T-E03` | `IMetadataService` |
| [05](wf-05-table-document.md) | Table Document + Data Grid | `T-D03` | `ITableDataService`, grid control |
| [06](wf-06-filter-builder.md) | Advanced Filter Builder | `T-D07` | Filter AST |
| [07](wf-07-pending-changes.md) | Pending Changes / SQL Preview | `T-D06` | Change set, SQL generator |
| [08](wf-08-sql-editor.md) | SQL Editor | `T-Q06` | AvaloniaEdit, completion provider |
| [09](wf-09-result-viewer.md) | Query Result Viewer | `T-Q07` | Streaming contract |
| [10](wf-10-history-favorites.md) | Query History and Favorites | `T-H03` | `IHistoryStore` |
| [11](wf-11-structure-editor.md) | Table Structure Editor | `T-M03` | DDL generator |
| [12](wf-12-indexes-constraints.md) | Indexes, Constraints, Relations | `T-M05` | Metadata descriptors |
| [13](wf-13-import-wizard.md) | Import Wizard | `T-X03` | COPY import |
| [14](wf-14-export-dialog.md) | Export Dialog | `T-X05` | COPY export |
| [15](wf-15-command-palette.md) | Command Palette + Open Anything | `T-U06` | `ICommandRegistry`, search |
| [16](wf-16-safety-dialogs.md) | Safety and Confirmation Dialogs | `T-P03` | Safety engine |
| [24](wf-24-settings.md) | Settings | `T-U08` | `ISettingsService` |

### MVP 2

| # | Screen | Task |
|---|---|---|
| [17](wf-17-er-diagram.md) | ER Diagram | `T-V01` |
| [18](wf-18-schema-diff.md) | Schema Diff and Migration Generator | `T-V03` |
| [19](wf-19-visual-explain.md) | Visual EXPLAIN | `T-V05` |
| [20](wf-20-health-dashboard.md) | Database Health Dashboard | `T-N01` |
| [21](wf-21-activity-locks.md) | Activity and Lock Analyzer | `T-N03` |
| [22](wf-22-storage-index-intel.md) | Storage and Index Intelligence | `T-N05` |

### MVP 3

| # | Screen | Task |
|---|---|---|
| [23](wf-23-ai-assistant.md) | AI Database Assistant | `T-AI03` |

---

## Document template

Every wireframe follows the same eight sections, in this order:

1. **Purpose** — one paragraph, and the one job the screen must do well.
2. **Entry points** — every way a user arrives here.
3. **Layout** — ASCII wireframe with regions labelled.
4. **Component inventory** — each region: what it contains, behaviour, tokens used.
5. **States** — all seven required states (`../07-design-system.md` section 9), stated
   concretely for this screen.
6. **Interactions and keyboard** — including shortcuts owned by this surface.
7. **Data contract** — the exact `Core` interfaces and models this screen consumes. This
   section is what makes the screen independently buildable against a fake.
8. **Acceptance criteria** — a checklist, testable, no ambiguity.

---

## Shared shell contract

Every document-type screen (05, 08, 09, 11, 12, 17, 18, 19, 20, 21, 22, 23) lives inside
the shell defined in [`wf-03-app-shell.md`](wf-03-app-shell.md) and may assume:

- It occupies the **document area** only. It does not draw the tab strip, sidebar,
  status bar, or menu.
- It receives an `IDocumentContext` giving it the active `IDatabaseSession`, the
  `DeploymentEnvironment`, a `CancellationToken` tied to the tab lifetime, and the
  `IMessenger`.
- It exposes `Title`, `IconKey`, `IsDirty` and `BadgeText` for the tab strip to render.
- It implements `Task<bool> CanCloseAsync()` so unsaved work can prompt.
- Its ViewModel is disposed when its tab closes; all its work is cancelled first.

Screens 01, 02, 06, 07, 13, 14, 15, 16 and 24 are **dialogs or panels**, not documents,
and say so in their own Purpose section.

---

## Fidelity note

These are **structural** wireframes: regions, hierarchy, states and behaviour. They fix
*what is on the screen and how it behaves*, not final pixel values. Visual values come
from `../07-design-system.md`; a wireframe never invents a colour or a size.

Where an ASCII drawing and the component inventory disagree, the **inventory wins** —
the drawing is a sketch, the inventory is the spec.
