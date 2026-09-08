# Basora — Documentation Index

**Basora — PostgreSQL, understood.**

A native, cross-platform PostgreSQL development, administration and observability
workspace built on .NET 10 + Avalonia + Npgsql.

The source of truth for *what we are building* is [`idea.md`](idea.md) (the raw product
brief). Everything else in this folder turns that brief into something a team — or a
fleet of parallel agents — can execute without talking to each other.

---

## How this documentation is organised

```text
docs/
│
├── idea.md ..................... Raw product brief (source of truth for scope)
├── README.md ................... You are here
│
├── 01-product-overview.md ...... Vision, positioning, users, differentiators, non-goals
├── 02-architecture.md .......... Solution layout, layer rules, MVVM, DI, threading
├── 03-tech-stack.md ............ Packages, versions, rationale, rejected alternatives
├── 04-domain-model.md .......... Core types every layer agrees on
├── 05-postgresql-data-layer.md . Npgsql, catalog queries, streaming, transactions, COPY
├── 06-security-and-credentials.md  Secret storage, SSH tunnels, SSL, redaction, audit
├── 07-design-system.md ......... Tokens, palettes, typography, density, a11y
├── 08-ux-flows.md .............. End-to-end user journeys across screens
├── 09-keyboard-map.md .......... Full shortcut map + platform modifier rules
├── 10-safety-rules.md .......... Production guard rule engine specification
├── 11-testing-strategy.md ...... Unit / integration / headless UI / perf budgets
├── 12-coding-standards.md ...... C#, XAML, ViewModel and commit conventions
├── 13-glossary.md .............. Shared vocabulary
│
├── adr/ ........................ Architecture Decision Records (one file per decision)
│
├── wireframes/ ................. One self-contained spec per screen (24 screens)
│   └── README.md ............... Wireframe index, conventions, shared shell contract
│
└── tasks/ ...................... Execution plan
    ├── README.md ............... Task format, ID scheme, parallelism rules
    ├── 00-task-board.md ........ Master board: waves, dependency graph, critical path
    └── epic-*.md ............... Task cards grouped by epic
```

---

## Reading order

**If you are planning:** `01` → `02` → `tasks/00-task-board.md`

**If you are about to write code:** `02` → `03` → `04` → `12` → your epic file in `tasks/`

**If you are about to build a screen:** `07` → `09` → your `wireframes/wf-NN-*.md` → the
matching task card. A wireframe doc is deliberately self-contained: it should be
buildable without reading any other wireframe.

**If you are reviewing:** `10` (safety) → `11` (testing) → `12` (standards)

---

## The parallelism contract

Basora is planned so that most work can happen **simultaneously**. That only works if
everyone honours three rules:

1. **Contracts before implementations.** Wave 0 lands every interface in
   `Basora.Core/Interfaces/` plus an in-memory fake for each. After Wave 0, no task
   waits on another task's implementation — it codes against the interface and tests
   against the fake.

2. **File ownership is exclusive.** Every task card lists the files/folders it owns.
   Two tasks with disjoint ownership can run at the same time, full stop. If you need
   to touch a file you do not own, that is a signal the task boundary is wrong — raise
   it rather than editing across the line.

3. **Shared files are append-only and pre-carved.** `App.axaml`, DI registration and
   the resource dictionaries are edited by many tasks. They are structured as a set of
   includes/partials so each task appends its own file instead of editing shared lines.
   See `tasks/README.md` § *Shared-file protocol*.

---

## Status

| Area | State |
|---|---|
| Product brief | Complete (`idea.md`) |
| Planning docs | Complete (this folder) |
| Solution restructure | Not started — see `T-F01` |
| MVP 1 | Not started |
| MVP 2 / MVP 3 | Backlog only (`tasks/epic-09-*`) |

> **Current repo reality:** the checked-in scaffold is a single **F#** Avalonia project
> (`Basora/Basora.fsproj`). The decision recorded in [`adr/0001-language-and-ui-stack.md`](adr/0001-language-and-ui-stack.md)
> is to move to a multi-project **C#** solution. `T-F01` performs that restructure and
> is the only task that may touch the existing scaffold.
