# Epic 06 — Protection (Safety Engine)

The differentiator that ships in MVP 1. `T-P01 -> T-P02 -> T-P03` is the second-longest
chain in the plan and is a hard prerequisite for shipping anything that writes — it
cannot be deferred to the end.

Spec: [10-safety-rules](../10-safety-rules.md), [wf-16](../wireframes/wf-16-safety-dialogs.md).

---

### T-P01 — Statement classifier

**Wave:** 2 | **Size:** M | **Spec:** [10-safety-rules](../10-safety-rules.md) section 2

**Depends on:** T-Q01, T-F07
**Blocks:** T-P02

**Owns:** `src/Basora.Core/Services/Safety/Classifier/**`,
`tests/Basora.Core.Tests/Safety/Classifier/**`

**Deliverables**
- Classification of every statement into a `StatementKind` using the shallow parser from
  `T-Q01` — never regex over raw text.
- Extraction of subject objects (target table, index, role, database) with schema
  qualification where present.
- Detection of structural facts the rules need: presence of a `WHERE` clause, `CASCADE`,
  `CONCURRENTLY`, `NOT VALID`, `IF EXISTS`, always-true predicates, CTE-wrapped
  mutations, and `UPDATE ... FROM`.

**Acceptance**
- [ ] The adversarial corpus from `10-safety-rules.md` section 11 classifies correctly:
      keywords in strings, dollar-quoted bodies containing `DROP TABLE`, comments
      containing statements, `DELETE` inside a CTE, `WITH ... DELETE`, multi-statement
      batches mixing kinds.
- [ ] Schema-qualified and unqualified names both resolve to the correct subject.
- [ ] An unparseable statement is classified `Unknown` and **escalates** risk rather than
      being treated as safe.
- [ ] No false positive on a plain `SELECT`.

---

### T-P02 — Safety rule engine

**Wave:** 3 | **Size:** L | **Spec:** [10-safety-rules](../10-safety-rules.md) sections 3–8

**Depends on:** T-P01, T-F07
**Blocks:** T-P03, T-P04, T-D06

**Owns:** `src/Basora.Core/Services/Safety/Engine/**`,
`src/Basora.PostgreSQL/Queries/SafetyProbe*`,
`tests/Basora.Core.Tests/Safety/Engine/**`

**Deliverables**
- All rules from the section-3 catalogue, each with a stable code, as data-driven rule
  objects.
- Risk scoring with the documented adjustments, clamped.
- The environment policy matrix as data, plus safe-mode escalation.
- `ISafetyProbe` implementation: bounded (500 ms) row estimation via `EXPLAIN` **without
  ANALYZE**, and table facts.
- Safer-alternative generation for every pairing in section 9.
- Type-to-confirm phrase construction naming the real object.

**Acceptance**
- [ ] The labelled corpus (several hundred statements) produces the expected kind,
      findings and risk for every entry.
- [ ] **A test asserts no code path can emit `EXPLAIN ANALYZE` for a mutating statement.**
      This is the single highest-consequence test in the repo.
- [ ] Property test: adding a `WHERE` clause never increases risk.
- [ ] Property test: moving from Local to Production never decreases the confirmation
      level.
- [ ] Probe failure or timeout escalates risk and reports "unknown" affected rows.
- [ ] Every safer alternative is valid, runnable SQL — golden-file tested.
- [ ] The environment matrix is unit-tested cell by cell.

---

### T-P03 — Safety and confirmation dialogs

**Wave:** 4 | **Size:** M | **Spec:** [wf-16](../wireframes/wf-16-safety-dialogs.md)

**Depends on:** T-P02, T-U01, T-U07, T-S05
**Blocks:** nothing (but gates every write feature's Definition of Done)

**Owns:** `src/Basora.UI/Views/Safety/**`, `src/Basora.UI/ViewModels/Safety/**`,
`tests/Basora.UI.Tests/Safety/**`

**Deliverables** — the three dialog levels exactly as specified in wf-16, with the
context block ordering, findings, safer-alternative buttons, non-selectable
type-to-confirm phrase, and audit integration.

**Acceptance** — the full checklist in [wf-16 section 8](../wireframes/wf-16-safety-dialogs.md).

Hard gates, each an explicit test:
- [ ] `Enter` triggers Cancel, never Continue.
- [ ] The destructive button is never focused on open.
- [ ] The backdrop does not dismiss.
- [ ] The type-to-confirm text is not selectable or copyable.
- [ ] There is no "don't show again" option anywhere in the product.

---

### T-P04 — Safe mode and read-only enforcement

**Wave:** 4 | **Size:** M | **Spec:** [10-safety-rules](../10-safety-rules.md) section 6

**Depends on:** T-P02, T-C01
**Blocks:** nothing

**Owns:** `src/Basora.PostgreSQL/Connection/SafeModeEnforcer*`,
`src/Basora.UI/ViewModels/Shell/ModeIndicator*`,
`tests/Basora.PostgreSQL.Tests/SafeMode/**`

**Deliverables**
- Server-side read-only enforcement via
  `SET SESSION CHARACTERISTICS AS TRANSACTION READ ONLY`, with an explicit per-session
  disarm.
- Auto-`LIMIT` injection on unbounded editor `SELECT`s, visible in the UI and never
  silent.
- Required explicit transaction for data modification under safe mode.
- Status-bar mode badge and the bypass counter.
- Disabled write actions in menus and context menus, with tooltips explaining why.

**Acceptance**
- [ ] With read-only armed, a write fails **server-side** even if the classifier is
      bypassed — the backstop property, asserted by an integration test.
- [ ] Auto-`LIMIT` is applied and displayed; the user can see and remove it.
- [ ] Disarming read-only requires an explicit action and is audited.
- [ ] The bypass counter increments on every safety override and links to the audit log.
- [ ] Production connections default to safe mode and read-only.

---

### T-P05 — Migration impact analyzer

**Wave:** 3 | **Size:** M | **Spec:** [10-safety-rules](../10-safety-rules.md) section 4, [wf-11](../wireframes/wf-11-structure-editor.md)

**Depends on:** T-P01, T-M01
**Blocks:** T-M04, T-V04 (MVP 2)

**Owns:** `src/Basora.Analytics/MigrationAnalyzer/**`,
`tests/Basora.Analytics.Tests/MigrationAnalyzer/**`

**Deliverables**
- `IMigrationSafetyAnalyzer` producing `MigrationImpact` per statement: lock level,
  rewrite verdict, blocks-reads/blocks-writes, table size, row estimate, duration
  estimate and risk.
- Version-aware behaviour (for example, `ADD COLUMN ... DEFAULT` rewrites before PG 11
  and does not from 11 onward).
- The `lock_timeout` + retry recommendation for production DDL.
- The brief-lock explanation surfaced as a finding, since it is the most valuable and
  least understood point.

**Acceptance**
- [ ] Lock level is correct for every operation in the section-4 table, verified by an
      integration test that actually inspects `pg_locks` on each supported major version.
- [ ] Rewrite detection is correct for the `ADD COLUMN` and `ALTER TYPE` cases, including
      the version boundary.
- [ ] Every impact carries the table size and row estimate when available, and says
      "unknown" when not.
- [ ] Duration estimates are labelled estimates and never presented as guarantees.
- [ ] The analyzer performs no I/O; facts are supplied to it.
