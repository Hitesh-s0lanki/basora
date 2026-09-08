# 07 — Design System

Owner: `Basora.UI/Themes/` and `Basora.UI/Styles/`. Every wireframe doc references
tokens defined here by name. **No screen defines its own colour, spacing or font size.**

---

## 1. Design principles

1. **Density over decoration.** This is a tool people stare at for eight hours. Compact
   rows, tight gutters, no decorative whitespace. Content is the interface.
2. **Chrome recedes, data advances.** Chrome is low-contrast grey; data is high-contrast.
   The eye should land on a cell value, not a toolbar.
3. **Colour carries meaning only.** Colour means environment, risk, state or data type.
   Nothing is coloured to look nice.
4. **Motion is functional.** Under 150 ms, only to explain a spatial change. Nothing
   animates on a data path — a grid that animates while streaming 50,000 rows is a bug.
5. **Every state is designed.** Empty, loading, partial, error, permission-denied and
   truncated are first-class, not afterthoughts. See section 9.

---

## 2. Token architecture

Three layers. UI never references a primitive directly.

```text
Primitive        Semantic                Component
--------         --------                ---------
gray.900    ->   surface.background  ->  grid.header.background
blue.600    ->   accent.default      ->  button.primary.background
red.600     ->   env.production      ->  tab.production.indicator
```

Defined in `Themes/Tokens.axaml` (primitives), `Themes/Light.axaml` and
`Themes/Dark.axaml` (semantic), and per-control dictionaries in `Styles/` (component).
Adding a screen means consuming semantic tokens; it never means adding primitives.

---

## 3. Colour primitives

Neutral ramp (the workhorse — most of the UI is these):

| Token | Light | Dark |
|---|---|---|
| `gray.0` | `#FFFFFF` | `#0D1117` |
| `gray.50` | `#FAFBFC` | `#12171F` |
| `gray.100` | `#F2F4F7` | `#171D26` |
| `gray.200` | `#E4E7EC` | `#1F2630` |
| `gray.300` | `#D0D5DD` | `#2A323D` |
| `gray.400` | `#98A2B3` | `#3D4754` |
| `gray.500` | `#667085` | `#5A6674` |
| `gray.600` | `#475467` | `#7C8899` |
| `gray.700` | `#344054` | `#A0AAB8` |
| `gray.800` | `#1D2939` | `#C6CDD6` |
| `gray.900` | `#101828` | `#E6EAEF` |
| `gray.1000` | `#000000` | `#FFFFFF` |

Accents:

| Token | Light | Dark | Use |
|---|---|---|---|
| `blue.600` / `blue.500` | `#2563EB` | `#3B82F6` | Primary action, selection, focus |
| `green.600` / `green.500` | `#16A34A` | `#22C55E` | Success, local environment, additions |
| `amber.600` / `amber.500` | `#D97706` | `#F59E0B` | Warning, staging, modified rows |
| `red.600` / `red.500` | `#DC2626` | `#EF4444` | Error, production, deletions |
| `purple.600` / `purple.500` | `#7C3AED` | `#8B5CF6` | AI-generated content — always visually distinct |
| `teal.600` / `teal.500` | `#0D9488` | `#14B8A6` | Informational highlights, plan heat low end |

**Contrast requirement:** all text meets WCAG AA (4.5:1 body, 3:1 for 18px+ and UI
glyphs). The AI purple and the environment reds must be checked against both themes;
these are the two that historically fail.

---

## 4. Semantic tokens

### Surfaces

| Token | Purpose |
|---|---|
| `surface.background` | App background |
| `surface.panel` | Sidebar, panels |
| `surface.raised` | Popovers, dialogs, completion list |
| `surface.overlay` | Modal scrim (`gray.1000` at 40%) |
| `surface.inset` | Editor gutter, code blocks |

### Borders

| Token | Purpose |
|---|---|
| `border.subtle` | Grid lines, panel dividers |
| `border.default` | Input borders, card outlines |
| `border.strong` | Focused input |
| `border.focus` | Focus ring — `accent.default`, 2px, always visible |

### Text

| Token | Purpose |
|---|---|
| `text.primary` | Cell values, body |
| `text.secondary` | Labels, column headers |
| `text.tertiary` | Hints, timestamps, row counts |
| `text.disabled` | |
| `text.inverse` | On accent fills |
| `text.null` | The `NULL` badge — `text.tertiary` at 70%, italic |

