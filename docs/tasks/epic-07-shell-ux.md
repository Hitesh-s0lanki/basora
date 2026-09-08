# Epic 07 — Shell, Theming and UX Infrastructure

Everything every other screen depends on. `T-U01` and `T-U03` are early and widely
blocking — treat them as near-critical-path even though they are not on it.

Spec: [07-design-system](../07-design-system.md), [09-keyboard-map](../09-keyboard-map.md),
[wf-03](../wireframes/wf-03-app-shell.md), [wf-15](../wireframes/wf-15-command-palette.md),
[wf-24](../wireframes/wf-24-settings.md).

---

### T-U01 — Design tokens and theme dictionaries

**Wave:** 1 | **Size:** M | **Spec:** [07-design-system](../07-design-system.md) sections 2–7

**Depends on:** T-F02
**Blocks:** every UI task

**Owns:** `src/Basora.UI/Themes/**`

**Deliverables**
- `Tokens.axaml` — colour primitives, spacing, radius, elevation, type scale, density.
- `Light.axaml`, `Dark.axaml`, `HighContrast.axaml` — semantic tokens only.
- Environment, status and data-state token sets.
- Typography resources including the bundled JetBrains Mono for code and data.
- Theme switching without restart via `DynamicResource`.

**Acceptance**
- [ ] Every semantic token is defined in **all three** theme dictionaries.
- [ ] All text/background pairs meet WCAG AA — asserted by a contrast test over the token
      table, not by eye.
- [ ] Theme switches live with no restart and no flicker.
- [ ] Density switching changes every row height through the single token.
- [ ] No primitive colour is referenced outside the semantic layer.

---

### T-U02 — Control styles and theme completeness test

**Wave:** 2 | **Size:** M | **Spec:** [07-design-system](../07-design-system.md) section 8, [12-coding-standards](../12-coding-standards.md) section 3

**Depends on:** T-U01
**Blocks:** every UI screen task

**Owns:** `src/Basora.UI/Styles/**`, `tests/Basora.UI.Tests/Theming/**`

**Deliverables**
- One dictionary per control: buttons, inputs, badges, tabs, tree, list, toolbar,
  scrollbar, tooltip, popover, dialog, toast, splitter, progress.
- `App.axaml` restructured to contain only `ResourceInclude` entries (the shared-file
  protocol).
- The theme-completeness test: every token referenced anywhere in XAML exists in every
  theme dictionary.
- A hard-coded-value analyzer or test that fails on a literal colour, size or margin in
  a view.

**Acceptance**
- [ ] Adding a token to one theme and not the others fails the test.
- [ ] A literal `#RRGGBB` or numeric `Margin` in a view fails the build.
- [ ] Every control renders correctly in all three themes at all three densities.
- [ ] Focus rings are visible on every focusable control in every theme.

---

### T-U03 — Application shell

**Wave:** 3 | **Size:** L | **Spec:** [wf-03](../wireframes/wf-03-app-shell.md)

**Depends on:** T-U01, T-U02, T-F10, T-C03
**Blocks:** every screen task

**Owns:** `src/Basora.UI/Views/Shell/**`, `src/Basora.UI/ViewModels/Shell/**`,
`tests/Basora.UI.Tests/Shell/**`

**Deliverables** — regions A–G from wf-03: menu, connection tab strip with environment
colouring and the Production window border, sidebar with its icon rail, document tab
strip, document area with split, bottom panel, and the nine-segment status bar.

**Acceptance** — the full checklist in [wf-03 section 8](../wireframes/wf-03-app-shell.md).

---

### T-U04 — Document host, tabs and workspace restore

**Wave:** 3 | **Size:** M | **Spec:** [wf-03](../wireframes/wf-03-app-shell.md) sections 4 and 7, [02-architecture](../02-architecture.md) section 6

**Depends on:** T-F10, T-U01
**Blocks:** T-D03, T-Q06, T-M03

**Owns:** `src/Basora.UI/Services/Documents/**`,
`src/Basora.UI/ViewModels/Shell/DocumentHost*`,
`tests/Basora.UI.Tests/Documents/**`

**Deliverables**
- `IDocumentHost` and `IDocumentViewModel` with lifetime, disposal and cancellation tied
  to the tab.
- MRU cycling, pinning, reopen-closed (`Ctrl+Shift+T`), drag between split panes.
- Workspace serialisation of tabs, split layout, scroll positions, filters, expansion
  state and unsaved editor text.
- `CanCloseAsync` prompting for unsaved SQL, pending changes and open transactions.

