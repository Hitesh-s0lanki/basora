# Epic 09 — MVP 2 and MVP 3 Backlog

**Epic-level only, deliberately.** These are not broken into actionable cards yet,
because MVP 1 will teach us things that would make those cards wrong. What *is* fixed:
the wireframes exist, the domain models are already in `Basora.Core` from wave 1, and
the analyzer projects are pure — so none of this requires re-architecting.

Break an epic into cards when its wave is next, not before.

---

## MVP 2 — Intelligence

**Theme:** a developer diagnoses a real production slowdown entirely inside Basora.

### T-V — Visualisation

| ID | Title | Size | Depends on | Spec |
|---|---|---|---|---|
| T-V01 | ER diagram canvas and rendering | L | T-M01, T-V02 | [wf-17](../wireframes/wf-17-er-diagram.md) |
| T-V02 | Graph layout engines (hierarchical, force, grid) | L | T-F05 | [wf-17](../wireframes/wf-17-er-diagram.md) section 7 |
| T-V03 | Schema comparer | L | T-M01 | [wf-18](../wireframes/wf-18-schema-diff.md) |
| T-V04 | Migration generator | L | T-V03, T-P05 | [wf-18](../wireframes/wf-18-schema-diff.md) |
| T-V05 | EXPLAIN capture, parsing and plan analyzer | L | T-Q04, T-F07 | [wf-19](../wireframes/wf-19-visual-explain.md) |
| T-V06 | Visual plan rendering (tree, flame, comparison) | L | T-V05 | [wf-19](../wireframes/wf-19-visual-explain.md) |

**Highest-risk item:** `T-V05`. Plan JSON varies across server versions and contains
parallel workers, CTEs, subplans, JIT and trigger timings. Budget for a corpus of real
plans as the primary test asset, gathered before the task starts.

### T-N — Monitoring

| ID | Title | Size | Depends on | Spec |
|---|---|---|---|---|
| T-N01 | Health dashboard screen | L | T-N02, T-U03 | [wf-20](../wireframes/wf-20-health-dashboard.md) |
| T-N02 | Health monitor collection and analyzer rules | L | T-C01, T-F07 | [wf-20](../wireframes/wf-20-health-dashboard.md) section 7 |
| T-N03 | Activity and lock analyzer screen | L | T-N04 | [wf-21](../wireframes/wf-21-activity-locks.md) |
| T-N04 | Activity monitor and blocking-chain builder | M | T-C01 | [wf-21](../wireframes/wf-21-activity-locks.md) section 7 |
| T-N05 | Storage, table health, index and slow-query screens | L | T-N06, T-M06 | [wf-22](../wireframes/wf-22-storage-index-intel.md) |
| T-N06 | Storage analyzer, table health analyzer, slow-query service | L | T-E01 | [wf-22](../wireframes/wf-22-storage-index-intel.md) section 7 |

**Dependency to protect:** the monitor channel from `T-C01` must stay genuinely separate.
If monitoring ever contends with user queries, the whole MVP-2 story becomes a liability
on a busy production server rather than a feature.

### MVP 2 exit criteria

- [ ] UX Flow 6 from `08-ux-flows.md` works end to end: dashboard finding to blocking
      tree to slow query to plan to index suggestion to safety check to apply to
      re-measure — every step one click from the last.
- [ ] Polling never degrades a production server; every monitoring query is bounded and
      cancellable.
- [ ] Every analyzer is pure and unit-tested against fixture data.
- [ ] Missing extensions and privileges degrade to specific, actionable messages, never
      to zeros or blank panels.

---

## MVP 3 — Assistance

**Theme:** Basora suggests a fix the user would not have found alone.

### T-AI — AI assistant

| ID | Title | Size | Depends on | Spec |
|---|---|---|---|---|
| T-AI01 | Provider abstraction and streaming client | M | T-S01 | [03-tech-stack](../03-tech-stack.md) section 6 |
| T-AI02 | Database context builder and read-only tool registry | L | T-E02, T-V05 | [wf-23](../wireframes/wf-23-ai-assistant.md) section 7, [06-security](../06-security-and-credentials.md) section 6 |
| T-AI03 | Assistant panel | L | T-AI01, T-AI02 | [wf-23](../wireframes/wf-23-ai-assistant.md) |
| T-AI04 | Natural language to SQL | M | T-AI02 | [idea.md](../idea.md) section 56 |
| T-AI05 | AI query optimisation and migration review | M | T-AI02, T-V05, T-P05 | [idea.md](../idea.md) sections 59–60 |

**The security boundary is `T-AI02`, and it is the reason this epic cannot be rushed.**
Its tests — no secret material in a context, no mutating tool in the registry, no cell
values without per-request opt-in — are the gate for the whole epic. Model identifiers,
pricing assumptions and SDK versions get pinned when `T-AI01` starts, not now.

### T-G — Governance

| ID | Title | Size | Depends on | Spec |
|---|---|---|---|---|
| T-G01 | Database snapshots | M | T-M01 | [idea.md](../idea.md) section 65 |
| T-G02 | Schema drift detection and alerts | L | T-V03, T-G01 | [idea.md](../idea.md) section 64 |
| T-G03 | Documentation generator (Markdown, HTML, PDF) | M | T-M01, T-AI02 | [idea.md](../idea.md) section 61 |
| T-G04 | Git integration and repository drift | L | T-G02 | [idea.md](../idea.md) section 63 |
| T-G05 | Database change tracking | M | T-S05 | [idea.md](../idea.md) section 62 |

`T-G01` (snapshots) is the enabler for `T-G02` and also unlocks the historical-growth
charts that `T-N06` has to omit in MVP 2 — worth doing early in this phase for that
reason alone.

### MVP 3 exit criteria

- [ ] The assistant answers a real question about a real database, grounded, with sources.
- [ ] No generated SQL can execute without the user running it through the normal editor
      and safety ladder.
- [ ] A context payload containing any secret or any cell value without opt-in is
      impossible — asserted by test, not by policy.
- [ ] Schema drift between three environments is detected and produces reconciliation SQL.

---

## Future — Enterprise (not scheduled)

Team workspaces, shared connections, centralised audit, RBAC, policy, SSO, fleet
management, organisation dashboards, compliance controls, plugin architecture, and the
MCP tool surface.

Two structural notes worth keeping in mind while building MVP 1–3, both of which cost
nothing now and a great deal later:

1. **The plugin seam already exists.** Analyzers, export formats, result renderers, AI
   providers and layout engines are all `IEnumerable<T>` DI collections. A plugin is
   mechanically just an assembly contributing to those collections.
2. **The MCP tool surface is `T-AI02`'s tool registry.** The read-only tool list in
   `wf-23` is deliberately the same shape as the MCP tool list in `idea.md` section 76.
   Building one builds most of the other — but only if the "no mutating tool exists"
   rule is never relaxed for convenience.