### Status and environment

| Token | Colour | Applied to |
|---|---|---|
| `status.success` | green | Committed, connected, passing |
| `status.warning` | amber | Truncated results, stale stats, degraded |
| `status.danger` | red | Errors, destructive actions |
| `status.info` | blue | Notices, hints |
| `status.ai` | purple | Anything model-generated |
| `env.local` | green | Environment chrome |
| `env.dev` | blue | |
| `env.staging` | amber | |
| `env.production` | red | |

**Environment chrome rule.** The environment colour appears in exactly four places, and
nowhere else: (1) a 3px top border on the connection tab, (2) the connection card
accent bar, (3) the status-bar connection pill, (4) a 2px window-edge border for
Production only. Overusing it destroys its signal value.

### Data-state tokens (grid)

| Token | Meaning |
|---|---|
| `data.modified` | Cell edited, uncommitted — amber left border + tinted background |
| `data.inserted` | New row — green tinted background |
| `data.deleted` | Marked for delete — red tint + strikethrough |
| `data.invalid` | Failed validation — red border + inline message |
| `data.selection` | Selected cells — `accent` at 12% |
| `data.match` | Search match — amber at 25% |

---

## 5. Typography

Two families:

| Role | Family | Fallbacks |
|---|---|---|
| UI | **Inter** (already bundled via `Avalonia.Fonts.Inter`) | Segoe UI Variable, SF Pro, system-ui |
| Code and data | **JetBrains Mono** (bundle it) | Cascadia Code, SF Mono, Consolas, monospace |

**Monospace is used for:** SQL, all grid cell values, identifiers, type names, numbers,
plan node details, log output. Data alignment matters more than prettiness — proportional
digits in a numeric column are unreadable.

| Token | Size | Line height | Weight | Use |
|---|---|---|---|---|
| `type.display` | 24 | 32 | 600 | Health score, dashboard hero |
| `type.title` | 18 | 26 | 600 | Dialog titles |
| `type.heading` | 15 | 22 | 600 | Section headers |
| `type.body` | 13 | 20 | 400 | Default UI |
| `type.label` | 12 | 16 | 500 | Column headers, field labels |
| `type.caption` | 11 | 16 | 400 | Timestamps, hints |
| `type.code` | 13 | 20 | 400 | Editor, cells (mono) |
| `type.code.small` | 12 | 18 | 400 | Inline SQL preview (mono) |

Base UI size is user-adjustable (11–16); every size is derived from base so the whole
app scales together.

---

## 6. Spacing, radius, elevation

**Spacing** — 4px base: `space.1`=4, `.2`=8, `.3`=12, `.4`=16, `.5`=20, `.6`=24,
`.8`=32, `.10`=40, `.12`=48.

**Radius** — `radius.sm`=3 (inputs, buttons, badges), `radius.md`=6 (cards, panels),
`radius.lg`=10 (dialogs), `radius.full` (pills). Grid cells have no radius.

**Elevation** — four levels only:

| Token | Use | Shadow (light) |
|---|---|---|
| `elevation.0` | Flat surfaces | none |
| `elevation.1` | Cards, docked panels | `0 1px 2px rgba(16,24,40,.06)` |
| `elevation.2` | Popovers, dropdowns, completion | `0 4px 12px rgba(16,24,40,.10)` |
| `elevation.3` | Modals | `0 16px 40px rgba(16,24,40,.18)` |

In dark theme, elevation is expressed by **surface lightening**, not shadow. Shadows on
a near-black background are invisible.

---

## 7. Density

Three modes, user-selectable, applied globally:

| Mode | Grid row | Tree row | Toolbar | Control |
|---|---|---|---|---|
| Compact | 22px | 22px | 32px | 26px |
| **Default** | 26px | 26px | 36px | 30px |
| Comfortable | 32px | 30px | 42px | 34px |

Row height is a single token (`density.row.height`) consumed by every list, tree and
grid. A screen must never hard-code a row height.

---

## 8. Core component specs

### Buttons

| Variant | Fill | Text | Border | Use |
|---|---|---|---|---|
| Primary | `accent.default` | `text.inverse` | none | The one main action per surface |
| Secondary | `surface.panel` | `text.primary` | `border.default` | Everything else |
| Ghost | transparent | `text.secondary` | none | Toolbar icons |
| Danger | `status.danger` | `text.inverse` | none | Confirmed destructive action |

Height follows density. Focus ring is always drawn, never removed.

