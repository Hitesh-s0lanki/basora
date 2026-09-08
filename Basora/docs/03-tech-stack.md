# 03 — Technology Stack

> **Version policy.** Exact package versions are pinned once, in
> `Directory.Packages.props` (central package management), by task `T-F01`. The table
> below records the *target* and the *reason*. Do not add a `PackageReference` with an
> inline version in a project file — CI rejects it.

---

## 1. Platform

| Concern | Choice | Notes |
|---|---|---|
| Runtime | **.NET 10** | Already targeted by the scaffold (`net10.0`) |
| Language | **C# 13+**, `nullable enable`, `ImplicitUsings enable` | See ADR-0001 |
| UI | **Avalonia 12.1.x** | Already in the scaffold; one codebase for Windows/macOS/Linux |
| Packaging | Windows: MSIX or Velopack. macOS: signed+notarised `.app`. Linux: AppImage + `.deb` | Deferred to `T-F09` |

---

## 2. Core dependencies

| Package | Used in | Why |
|---|---|---|
| `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter` | UI, App | Base UI stack, already present |
| `Avalonia.Diagnostics` / `AvaloniaUI.DiagnosticsSupport` | App (Debug only) | Dev tools; already wired via `WithDeveloperTools()` |
| `CommunityToolkit.Mvvm` | UI | `[ObservableProperty]`, `[RelayCommand]`, `IMessenger`. Source generators are a major reason ADR-0001 chose C# |
| `Npgsql` | PostgreSQL | The PostgreSQL driver. `NpgsqlDataSource`, async streaming, `COPY`, notifications |
| `Microsoft.Extensions.DependencyInjection` | App | Composition root |
| `Microsoft.Extensions.Logging` + `Serilog` (+ `Serilog.Sinks.File`, `Serilog.Extensions.Logging`) | Infrastructure | Structured, file-rotating logs with redaction enrichers |
| `System.Text.Json` | Infrastructure | Settings, workspace state, saved queries. Source-generated contexts for startup speed |

---

## 3. Feature dependencies

| Package | Feature | Notes |
|---|---|---|
| `AvaloniaEdit` (`Avalonia.AvaloniaEdit`) | SQL editor | Text editor with folding, highlighting, completion window. Must match the Avalonia major version |
| `SSH.NET` (`Renci.SshNet`) | SSH tunnels | Local port forwarding for `Basora.Security`. Verify maintained fork/version at `T-S03` |
| `LiveChartsCore.SkiaSharpView.Avalonia` | Charts, metrics board, sparklines | MVP 2. Skia-backed, matches Avalonia's renderer |
| `SkiaSharp` | ER diagram, visual EXPLAIN | Custom-drawn canvases; Avalonia already ships Skia |
| A SQL parser/lexer | Autocomplete, statement splitting, safety analysis | See section 5 — this is the one genuinely open choice |

---

## 4. Test dependencies

| Package | Purpose |
|---|---|
| `xunit`, `xunit.runner.visualstudio` | Test framework |
| `Shouldly` | Assertions. **Not FluentAssertions** — its licence changed at v8; avoid the ambiguity |
| `NSubstitute` | Mocking where a hand-written fake is not worth it |
| `Testcontainers.PostgreSql` | Real PostgreSQL per integration-test class. Non-negotiable: catalog queries cannot be meaningfully faked |
| `Avalonia.Headless.XUnit` | ViewModel + view smoke tests without a display server |
| `Verify.Xunit` | Golden-file snapshots for generated SQL and EXPLAIN parsing |
| `NetArchTest.Rules` or a hand-rolled reflection test | Enforces the layer rules in `02-architecture.md` section 3 |

---

## 5. Open decision — SQL parsing

Autocomplete, statement splitting, formatting and safety analysis all need to
understand SQL text to differing depths. Three tiers, and we should be honest about
which each feature needs:

| Tier | Technique | Good enough for |
|---|---|---|
| **Lexer** | Hand-written tokenizer (strings, dollar-quotes, comments, identifiers) | Statement splitting, keyword highlighting, "is there a WHERE clause", cursor-context detection |
| **Shallow parse** | Lexer + a small recursive-descent parser for the statement head | Autocomplete context (which table, which aliases), DDL classification for the safety engine |
| **Full parse** | `libpg_query` bindings, or PostgreSQL's own grammar | Exact rewriting, provable safety analysis |

**Recommendation:** build the **lexer** ourselves in `Basora.Core/Sql/` (task `T-Q01`).
It is a few hundred lines, has no native dependency, must handle dollar-quoting and
nested comments correctly, and it unblocks statement splitting, the safety engine and
basic completion. Escalate to `libpg_query` only if a concrete MVP-2 feature demands it.

**Do not** take a dependency on a general-purpose multi-dialect SQL parser. PostgreSQL's
dollar-quoting, arrays, casts and `jsonb` operators break most of them.

For **formatting**, evaluate an existing .NET SQL formatter before writing one; if
nothing suits PostgreSQL, a token-stream formatter over our own lexer is the fallback
(`T-Q03`).

---

## 6. AI provider (MVP 3)

`Basora.AI` defines its own `IAiProvider` abstraction and a `DatabaseContextBuilder`.
Nothing in the app binds to a vendor SDK outside that project.

Requirements that drive the abstraction:

- **Streaming responses** — answers must render token-by-token.
- **Tool/function calling** — the assistant asks for schema, plans, and statistics
  rather than being handed the whole database up front.
- **Strict read-only tooling** — the tool surface exposed to the model contains no
  mutating operation. Generated SQL is *returned to the user*, never executed.
- **Bring-your-own-key** — the user supplies a key, stored via `ISecretStore`. No
  Basora-operated proxy in Phase 1.

Anthropic Claude is the intended default provider. Concrete model identifiers, pricing
assumptions and SDK version are pinned at MVP-3 implementation time (`T-AI01`), not
now — they move faster than this document will.

---

## 7. Rejected alternatives

| Rejected | In favour of | Reason |
|---|---|---|
| WPF / WinUI | Avalonia | Windows-only; the brief requires macOS and Linux |
| Electron / Tauri + web UI | Avalonia | Native performance for 1M-row grids; single language; no Node toolchain |
| MAUI | Avalonia | Desktop story is weaker, Linux unsupported |
| Entity Framework Core | Raw Npgsql | We are a database *tool*. We need the driver's edges (COPY, cancellation, notices, raw types), not an abstraction over them |
| ReactiveUI | CommunityToolkit.Mvvm | Smaller conceptual surface, source generators, less ceremony for a team that will grow |
| Multi-dialect SQL parser | Own lexer | See section 5 |
| FluentAssertions | Shouldly | Licence change at v8 |
| Dapper | Raw Npgsql | Micro-ORM adds nothing when every query is a hand-tuned catalog query returning a bespoke shape |

---

## 8. Repository conventions

- **Central package management** — `Directory.Packages.props` at the repo root holds
  every version; project files list bare `PackageReference` items.
- **`Directory.Build.props`** sets `TargetFramework`, `Nullable`, `ImplicitUsings`,
  `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, and `LangVersion` once for all
  projects. Never repeat these per project.
- **`.editorconfig`** at the root is the single style authority (see
  `12-coding-standards.md`).
- **`Basora.slnx`** stays the solution format already in use.
