# Basora

### PostgreSQL, understood.

A native, cross-platform PostgreSQL development, administration and observability
workspace for Windows, macOS and Linux.

> **Basora is a modern PostgreSQL desktop workspace that combines the everyday database
> workflow of TablePlus with the administration reach of pgAdmin, plus an intelligence
> layer for query performance, schema health, migration safety, production guardrails
> and AI-assisted database work.**

---

## Status

> **Pre-alpha — planning complete, implementation just started.**
>
> The repository contains a complete [design and task specification](docs/README.md) and
> the empty C# multi-project solution that `T-F01` laid down. Nothing described under
> "Features" below is implemented yet.

| Area | State |
|---|---|
| Product brief | Complete — [`docs/idea.md`](docs/idea.md) |
| Architecture, design system, specs | Complete — 52 documents |
| Wireframes | Complete — 24 screens |
| Task plan | Complete — 73 tasks for MVP 1 |
| Code | Solution skeleton — `T-F01` done, `T-F02` next |

---

## Why Basora

Every database client answers *"show me my database."* Basora is built to answer a harder
question:

> **What is happening in my PostgreSQL database, why is it happening, and what should I
> do about it?**

That single sentence is the tie-breaker for every scope decision. It produces a product
with two layers:

**Layer 1 — Database client.** Connections, SQL editor, table browser, data editor,
schema management, import/export, SSH, transactions, history, favorites, ER diagrams.
Table stakes: we copy proven patterns rather than inventing new ones.

**Layer 2 — Database intelligence.** The reason to switch:

| | |
|---|---|
| **Query intelligence** | Why is this query slow? |
| **Migration intelligence** | Is this migration safe to run *right now*? |
| **Schema intelligence** | What is wrong with my schema? |
| **Index intelligence** | Which indexes should I create or drop? |
| **Production intelligence** | What is hurting my database at this moment? |
| **Schema drift** | What changed between environments? |
| **AI assistant** | Explain and diagnose my *actual* database. |

### PostgreSQL only, on purpose

Phase 1 supports PostgreSQL and nothing else. Going deep buys things a multi-engine tool
structurally cannot have: real `EXPLAIN (ANALYZE, BUFFERS)` analysis, `pg_stat_statements`
integration, lock-tree analysis via `pg_locks`, bloat and autovacuum diagnostics,
per-version feature detection, and correct handling of PostgreSQL's actual type system —
arrays, ranges, composites, domains, enums, `jsonb`.

---

## Product principles

These are constraints, not aspirations. Each one is enforced by a spec, a test, or both.

1. **Never surprise a user in production.** Friction escalates with environment risk.
2. **Every mutation is previewable as SQL** before it runs. No exceptions.
3. **Recommend, never act** — especially for destructive suggestions like dropping an
   unused index.
4. **The UI never blocks.** Every database call is cancellable and off the UI thread.
5. **Keyboard first.** Anything reachable by mouse has a shortcut or a palette command.
6. **Explain the finding, not just the number.** "Cache hit 91%" is noise. "Cache hit
   dropped because `orders` no longer fits in `shared_buffers`" is a product.
7. **Degrade honestly.** If an extension is missing or a role lacks privilege, say exactly
   that and what to do — never show an empty panel or a misleading zero.
8. **Credentials never touch disk in plaintext**, never appear in a log, an export, a
   support bundle, or an AI request.

---

## Roadmap

### MVP 1 — a credible client
Connection manager · secure credential storage · SSH & TLS · object explorer · table
browser · data grid with inline editing · advanced filters · pending changes with SQL
preview · SQL editor with schema-aware autocomplete · streaming query execution with
cancellation · history · favorites · CSV import · CSV/JSON/SQL/Excel export · structure,
index and constraint editors · **production safety engine** · command palette · dark/light
themes.

*Exit criterion: a developer can delete their existing client for daily work.*

### MVP 2 — intelligence
ER diagram · schema diff · migration generator · visual `EXPLAIN` with findings · health
dashboard · activity & lock analyzer · storage analyzer · index intelligence · table
health · slow queries.

*Exit criterion: a developer diagnoses a real production slowdown entirely inside Basora.*

### MVP 3 — assistance
AI assistant grounded in real schema, plans and statistics · natural language to SQL ·
query optimisation · migration risk review · schema health score · schema drift ·
documentation generator · Git integration.

*Exit criterion: Basora suggests a fix the user would not have found alone.*

Full detail: [`docs/01-product-overview.md`](docs/01-product-overview.md) §7.

---

## Technology

| | |
|---|---|
| Runtime | .NET 10 |
| Language | C# 13 (`nullable enable`, warnings as errors) |
| UI | Avalonia 12 — one codebase for Windows, macOS and Linux |
| MVVM | CommunityToolkit.Mvvm (source generators) |
| Database | Npgsql — raw, no ORM |
| Editor | AvaloniaEdit |
| Charts | LiveCharts2 |
| Logging | Serilog |
| Tests | xUnit · Testcontainers · Avalonia.Headless · Verify |

The repository started as an F# Avalonia scaffold; the decision to move to C# is recorded
in [ADR-0001](docs/adr/0001-language-and-ui-stack.md).

---

## Architecture at a glance

