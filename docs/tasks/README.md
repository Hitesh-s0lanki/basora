# Tasks — Format, IDs and Parallelism Rules

---

## 1. Why this exists

The point of this plan is that **most of Basora can be built at the same time**. That is
only true if three things hold:

1. Contracts exist before implementations.
2. Every task owns a disjoint set of files.
3. Shared files are structured so tasks append rather than edit.

This document defines those rules. `00-task-board.md` is the board itself.

---

## 2. Task ID scheme

```text
T-<epic letter><number>

  F   Foundation        solution, build, Core models and contracts, TestKit, CI
  U   UI shell / UX     shell, themes, tabs, palette, settings, keymap
  S   Security          secret store, tunnels, redaction, audit
  C   Connections       session, registry, editor, importers
  E   Explorer          catalog queries, metadata service, tree, search
  M   Metadata objects  descriptors, structure editor, indexes, DDL
  D   Data grid         grid control, paging, editing, filters, change sets
  Q   Query             lexer, completion, formatter, executor, editor, results
  H   History           history store, favorites
  X   Import / Export
  P   Protection        safety engine, classifier, dialogs, migration impact
  V   Visualisation     ER diagram, schema diff, EXPLAIN            (MVP 2)
  N   Monitoring        health, activity, locks, storage, slow queries (MVP 2)
  AI  AI assistant                                                   (MVP 3)
  G   Governance        drift, snapshots, docs, Git                  (MVP 3)
```

The ID appears in the branch name (`feat/T-D05-pending-changes`) and in the commit
subject (`feat(grid): ... [T-D05]`), which links code to plan with no extra tooling.

---

## 3. Card format

Every task card carries exactly these fields:

```markdown
### T-XNN — Title

**Wave:** N &nbsp;|&nbsp; **Size:** S / M / L &nbsp;|&nbsp; **Spec:** link

**Depends on:** T-A01, T-B02 (or "nothing")
**Blocks:** T-C03, T-D04

**Owns:**
- `src/Path/To/Folder/**`
- `tests/Path/To/Tests/**`

**Deliverables**
- Concrete artefacts, not activities.

**Acceptance**
- [ ] Testable statements. No "works well".
```

**Size** is rough effort, not time: S = under a day, M = 1–3 days, L = 3–8 days. A task
larger than L must be split — an L task that overruns is the usual reason a wave stalls.

---

## 4. The parallelism rules

### Rule 1 — Contracts before implementations

Wave 1 lands every model and every `I*` interface in `Basora.Core`. Wave 2 lands
`Basora.TestKit`, an in-memory fake for each interface. After that, **no task waits on
another task's implementation**: it codes against the interface and tests against the
fake.

This is the single most important structural decision in the plan. It converts a chain
of dependencies into a fan-out.

### Rule 2 — File ownership is exclusive

Every card lists the paths it owns. **Two tasks with disjoint ownership can run
concurrently, with no coordination.** If you need to modify a file another task owns:

- If it is a contract in `Core` — that is a design change. Raise it; do not edit
  unilaterally, because someone else is coding against it right now.
- If it is an implementation — the task boundary is wrong. Say so in the PR.

Ownership is also the merge-conflict policy. Disjoint ownership means near-zero
conflicts even with a dozen concurrent branches.

### Rule 3 — Shared-file protocol

Four files are genuinely shared. Each is structured so tasks **add a file** instead of
editing a shared one:

| Shared file | Protocol |
|---|---|
| `App.axaml` | Contains only `<ResourceInclude>` entries. A new control adds `Styles/MyControl.axaml` and one include line at the end of the list |
| DI registration | Each module owns `Composition/Add<Module>.cs`. `Program.cs` calls them in a fixed order and is edited only by `T-F09` |
| `Directory.Packages.props` | Append-only, alphabetically sorted. Adding a package is one line at its sorted position |
| `DefaultKeymap.cs` | One region per context, appended within your region |

Everything else is owned by exactly one task.

### Rule 4 — A wave is a barrier, not a schedule

Everything in wave N can start once wave N-1 is merged. Tasks within a wave have no
ordering between them. A wave is not a sprint and carries no date.

---

## 5. Definition of Ready

A task may start when:

- [ ] Its dependencies are merged to `main`.
- [ ] Its spec (wireframe or doc section) exists and is unambiguous.
- [ ] Its `Core` contracts exist, or it is the task that creates them.
- [ ] Its owned paths do not overlap another in-flight task.

---

## 6. Definition of Done

The full checklist is `../12-coding-standards.md` section 8. In short: builds clean on
three platforms, tested, all seven UI states, keyboard-accessible, both themes,
cancellable, errors mapped, public `Core` API documented, acceptance criteria checked,
no files touched outside ownership.

---

## 7. Status tracking

Cards are not a status board — they do not carry a status field, because a stale status
in a markdown file is worse than none. Status lives in the branch and PR state:

```text
No branch          -> not started
Branch, no PR      -> in progress
PR open            -> in review
Merged             -> done
```

`git branch -r | grep T-` is the live board.

---

## 8. Working these tasks with parallel agents

If tasks are dispatched to concurrent agents, give each agent exactly:

1. Its task card.
2. The linked spec (wireframe or doc).
3. `02-architecture.md`, `04-domain-model.md`, `12-coding-standards.md`.
4. The instruction: **you may create and modify only the paths under "Owns".**

That is sufficient context for an agent to produce a mergeable branch without talking to
any other agent — which is the whole design goal of this plan.
