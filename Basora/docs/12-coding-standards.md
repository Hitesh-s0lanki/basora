# 12 — Coding Standards

Enforced by `.editorconfig` + analyzers + `TreatWarningsAsErrors`. If a rule can be
automated, it is automated and does not belong in code review.

---

## 1. Project-wide settings

Set once in `Directory.Build.props`, never per project:

```xml
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<LangVersion>latest</LangVersion>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
<AnalysisLevel>latest-recommended</AnalysisLevel>
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

Package versions live only in `Directory.Packages.props`.

---

## 2. C# conventions

### Naming

| Element | Convention |
|---|---|
| Types, methods, properties, events | `PascalCase` |
| Parameters, locals | `camelCase` |
| Private fields | `_camelCase` |
| Constants | `PascalCase` (not `SCREAMING_CASE`) |
| Interfaces | `IPascalCase` |
| Async methods | suffix `Async` |
| Type parameters | `T`, `TResult`, `TDescriptor` |
| Test methods | `Method_Condition_ExpectedResult` |

Names say what a thing *is*, not what it technically is: `TableDescriptor`, not
`TableInfoData`. No `Manager`/`Helper`/`Util` unless the type genuinely manages
lifetimes (`ISshTunnelManager` does; `SqlHelper` would not).

### Types

- `record` for data, `sealed class` for behaviour. **Every class is `sealed` unless
  designed for inheritance** — and designing for inheritance requires a comment saying
  why.
- `readonly record struct` for small value types.
- `init`-only properties; `required` for genuinely required members.
- Public collections are `IReadOnlyList<T>` / `IReadOnlyDictionary<K,V>`. Never expose a
  mutable collection from a public API.
- Prefer collection expressions (`[]`) and target-typed `new`.
- `file`-scoped namespaces. One public type per file, named after the file.

### Nullability

Non-nullable by default and taken seriously. `!` (null-forgiving) requires a comment
justifying it — most uses are a design smell. Validate at boundaries with
`ArgumentNullException.ThrowIfNull`.

### Async

- `async` all the way down. **No `.Result`, `.Wait()`, or `GetAwaiter().GetResult()`** —
  an analyzer rule, not a guideline.
- `async void` only in event handlers, and those handlers must have a try/catch.
- **Every public async method takes a `CancellationToken`**, positioned last, without a
  default value in interfaces. Enforced by the architecture test.
- `ConfigureAwait(false)` in `Core`, `PostgreSQL`, `Analytics`, `Security`,
  `Infrastructure`. Omit in `UI`, where continuation on the UI thread is wanted.
- `ValueTask` only where profiling proves it matters.
- Never fire-and-forget. Background work goes through a tracked task with a cancellation
  token and an owner.

### Errors

- Expected failures return `Result`/`Result<T>`. Exceptions are for bugs and truly
  exceptional conditions.
- Never `catch (Exception)` without either rethrowing or logging with full context.
- Never swallow a `Task` exception silently.
- `OperationCanceledException` is control flow, not an error: never logged as one, never
  surfaced as an error toast.

### Comments

Comment **why**, never **what**. A comment restating the code is noise; a comment
explaining a non-obvious PostgreSQL behaviour is gold:

```csharp
// reltuples is -1 on a table that has never been analyzed (PG 14+), and stale
// otherwise. Treat any negative value as unknown rather than showing "-1 rows".
```

XML docs on every public `Core` interface member — those are the contracts other people
build against, and they are the docs that actually get read.

---

## 3. XAML conventions

- **Views contain no logic.** Code-behind holds `InitializeComponent`, control-specific
  plumbing that cannot be expressed in XAML, and focus management. Nothing else.
- No inline styles. Every visual value is a `DynamicResource` pointing at a semantic
  token from `07-design-system.md`.
- No hard-coded colours, sizes, margins or font sizes. Ever. A literal `#FF0000` or
  `Margin="7"` in a view fails review.
- `x:CompileBindings="True"` and `x:DataType` on every view — compiled bindings catch
  typos at build time and are dramatically faster.
- Name elements only when referenced; prefix `PART_` for template parts.
- One resource dictionary per control in `Styles/`, merged in `App.axaml`. This is what
  makes the shared-file protocol work: a new control means a new file, not an edit to a
  shared one.