**Acceptance**
- [ ] Closing a document cancels its work and disposes its ViewModel.
- [ ] Workspace restores every listed piece of state after a force-kill.
- [ ] Unsaved SQL survives; pending data changes do **not** and the user is told why.
- [ ] Production connections are restored as "click to reconnect", never auto-connected.
- [ ] `Ctrl+Shift+T` restores the last closed tab with its content.

---

### T-U05 — Command registry and keymap infrastructure

**Wave:** 2 | **Size:** M | **Spec:** [09-keyboard-map](../09-keyboard-map.md)

**Depends on:** T-F02
**Blocks:** T-U06, T-Q06, T-U08

**Owns:** `src/Basora.Core/Input/**`, `src/Basora.UI/Services/Input/**`,
`tests/Basora.Core.Tests/Input/**`

**Deliverables**
- `ICommandRegistry` — the single list of commands, driving menus, palette and keymap.
- `DefaultKeymap` as **data**, with the shared-file region protocol.
- Platform modifier resolution (`Mod` to `Ctrl`/`Cmd`).
- Context-scoped bindings (`Global`, `Editor`, `Grid`, `Tree`, `Dialog`) with
  innermost-wins resolution.
- Chord support with the pending-chord status-bar indicator.
- Conflict detection, a user keymap overlay, and the four presets.

**Acceptance**
- [ ] Every shortcut in `09-keyboard-map.md` is registered and resolves on all platforms.
- [ ] A conflicting binding within one context is detected and cannot be saved.
- [ ] Context resolution is correct: the same gesture does different things in the editor
      and the grid.
- [ ] Chords show pending state and time out cleanly.
- [ ] A test asserts every registered command has a non-null handler.

---

### T-U06 — Command palette and Open Anything

**Wave:** 5 | **Size:** M | **Spec:** [wf-15](../wireframes/wf-15-command-palette.md)

**Depends on:** T-U05, T-E04, T-U03
**Blocks:** nothing

**Owns:** `src/Basora.UI/Views/Palette/**`, `src/Basora.UI/ViewModels/Palette/**`,
`tests/Basora.UI.Tests/Palette/**`

**Deliverables** — both modes exactly as specified in wf-15, including mode prefixes,
grouped virtualised results, `Tab` preview, and cached-then-server result merging.

**Acceptance** — the full checklist in [wf-15 section 8](../wireframes/wf-15-command-palette.md).
Hard gate: closing restores the previous focus and caret position exactly.

---

### T-U07 — Dialog, toast and notification services

**Wave:** 3 | **Size:** S | **Spec:** [07-design-system](../07-design-system.md) section 8

**Depends on:** T-U02
**Blocks:** T-P03, T-D06, T-X05

**Owns:** `src/Basora.UI/Services/Dialogs/**`,
`src/Basora.UI/Controls/Toast/**`, `tests/Basora.UI.Tests/Dialogs/**`

**Deliverables**
- `IDialogService` — modal host with focus trapping, focus restoration on close, and
  configurable default/cancel buttons.
- Toast host: bottom-right, auto-dismiss except errors, queueing, and a rule that toasts
  never carry a decision.
- A global error surface for unexpected exceptions that does not kill the app.
- File-picker abstraction so ViewModels stay testable.

**Acceptance**
- [ ] Focus is trapped inside a modal and restored to the exact prior element on close.
- [ ] Error toasts persist until dismissed; others auto-dismiss at 5 s.
- [ ] A toast never contains an action that constitutes a decision.
- [ ] An unhandled exception surfaces without terminating the app and is logged with
      context.

---

### T-U08 — Settings catalog and settings screen

**Wave:** 5 | **Size:** M | **Spec:** [wf-24](../wireframes/wf-24-settings.md)

**Depends on:** T-F10, T-U05, T-U03
**Blocks:** nothing

**Owns:** `src/Basora.UI/Views/Settings/**`, `src/Basora.UI/ViewModels/Settings/**`,
`src/Basora.Core/Settings/**`, `tests/Basora.UI.Tests/Settings/**`

**Deliverables**
- The declarative `SettingKey<T>` catalog covering all thirteen categories from wf-24,
  driving the UI, the search index and the defaults.
- The settings screen with search-first navigation, modified indicators and revert.
- The keymap editor with conflict detection.
- Per-connection override indicators.

**Acceptance** — the full checklist in [wf-24 section 8](../wireframes/wf-24-settings.md).

Additional gate:
- [ ] A test asserts no setting is read anywhere in the codebase via a bare string key —
      all access goes through the catalog.