### Inputs

Height per density, `radius.sm`, `border.default`, focus to `border.focus` with a 2px
ring. Validation errors render **below** the field in `status.danger` `type.caption` —
never as a tooltip, never as placeholder text.

### Badges and pills

Height 18px, `radius.full`, `type.caption`, 6px horizontal padding. Used for: `PK`, `FK`,
`NOT NULL`, index method, environment, row count, `NULL`, `AI`.

### Data grid (the most important control)

- Header: `surface.panel`, `type.label`, `text.secondary`, sticky, resizable, reorderable,
  sort indicator, type glyph on the left of the name.
- Rows: alternating rows **off** by default (stripes fight with the modified/inserted
  tints, which carry real meaning). A 1px `border.subtle` row separator instead.
- Cells: `type.code`, 8px horizontal padding, single line with ellipsis, full value on
  hover after 500 ms and in the inspector panel.
- Alignment: numbers right, booleans centre, everything else left.
- `NULL`: the literal text `NULL` in `text.null`, italic. Never blank — an empty string
  and a NULL must be distinguishable at a glance.
- Selection: cell-level, row-level and column-level, all keyboard-navigable.
- Frozen first column (usually the key) is optional and remembered per table.

### Tabs

Two levels. **Connection tabs** at the top carry the environment colour on a 3px top
border. **Document tabs** below are neutral, show a dirty dot for unsaved SQL and a
pending-change count badge for tables with uncommitted edits.

### Toasts

Bottom-right, `elevation.2`, auto-dismiss after 5 s except errors, which persist until
dismissed. Never used for anything requiring a decision — that is a dialog.

---

## 9. Required states

Every data-showing surface implements all seven. A screen that ships without them is not
done, and each wireframe doc restates them concretely.

| State | Requirement |
|---|---|
| **Loading** | Skeleton rows for grids/trees, never a centred spinner over a blank panel. Cancel available after 2 s |
| **Empty (no data)** | Explain *why* it is empty and give the next action ("no rows match this filter — clear filter") |
| **Empty (not configured)** | Explain what is missing and how to fix it ("`pg_stat_statements` is not installed. Ask a superuser to run: `CREATE EXTENSION pg_stat_statements;`") |
| **Permission denied** | Name the object, the privilege, and the exact `GRANT` that would fix it |
| **Error** | Message + SQLSTATE + a retry action + "copy details". Never a raw stack trace in the UI |
| **Truncated** | Explicit banner: "showing first 50,000 of ~4.2M rows" with load-more and export-all |
| **Stale** | For monitoring surfaces: when the last poll is older than 2 intervals, dim and show the age |

---

## 10. Iconography

One 16px line-icon set, 1.5px stroke, consistent optical weight. Object-kind icons must
be distinguishable at 16px in both themes — table vs view vs matview is the pair users
most often confuse, so they need distinct silhouettes, not just distinct colours
(colour-blind users get nothing from a hue-only difference).

Type glyphs in column headers: `#` numeric, `T` text, `?` boolean, clock datetime,
`{}` json, braces-array for arrays, key for uuid, binary for bytea.

---

## 11. Accessibility

- **Keyboard reachability is absolute.** Every action has a keyboard path. Focus order
  is logical, focus is always visible, focus traps in modals and returns on close.
- **No colour-only meaning.** Modified cells get a left border *and* a tint. Risk levels
  get an icon *and* a colour. Environment gets a label *and* a colour.
- **Screen reader:** `AutomationProperties.Name` on every interactive element; grid cells
  announce column name, type, and NULL-ness.
- **Reduced motion:** honour the OS setting; disable all transitions when set.
- **Zoom:** the app remains usable at 200% OS scaling and at base font 16.
- **High contrast:** a third theme variant that maximises borders and disables tints in
  favour of patterns.

---

## 12. Theme implementation notes

- `RequestedThemeVariant="Default"` follows the OS; the user may pin Light, Dark or High
  Contrast. The setting is applied without restart.
- Semantic tokens are `DynamicResource` so a theme switch repaints live. Component
  dictionaries must not use `StaticResource` for colours.
- Every colour is defined in **both** `Light.axaml` and `Dark.axaml`. A token that exists
  in only one is a build failure (`T-U02` adds the check).
- The editor (AvaloniaEdit) has its own highlighting definition per theme; SQL syntax
  colours are derived from the same primitives so the editor never looks foreign.