- Prefer `Grid` with named rows/columns over deep nesting. Deeply nested `StackPanel`
  layouts are a performance and maintenance problem.
- Virtualise every list of unbounded length. A non-virtualised `ItemsControl` over
  database objects is a bug.

---

## 4. ViewModel conventions

```csharp
public sealed partial class TableDataViewModel : ViewModelBase
{
    private readonly ITableDataService _dataService;
    private readonly ISafetyEngine _safety;
    private CancellationTokenSource? _loadCts;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    public ObservableCollection<RowViewModel> Rows { get; } = [];

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync(CancellationToken ct) { /* ... */ }
}
```

Rules:

- Constructor injection only. No service locator, no `App.Services.GetService<T>()`
  inside a ViewModel.
- ViewModels depend on `Core` interfaces, never on `Basora.PostgreSQL` (enforced).
- One `CancellationTokenSource` per cancellable operation, cancelled and disposed on
  replacement and on `Dispose`.
- Every ViewModel with resources implements `IDisposable`; the tab that owns it disposes
  it.
- Loading, error and empty are **explicit properties**, not implied by an empty
  collection — a screen cannot render the seven required states otherwise.
- No `Dispatcher` calls in services. Marshalling is the ViewModel's job.

---

## 5. SQL conventions (SQL we write)

- Catalog queries live in `.sql` files as embedded resources, one per method. They are
  reviewable, diffable, and testable in isolation.
- Keywords uppercase; identifiers lowercase; one column per line for anything over three
  columns.
- Always schema-qualify `pg_catalog` objects in our own queries — a user's `search_path`
  is not ours to trust.
- Always parameterise values. Identifiers go through `QuoteIdentifier`, always quoted.
- Every catalog query carries a header comment stating its purpose and the minimum
  server version it supports.

---

## 6. Logging

```csharp
_logger.LogInformation("Opened session {SessionId} to {Host}/{Database} as {Role}",
    sessionId, host, database, role);
```

- Structured logging with named placeholders. Never string interpolation into the
  message template — it defeats structured sinks and the redaction enrichers.
- Levels: `Trace` (wire detail), `Debug` (SQL text, timings), `Information` (lifecycle),
  `Warning` (degraded but working), `Error` (operation failed), `Critical` (app-level).
- **Never log:** passwords, connection strings, parameter values, or result cells.
  See `06-security-and-credentials.md` section 4.

---

## 7. Git and review

### Branches

`feat/T-D05-pending-changes`, `fix/T-Q05-error-position`, `chore/...`. The task ID in
the branch name links code to the task board automatically.

### Commits

Conventional commits, with the task ID:

```text
feat(grid): generate UPDATE statements for pending cell edits [T-D05]

Uses the primary key when present, falling back to a unique index, then ctid
with a warning. Original values are included in the WHERE clause so concurrent
modifications surface as "0 rows affected" instead of a silent overwrite.
```

### Pull requests

Small and single-purpose. A PR that touches files outside its task's declared ownership
must say why in the description — that is the signal that a task boundary was wrong.

### Review checklist

1. Does it honour the layer rules in `02-architecture.md` section 3?
2. Are all async paths cancellable?
3. Are the seven required states implemented (`07-design-system.md` section 9)?
4. Any hard-coded colour, size, or string that should be a token or resource?
5. Does every mutation route through the safety engine?
6. Are new public `Core` members documented?
7. Are generated-SQL changes covered by a golden file?
8. Is anything logged that should not be?

---

## 8. Definition of Done

A task is done when **all** of these hold:

- [ ] Builds clean, zero warnings, on all three platforms
- [ ] Unit tests for the logic, integration tests if it touches PostgreSQL
- [ ] All seven UI states implemented, if it has UI
- [ ] Keyboard-accessible, with shortcuts registered in the keymap
- [ ] Works in light and dark themes at all three densities
- [ ] Cancellable if it can run longer than 200 ms
- [ ] Errors mapped to actionable messages, not raw exception text
- [ ] Public `Core` API documented
- [ ] Task card's acceptance criteria all checked
- [ ] No file touched outside the task's declared ownership (or a justification given)
