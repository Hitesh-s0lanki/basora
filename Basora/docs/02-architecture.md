# 02 — Architecture

> Read this before writing any code. The layer rules in section 3 are enforced by build
> checks (`T-F02`) — violating them fails CI, not code review.

---

## 1. Shape of the system

```text
                            BASORA
                               |
              +----------------+----------------+
              |                                 |
          UI Layer                        Application Layer
        (Avalonia XAML)                   (ViewModels, Commands)
              |                                 |
              +----------------+----------------+
                               |
                          Core Engine
                    (contracts, domain, rules)
                               |
        +----------------------+----------------------+
        |                      |                      |
     Npgsql               Analyzers                  AI
   (data access)      (intelligence layer)      (assistants)
        |                      |                      |
        +----------------------+----------------------+
                               |
                          PostgreSQL
```

The important property: **`Basora.Core` knows nothing about Avalonia and nothing about
Npgsql.** It defines contracts and domain types. Everything else plugs into it. That is
what makes the whole plan parallelisable and the whole thing testable without a
database or a window.

---

## 2. Solution layout

```text
Basora.slnx
│
├── src/
│   ├── Basora.App/                  Entry point, composition root, DI wiring
│   │   ├── Program.cs
│   │   ├── App.axaml(.cs)
│   │   ├── Composition/             ServiceCollection extensions per module
│   │   └── app.manifest
│   │
│   ├── Basora.UI/                   All views, view models, controls, themes
│   │   ├── Views/                   One folder per feature area
│   │   ├── ViewModels/              Mirrors Views/ one-for-one
│   │   ├── Controls/                Reusable custom controls (DataGrid, SqlEditor...)
│   │   ├── Converters/
│   │   ├── Behaviors/
│   │   ├── Themes/                  Light.axaml, Dark.axaml, Tokens.axaml
│   │   ├── Styles/                  Per-control style dictionaries
│   │   └── Services/                UI-only services (dialogs, clipboard, notify)
│   │
│   ├── Basora.Core/                 NO framework dependencies. The contract layer.
│   │   ├── Models/                  Domain records (see 04-domain-model.md)
│   │   ├── Interfaces/              Every I* contract in the product
│   │   ├── Commands/                Command/result DTOs
│   │   ├── Results/                 Result<T>, Error, Outcome types
│   │   └── Services/                Pure implementations (SQL builders, rules)
│   │
│   ├── Basora.PostgreSQL/           Npgsql-facing. The only project that talks to PG.
│   │   ├── Connection/              Pooling, session lifecycle, server probing
│   │   ├── Metadata/                Catalog queries -> domain descriptors
│   │   ├── Queries/                 Execution, streaming, cancellation
│   │   ├── Schema/                  DDL generation, structure edits
│   │   ├── Monitoring/              pg_stat_*, pg_locks, activity
│   │   ├── Explain/                 EXPLAIN capture + plan parsing
│   │   ├── Copy/                    COPY-based import/export
│   │   └── Backup/                  pg_dump / pg_restore orchestration
│   │
│   ├── Basora.Analytics/            Pure analysis over metadata + stats. No I/O.
│   │   ├── QueryAnalyzer/
│   │   ├── IndexAnalyzer/
│   │   ├── SchemaAnalyzer/
│   │   ├── MigrationAnalyzer/
│   │   └── HealthAnalyzer/
│   │
│   ├── Basora.AI/                   Provider-agnostic assistant + context builder
│   ├── Basora.Security/             Secret storage, SSH tunnels, redaction, audit
│   └── Basora.Infrastructure/       Settings, workspace persistence, logging, files
│
├── tests/
│   ├── Basora.Core.Tests/
│   ├── Basora.PostgreSQL.Tests/     Testcontainers-backed integration tests
│   ├── Basora.Analytics.Tests/
│   ├── Basora.Security.Tests/
│   └── Basora.UI.Tests/             Avalonia.Headless
│
└── docs/                            This folder
```

`src/` and `tests/` prefixes are new — the current repo has the project at the root.
`T-F01` performs that move.

---

## 3. Layer dependency rules

Allowed references (arrow = "may reference"):

```text
Basora.App        ->  UI, Core, PostgreSQL, Analytics, AI, Security, Infrastructure
Basora.UI         ->  Core, Infrastructure
Basora.PostgreSQL ->  Core, Security
Basora.Analytics  ->  Core
Basora.AI         ->  Core
Basora.Security   ->  Core
Basora.Infrastructure -> Core
Basora.Core       ->  (nothing)
```

Hard rules, enforced by an architecture test in `Basora.Core.Tests`:

1. **`Basora.Core` references no other Basora project and no UI/data framework.**
   Its only permitted third-party dependency is `System.*`.
2. **`Basora.UI` never references `Basora.PostgreSQL`.** ViewModels talk to `I*`
   interfaces from `Core`. If a ViewModel needs Npgsql, the contract is wrong.
3. **`Basora.PostgreSQL` is the sole owner of `NpgsqlConnection`.** No other project
   may reference the Npgsql package.
4. **`Basora.Analytics` performs no I/O.** It receives already-fetched statistics and
   returns findings. This makes every analyzer trivially unit-testable.
5. **Nothing references `Basora.App`.**

### Why this matters for parallel work

Because `Core` is dependency-free and lands in Wave 0, a team member building the
Index Analyzer and a team member building the SQL Editor never touch the same file and
never wait for each other. Both consume `Core` contracts; both test against fakes.

---

## 4. Application architecture

