# Epic 08 — MVP-1 Hardening

Wave 6. These run **after** every feature task and are what turns "the features exist"
into "we can ship this". They are sized as real work, not as a checklist someone does on
a Friday afternoon.

---

### T-Z01 — Performance budgets and profiling

**Wave:** 6 | **Size:** M | **Spec:** [11-testing-strategy](../11-testing-strategy.md) section 6

**Depends on:** all feature tasks
**Blocks:** T-Z05

**Owns:** `tests/Basora.Performance.Tests/**`, `build/perf/**`

**Deliverables**
- An automated benchmark per budget in the section-6 table, run on a fixed CI machine
  class with results tracked over time.
- Allocation profiling of the grid scroll path and the streaming read path.
- A startup-time trace with the cost of each initialisation step attributed.

**Acceptance**
- [ ] Every budget in the table is measured and met:
      cold start under 2.0 s; tree first paint under 1.5 s; first result row under
      500 ms; 1M-row scroll at 60 fps; 50k x 20 result under 250 MB; completion popup
      under 100 ms; format of 2,000 lines under 200 ms; cancel acknowledged under 100 ms.
- [ ] Per-frame allocation during grid scroll is effectively zero.
- [ ] A regression beyond 10% on any budget fails the build.
- [ ] The startup trace is checked in so future regressions can be attributed.

---

### T-Z02 — Accessibility and keyboard audit

**Wave:** 6 | **Size:** M | **Spec:** [07-design-system](../07-design-system.md) section 11, [09-keyboard-map](../09-keyboard-map.md)

**Depends on:** all UI tasks
**Blocks:** T-Z05

**Owns:** `tests/Basora.UI.Tests/Accessibility/**`, `docs/accessibility-report.md`

**Deliverables**
- Automated checks: every interactive element has an automation name; focus order is
  logical per screen; every semantic token pair meets AA contrast in all three themes.
- A manual screen-reader pass over the connection editor, the data grid and the safety
  dialogs, with findings recorded.
- A full keyboard-only run through UX Flows 1, 2, 3 and 5 from `08-ux-flows.md`.
- Verification at 100%, 150% and 200% OS scaling, and at base font 16.

**Acceptance**
- [ ] Every flow above is completable with **no mouse at all**.
- [ ] Focus is visible on every focusable element in every theme.
- [ ] No meaning is conveyed by colour alone anywhere — verified against the
      modified-cell, risk-level and environment indicators specifically.
- [ ] Reduced-motion OS setting disables all transitions.
- [ ] High-contrast theme is usable end to end.
- [ ] The accessibility report is written and its open items are triaged, not silently
      closed.

---

### T-Z03 — Cross-platform packaging and signing

**Wave:** 6 | **Size:** L | **Spec:** [03-tech-stack](../03-tech-stack.md) section 1

**Depends on:** T-F09
**Blocks:** T-Z05

**Owns:** `build/packaging/**`, `.github/workflows/release.yml`

**Deliverables**
- Windows: signed MSIX or Velopack installer with an update channel.
- macOS: signed, notarised and stapled `.app` in a `.dmg`, universal (x64 + arm64).
- Linux: AppImage plus `.deb`, with a desktop entry and icon.
- Version stamping from the git tag; a release workflow producing all three artefacts.
- Auto-update check (opt-in, off by default, with no telemetry attached).

**Acceptance**
- [ ] Each artefact installs and launches on a clean machine of its OS.
- [ ] macOS notarisation passes and Gatekeeper does not warn.
- [ ] Windows SmartScreen does not warn on a signed build.
- [ ] The Linux AppImage runs on a distribution without a Secret Service daemon, and the
      app degrades correctly to the encrypted vault.
- [ ] Version and build metadata are visible in Help > About.
- [ ] Uninstall leaves no secrets in the OS keystore without asking first.

---

### T-Z04 — Error-path and resilience sweep

**Wave:** 6 | **Size:** M | **Spec:** [05-postgresql](../05-postgresql-data-layer.md) section 10, [07-design-system](../07-design-system.md) section 9

**Depends on:** all feature tasks
**Blocks:** T-Z05

**Owns:** `tests/Basora.PostgreSQL.Tests/Resilience/**`, `tests/Basora.UI.Tests/States/**`

**Deliverables**
- A screen-by-screen audit that all seven required states are implemented, with a
  checklist per wireframe.
- Chaos tests: server killed mid-query, network dropped mid-stream, pool exhausted,
  disk full during export, permission revoked mid-session, tunnel dropped, keystore
  removed mid-session.
- Verification that every SQLSTATE in the catalogue produces its mapped, actionable
  message in the real UI, not just in a unit test.

**Acceptance**
- [ ] Every screen implements all seven states; the checklist is complete and checked in.
- [ ] No chaos scenario crashes the app or loses the user's editor content.
- [ ] Every mapped SQLSTATE has been observed in the real UI with the right message.
- [ ] No raw stack trace is ever shown to a user.
- [ ] Every long operation is cancellable and every cancel is honoured promptly.

---

### T-Z05 — Release candidate and manual test pass

**Wave:** 6 | **Size:** M | **Spec:** [11-testing-strategy](../11-testing-strategy.md) section 10

**Depends on:** T-Z01, T-Z02, T-Z03, T-Z04
**Blocks:** nothing — this is the ship gate

**Owns:** `docs/release-checklist.md`, `CHANGELOG.md`, `README.md`

**Deliverables**
- The full manual checklist from `11-testing-strategy.md` section 10, executed on all
  three platforms and recorded.
- A real-world soak: connect to an actual production-scale database (5,000+ tables) and
  work in it for a full day.
- User-facing README, changelog and a first-run guide.
- A triaged known-issues list.

**Acceptance**
- [ ] The manual checklist passes on Windows, macOS and Linux.
- [ ] SSH connections work against a real bastion with all three auth methods.
- [ ] A 500 MB CSV import cancelled halfway leaves the database untouched.
- [ ] A 5,000-table database stays responsive throughout a full working day.
- [ ] **Zero destructive operations reached a Production-tagged database without
      confirmation** across the entire soak — the one criterion that is not negotiable.
- [ ] Documentation is written and accurate.
- [ ] Every known issue is either fixed or explicitly accepted, with a rationale.
