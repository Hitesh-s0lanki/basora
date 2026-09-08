# 01 — Product Overview

## 1. One-sentence definition

> Basora is a modern PostgreSQL desktop workspace that combines the everyday database
> workflow of TablePlus with the administration reach of pgAdmin, plus an intelligence
> layer for query performance, schema health, migration safety, production guardrails
> and AI-assisted database work.

**Tagline:** *Basora — PostgreSQL, understood.*

---

## 2. The thesis

Every existing tool answers "show me my database." Basora answers a harder question:

> **What is happening in my PostgreSQL database, why is it happening, and what should I do about it?**

This single sentence is the tie-breaker for every scope argument. If a feature helps
answer it, it is core. If it only makes the grid prettier, it is table stakes and we
copy the proven pattern rather than inventing one.

---

## 3. Two product layers

### Layer 1 — Database Client (table stakes)

We must be *at least as good as* the incumbents here, and we win no points for it.
Copy proven interaction patterns; do not innovate for its own sake.

```text
Connections · SQL Editor · Table Browser · Data Editor · Schema Management
Import/Export · SSH · Transactions · Query History · Favorites · ER Diagrams
```

### Layer 2 — Database Intelligence (the differentiator)

This is why someone switches to Basora.

```text
Query Intelligence      Why is this query slow?
Migration Intelligence  Is this migration safe to run right now?
Schema Intelligence     What is wrong with my schema?
Index Intelligence      Which indexes should I create or drop?
Production Intelligence What is hurting my database at this moment?
Schema Drift            What changed between environments?
AI Assistant            Explain and diagnose my actual database.
```

Layer 2 is only credible if Layer 1 is boringly reliable. Hence the phasing in section 7.

---

## 4. Scope boundary — PostgreSQL only

Phase 1 supports **PostgreSQL and nothing else**. This is a deliberate constraint, not
a limitation to apologise for.

Going deep buys us things a multi-engine tool structurally cannot have: real
`EXPLAIN (ANALYZE, BUFFERS)` plan analysis, `pg_stat_statements` integration, lock-tree
analysis via `pg_locks`, bloat estimation, autovacuum diagnostics, per-version feature
detection, and correct handling of PostgreSQL's actual type system (arrays, ranges,
composites, `jsonb`, enums, domains).

**Explicitly out of scope for Phase 1:** MySQL, SQL Server, SQLite, MongoDB, Oracle,
Redis, and any abstraction layer built to accommodate them later. We will pay the
refactor cost if and when a second engine is ever justified.

---

## 5. Target users

| User | Primary need | Features that earn their loyalty |
|---|---|---|
| **Backend developer** | Inspect, query, debug, modify | SQL editor, data grid, autocomplete, query performance analyzer, visual EXPLAIN |
| **Full-stack developer** | Fast access, simple edits | Connection manager, table browser, inline editing, ER diagram |
| **DevOps engineer** | Monitor and protect production | Health dashboard, activity, locks, storage, backup/restore, production guard |
| **DBA** | Administer PostgreSQL properly | Roles, permissions, extensions, vacuum/analyze, index intelligence, configuration |
| **Technical founder / small team** | One tool for dev to staging to prod | Everything above with environment tagging and safety rails |

The common thread: **one application across all environments**, with the tool itself
enforcing that production is treated differently from local.

---

## 6. Differentiators, ranked

1. **Query Intelligence** — automatic plan capture on every run, not a separate mode.
   Run a query, immediately see rows-scanned vs rows-returned and the offending node.
2. **Migration Safety Analyzer** — before any DDL: table size, row estimate, index
   count, lock level acquired, blocking risk, and a safer rewrite when one exists.
3. **Production Database Guard** — environment-aware confirmation ladder, dangerous
   statement detection (missing `WHERE`, unqualified `DROP`), affected-row estimation.
4. **Index Intelligence** — unused, duplicate, redundant and missing indexes with sizes
   and scan counts. Recommends; never acts.
5. **Schema Drift** — dev vs staging vs prod vs Git, with generated reconciliation SQL.
6. **AI Assistant with real context** — schema, EXPLAIN, statistics, locks and history,
   not a chat box bolted onto a grid. Never executes generated SQL silently.
7. **Schema Health Score** — a single number that decomposes into actionable findings.

---

## 7. Phasing

### MVP 1 — Credible client

Connection manager, secure credential storage, SSH/SSL, object explorer, table browser,
data grid with inline editing, advanced filters, pending changes with SQL preview, SQL
editor with autocomplete and formatting, query execution with streaming and
cancellation, history, favorites, CSV import, CSV/JSON/SQL export, structure, index,
constraint and FK viewers, safe mode, multiple tabs, dark/light theme, command palette,
Open Anything.

**Exit criterion:** a developer can delete their existing client for daily work.

### MVP 2 — Intelligence

ER diagram, schema diff, migration generator, EXPLAIN capture, visual query plan, query
statistics, health dashboard, active queries, lock viewer, storage analyzer, index
analyzer, table health.

**Exit criterion:** a developer diagnoses a real production slowdown entirely inside
Basora.

### MVP 3 — Assistance

AI assistant, natural language to SQL, AI query optimization, migration risk analysis,
schema health score, schema drift, documentation generator, Git integration.

**Exit criterion:** Basora suggests a fix the user would not have found alone.

### Future — Enterprise

Team workspaces, shared connections, audit logs, RBAC, centralised policy, SSO, fleet
management, organisation dashboards, compliance controls, plugin architecture, MCP tool
surface.

---

## 8. Non-goals

- **Not a pgAdmin clone.** We do not aim to expose every PostgreSQL knob in a tree.
- **Not a web app.** Native desktop, offline-capable, no server component in Phase 1.
- **Not multi-engine.** See section 4.
- **Not an ORM, migration framework, or CI tool.** We generate SQL and hand it over; we
  do not own the user's migration pipeline.
- **Not autonomous.** No feature ever mutates a database without explicit human
  confirmation. AI proposes, humans dispose. This is a hard product rule, not a default.
- **No telemetry in Phase 1.** Nothing about the user's schema, queries or data leaves
  the machine, except AI requests the user explicitly initiates.

---

## 9. Product principles

1. **Never surprise a user in production.** Escalate friction with environment risk.
2. **Every mutation is previewable as SQL** before it runs, without exception.
3. **Recommend, never act** — especially for destructive suggestions such as dropping
   an unused index.
4. **The UI never blocks.** Every database call is cancellable and off the UI thread.
5. **Keyboard first.** Any operation reachable by mouse has a shortcut or a palette
   command.
6. **Explain the finding, not just the number.** A bare "cache hit 91%" is noise;
   "cache hit dropped because `orders` no longer fits in `shared_buffers`" is a product.
7. **Degrade honestly.** If `pg_stat_statements` is absent or the role lacks privilege,
   say exactly that and what to do — never show an empty panel.

---

## 10. Success criteria

| Dimension | Target |
|---|---|
| Cold start to first query result | under 3 s on a local database |
| Grid scroll on a 1M-row table | 60 fps, constant memory |
| First row visible for a streaming query | under 500 ms after server responds |
| Cancel a runaway query | effective in under 1 s, UI never frozen |
| Destructive op in production without confirmation | **zero, ever** |
| Crash-free sessions | above 99.5% |