### 4.1 MVVM

- **View** (`.axaml`) — layout and binding only. No logic in code-behind beyond
  `InitializeComponent`, control-specific plumbing, and focus management.
- **ViewModel** — `ObservableObject` from CommunityToolkit.Mvvm, using
  `[ObservableProperty]` and `[RelayCommand]` source generators. No Avalonia types
  except where genuinely unavoidable (document behind an interface if so).
- **Model** — records from `Basora.Core.Models`. Immutable by default.

`ViewLocator` maps `Foo.ViewModels.BarViewModel` to `Foo.Views.BarView` by convention.
Every ViewModel derives from `ViewModelBase`.

### 4.2 Dependency injection

`Microsoft.Extensions.DependencyInjection`, composed in `Basora.App/Composition/`. Each
module contributes one extension method so tasks append a file rather than editing a
shared one:

```csharp
services
    .AddBasoraCore()
    .AddBasoraSecurity()
    .AddBasoraPostgreSql()
    .AddBasoraAnalytics()
    .AddBasoraInfrastructure()
    .AddBasoraAi()
    .AddBasoraUi();
```

Lifetimes:

| Kind | Lifetime | Notes |
|---|---|---|
| Stateless services, analyzers, SQL builders | Singleton | Must be thread-safe |
| `IConnectionRegistry`, `ISecretStore`, `ISettingsService` | Singleton | App-wide state |
| Per-connection session (`IDatabaseSession`) | Managed by `ISessionManager` | Not DI-resolved; created per connection |
| ViewModels | Transient | Resolved via `IViewModelFactory` |
| Document/tab ViewModels | Transient, owned by their tab | Disposed with the tab |

### 4.3 Threading model

- All database work is `async` all the way down. No `.Result`, no `.Wait()`, no
  `async void` outside event handlers.
- ViewModels marshal to the UI thread through `Dispatcher.UIThread`. Services never
  touch the dispatcher — that is a UI-layer concern.
- Long-running work is owned by a `CancellationTokenSource` held by the ViewModel and
  cancelled on tab close, navigation away, or explicit user cancel.
- **Every public async method in `Core` interfaces takes a `CancellationToken`.** No
  exceptions. This is checked by the architecture test.

### 4.4 Error handling

`Core` defines a `Result<T>` type. Expected failures (connection refused, permission
denied, syntax error, timeout) are returned as `Result<T>` and rendered as inline UI
states. Unexpected failures throw, are logged with Serilog, and surface in a global
error surface without killing the app.

Postgres errors carry `SqlState`; `Core` maps well-known states to friendly diagnostics
(for example `42501` -> "the role lacks privilege on this object", plus the exact GRANT
that would fix it).

### 4.5 Messaging

`CommunityToolkit.Mvvm.Messaging.IMessenger` for cross-ViewModel events, with strongly
typed messages in `Basora.Core/Messages/`. Used sparingly: schema-changed, connection
state changed, theme changed, pending-changes count changed. Anything else goes through
a service.

---

## 5. Session and connection model

```text
ConnectionProfile        Saved, serialisable, no secret material inline
        |
        |  connect
        v
IDatabaseSession         Live. Owns an Npgsql data source + server capability info.
        |
        +-- QueryChannel     Pooled connection for user queries (cancellable)
        +-- MetadataChannel  Separate connection so tree loads never queue behind a
        |                    long-running user query
        +-- MonitorChannel   Optional, for dashboard polling
```

Rationale for multiple channels: a user running a 40-second report must not freeze the
object tree, and a monitoring poll must not be cancelled when the user cancels a query.

Server capabilities are probed once per session (version, available extensions,
`pg_stat_statements` presence, role attributes) and cached on the session. Features gate
on capabilities, never on assumptions — see `05-postgresql-data-layer.md`.

---

## 6. Workspace and document model

```text
Workspace  (persisted per user)
 └── Connection Tab              one per open connection
      ├── Object Explorer state  expanded nodes, scroll position, filter
      └── Documents              open editors and browsers
           ├── SqlDocument       editor text + results + history cursor
           ├── TableDocument     table + active sub-tab + filters + pending changes
           ├── DiagramDocument
           └── DashboardDocument
```

Persisted to disk on change (debounced) so a crash or restart restores the exact
working state, including unsaved SQL text. Pending data changes are **not** persisted —
they are discarded with a warning on close.

---

## 7. Extensibility seam (Phase 3+)

Analyzers, result renderers, export formats and AI providers are all registered through
DI collections (`IEnumerable<IIndexRule>`, `IEnumerable<IExportFormat>`, ...). A plugin
is, mechanically, an assembly that contributes to those collections. Building against
this shape now costs nothing and makes the Phase 3 plugin story an infrastructure task
rather than a rewrite.

---

## 8. What can be built in parallel, structurally

| Track | Depends only on | Can start after |
|---|---|---|
| Metadata queries | `Core.Interfaces` + a real PG in Testcontainers | Wave 0 |
| Data grid control | `Core.Models` + fake row source | Wave 0 |
| SQL editor control | AvaloniaEdit + `Core` completion contract | Wave 0 |
| Analyzers | `Core.Models` stat records | Wave 0 |
| Secret storage | `Core.Interfaces.ISecretStore` | Wave 0 |
| Theming and design tokens | Nothing | Wave 0 |
| Every wireframe screen | Its own contract + fake | Wave 0 |

This is the whole point of the Wave 0 contract push: after roughly one week of
foundation work, more than a dozen tracks proceed independently.