```text
Basora.App              entry point, composition root
Basora.UI               views, view models, controls, themes
Basora.Core             contracts + domain types — references NOTHING
Basora.PostgreSQL       the only project that touches Npgsql
Basora.Analytics        pure analysis: query, index, schema, migration, health
Basora.AI               provider-agnostic assistant + context builder
Basora.Security         secret stores, SSH tunnels, redaction, audit
Basora.Infrastructure   settings, workspace persistence, logging
```

Three rules, enforced by architecture tests rather than code review:

- `Basora.Core` references no other project and no UI or data framework.
- `Basora.UI` never references `Basora.PostgreSQL` — view models talk to interfaces.
- `Basora.Analytics` performs no I/O; it receives statistics and returns findings.

That shape is what makes the whole thing testable without a window or a database — and
what lets most of the work happen in parallel.

Full detail: [`docs/02-architecture.md`](docs/02-architecture.md).

---

## Getting started

### Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) (developed against 10.0.400)
- A PostgreSQL 13+ server for integration work
- Docker, for Testcontainers-based integration tests

### Build and run

```bash
dotnet build
dotnet run --project src/Basora.App
dotnet test
```

> This currently launches an empty Avalonia window. The real application shell arrives
> with `T-U03`.

---

## Documentation

Everything lives in [`docs/`](docs/README.md). Start with the index — it
explains the reading order for planning, coding, building a screen, and reviewing.

| Document | What it covers |
|---|---|
| [`idea.md`](docs/idea.md) | The original product brief — the source of truth for scope |
| [`01-product-overview.md`](docs/01-product-overview.md) | Vision, users, differentiators, non-goals |
| [`02-architecture.md`](docs/02-architecture.md) | Solution layout, layer rules, MVVM, DI, threading |
| [`03-tech-stack.md`](docs/03-tech-stack.md) | Packages, rationale, rejected alternatives |
| [`04-domain-model.md`](docs/04-domain-model.md) | Every type in `Basora.Core` |
| [`05-postgresql-data-layer.md`](docs/05-postgresql-data-layer.md) | Npgsql, catalog queries, streaming, `COPY`, error mapping |
| [`06-security-and-credentials.md`](docs/06-security-and-credentials.md) | Secret storage, SSH, TLS, redaction, audit, AI boundary |
| [`07-design-system.md`](docs/07-design-system.md) | Tokens, palettes, density, accessibility, required states |
| [`08-ux-flows.md`](docs/08-ux-flows.md) | Nine end-to-end user journeys |
| [`09-keyboard-map.md`](docs/09-keyboard-map.md) | Full shortcut map |
| [`10-safety-rules.md`](docs/10-safety-rules.md) | The production guard rule engine |
| [`11-testing-strategy.md`](docs/11-testing-strategy.md) | Test pyramid, version matrix, performance budgets |
| [`12-coding-standards.md`](docs/12-coding-standards.md) | Conventions and Definition of Done |
| [`13-glossary.md`](docs/13-glossary.md) | Shared vocabulary |
| [`wireframes/`](docs/wireframes/README.md) | 24 self-contained screen specs |
| [`tasks/`](docs/tasks/00-task-board.md) | The execution plan |

---

## The build plan

73 tasks for MVP 1, organised into waves. The plan is deliberately structured so most
work happens **concurrently**:

```text
WAVE 0  solution restructure, build infrastructure       2 tasks (serial)
WAVE 1  Core contracts and design tokens                 7 tasks (parallel)
WAVE 2  TestKit, secrets, session, lexer, grid control  13 tasks (parallel)
WAVE 3  services and the app shell                      20 tasks (parallel)
WAVE 4  feature screens                                 14 tasks (parallel)
WAVE 5  composition and editing                         13 tasks (parallel)
WAVE 6  hardening: performance, a11y, packaging          5 tasks
```

Three rules make that possible:

1. **Contracts before implementations.** Wave 1 lands every interface; wave 2 lands an
   in-memory fake for each. After that, no task waits on another task's implementation.
2. **File ownership is exclusive.** Every task card lists the paths it owns. Disjoint
   ownership means safe concurrency and near-zero merge conflicts.
3. **Shared files are pre-carved.** The four genuinely shared files are structured so
   tasks append a file rather than edit a shared line.

The critical path is the data-grid chain (`T-D01 → D02 → D03 → D05 → D06`). See
[`docs/tasks/00-task-board.md`](docs/tasks/00-task-board.md) for the dependency
graph, and [`docs/tasks/README.md`](docs/tasks/README.md) for the card format.

### Demoable milestones

| Milestone | You can |
|---|---|
| Walking skeleton | Build, test, and see a themed empty shell |
| First connection | Connect to a real database |
| First read | Browse and read data |
| First query | Write and run SQL |
| First safe write | Edit data with SQL review and safety checks |
| MVP 1 | Replace an existing client |

---

## Contributing

Read [`docs/12-coding-standards.md`](docs/12-coding-standards.md) before opening a
PR, then pick a task whose dependencies have merged.

- Branch: `feat/T-D05-pending-changes`
- Commit: `feat(grid): generate UPDATE statements for pending edits [T-D05]`
- Only modify paths listed under **Owns** on your task card. Needing to touch someone
  else's files is a signal the task boundary is wrong — say so in the PR rather than
  editing across the line.

A change is done when it builds clean on three platforms, is tested, implements all seven
required UI states, is keyboard-accessible, works in both themes, is cancellable, maps
errors to actionable messages, and checks off its card's acceptance criteria.

---

## License

Not yet decided.
