# wf-24 — Settings

> Task: `T-U08`. Dialog or document (a document tab is preferred — settings are browsed,
> not decided in a modal).

---

## 1. Purpose

Every preference in one searchable place. The one job it must do well: **be searchable**.
Nobody remembers which category "auto-limit unbounded SELECT" lives in.

---

## 2. Entry points

- `Ctrl+,`
- Tools > Settings, or the gear in the status bar.
- Deep links from elsewhere ("Settings: SQL formatting" from the command palette, or a
  "change this" link in a warning banner).

---

## 3. Layout

```text
+---------------------------------------------------------------------------+
|  Settings                                    [ search settings...     ]   |
+------------------+--------------------------------------------------------+
| General          |  EDITOR                                                |
| Appearance       |                                                        |
| > Editor         |  Font family        [ JetBrains Mono            v ]    |
|   Data Grid      |  Font size          [ 13 ]                             |
|   Query          |  Tab size           [ 4 ]     [x] Insert spaces        |
|   Safety         |  [x] Word wrap                                         |
|   History        |  [x] Show line numbers                                 |
|   Import/Export  |  [ ] Show minimap                                      |
| Connections      |  [x] Auto-close brackets and quotes                    |
| Keyboard         |  [x] Highlight the current statement                   |
| Security         |                                                        |
| AI               |  AUTOCOMPLETE                                          |
| Advanced         |  [x] Trigger automatically after [ 2 ] characters      |
|                  |  [x] Include keywords    [x] Include functions         |
|                  |  Max suggestions    [ 50 ]                             |
|                  |                                                        |
|                  |  FORMATTING                                            |
|                  |  Keyword case  ( ) upper (o) lower ( ) leave as typed  |
|                  |  Indent        [ 4 ] spaces                            |
|                  |  [x] Comma-first lists                                 |
|                  |  [ Preview... ]                                        |
+------------------+--------------------------------------------------------+
|  Settings file: C:\Users\...\Basora\settings.json      [ Open ] [ Reset ]  |
+---------------------------------------------------------------------------+
```

---

## 4. Categories

| Category | Contains |
|---|---|
| **General** | Startup behaviour, workspace restore, update checks, telemetry (off, and not switchable on in Phase 1 — shown so users can verify it is off), language |
| **Appearance** | Theme (system/light/dark/high-contrast), density, UI font and size, accent, tab behaviour, sidebar position |
| **Editor** | Font, size, tabs, wrap, line numbers, minimap, bracket completion, statement highlighting, autocomplete behaviour, formatting rules |
| **Data Grid** | Page size, default sort, NULL display, date/time format, timezone display (server/UTC/local), number formatting, max cell preview length, frozen columns default |
| **Query** | Max rows, memory ceiling, statement timeout, `lock_timeout`, auto-EXPLAIN, plan capture, notice display, transaction warning threshold |
| **Safety** | Safe mode defaults per environment, auto-limit value, confirmation thresholds, row-count threshold for "large", whether estimates are required |
| **History** | Enable/disable, retention days, max entries, record failures, redaction patterns, storage location |
| **Import/Export** | Default formats, CSV defaults, encoding, sensitive-column patterns, default directory |
| **Connections** | Default port, default SSL mode, default timeouts, default application name, auto-reconnect, keepalive |
| **Keyboard** | Full keymap editor with conflict detection and preset switching (see `../09-keyboard-map.md` section 8) |
| **Security** | Secret backend selection and health, vault auto-lock timeout, audit log location and retention, SSH known-hosts management |
| **AI** | Provider, API key (via `ISecretStore`), model, per-connection enablement, context preview, global kill switch |
| **Advanced** | Log level, log location, metadata cache TTL, connection-pool sizes, experimental flags, "Open settings.json" |

---

## 5. Component inventory

### Search

Filters across setting **names, descriptions and keywords**, showing matching settings
grouped by category with the search terms highlighted. Searching is the primary
navigation path; the category list is secondary.

### Setting row

Label, control, and a one-line description beneath in `text.secondary`. A setting that
differs from its default shows a **modified indicator** with a revert action — this makes
"what have I changed?" answerable at a glance.

### Scope

Most settings are global. Some (safe mode, read-only default, history recording, AI
enablement) are **per connection** and are edited in
[wf-02](wf-02-connection-editor.md); the settings screen shows the global default and
says clearly that connections can override it.

### Live application

Settings apply immediately, without a Save button or a restart. The few that genuinely
need a restart are marked and offer to restart.

### Footer

The settings file path with Open and Reset-to-defaults (which confirms and names how many
settings will change).

---

## 6. States

| State | Rendering |
|---|---|
| **Loading** | Instant in practice; skeleton implemented for completeness |
| **Search no match** | "No settings match 'x'" with a suggestion to try the JSON file for advanced keys |
| **Invalid value** | Inline validation beneath the field; the previous value is retained until valid |
| **Settings file unreadable** | Banner: defaults are in use, the file is not being overwritten, with Open file and Reset |
| **Settings file changed externally** | Notice offering to reload |
| **Restart required** | Per-setting badge plus a footer prompt to restart |
| **Overridden per connection** | Badge on the global setting naming which connections override it |

---

## 7. Data contract

```csharp
public interface ISettingsService
{
    T Get<T>(SettingKey<T> key);
    void Set<T>(SettingKey<T> key, T value);
    bool IsModified<T>(SettingKey<T> key);
    void Reset<T>(SettingKey<T> key);
    void ResetAll();
    string FilePath { get; }
    event EventHandler<SettingChangedEventArgs>? Changed;
}

public sealed record SettingKey<T>(
    string Id, string Category, string Label, string Description,
    T DefaultValue, IReadOnlyList<string> Keywords, bool RequiresRestart = false);

public interface ISettingsCatalog
{
    IReadOnlyList<ISettingKey> All { get; }
    IReadOnlyList<ISettingKey> Search(string query);
}
```

Settings are declared **once** in a catalog, which drives the UI, the search index, the
JSON schema and the defaults. No setting exists as a bare string key scattered through
the code — that is what makes search and the modified-indicator possible at all.

---

## 8. Acceptance criteria

- [ ] Search matches names, descriptions and keywords, with highlights.
- [ ] Every setting has a label, a description, and a sensible default.
- [ ] Modified settings show an indicator and a revert action.
- [ ] Settings apply immediately; restart-required settings are marked and offer restart.
- [ ] Per-connection overrides are indicated on the corresponding global setting.
- [ ] Invalid input is rejected inline and never corrupts the stored value.
- [ ] A corrupt settings file falls back to defaults **without overwriting the file**,
      and says so.
- [ ] External changes to the file are detected and offer a reload.
- [ ] Reset to defaults confirms and states how many settings will change.
- [ ] The keymap editor detects conflicts and refuses to save an unresolved one.
- [ ] The AI section shows a global kill switch and a context preview.
- [ ] Telemetry is shown as off and is not switchable on in Phase 1.
- [ ] All seven states render in both themes.
