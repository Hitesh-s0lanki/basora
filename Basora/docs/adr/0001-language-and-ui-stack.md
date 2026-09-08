# ADR-0001 — Language and UI stack

- **Status:** Accepted
- **Date:** 2026-09-08
- **Deciders:** Product owner

---

## Context

The product brief (`docs/idea.md`) specifies **C# / .NET + Avalonia UI + Npgsql**. The
repository as checked in contains a single **F#** Avalonia project
(`Basora/Basora.fsproj`, net10.0, Avalonia 12.1.2) generated from the Avalonia F#
template. This conflict had to be resolved before any architecture or task planning
could be written, because it determines the project layout, the MVVM approach, and every
task card.

Options considered:

1. **C#** — restructure into the multi-project solution the brief describes.
2. **F#** — keep the existing scaffold and adapt the architecture to F# idioms.
3. **Hybrid** — C# for UI/ViewModels, F# for the analyzer and domain projects.

---

## Decision

**C#**, with the multi-project solution laid out in `02-architecture.md`.

---

## Rationale

- **The brief says C#.** Contradicting an explicit product decision needs a strong
  reason; none of the alternatives supplied one.
- **CommunityToolkit.Mvvm source generators** (`[ObservableProperty]`, `[RelayCommand]`)
  do not work for F# types. In F#, every ViewModel property becomes hand-written
  `INotifyPropertyChanged` boilerplate. Basora will have dozens of ViewModels with
  hundreds of bindable properties; that cost compounds badly.
- **Avalonia's XAML tooling, compiled bindings (`x:DataType`), designer preview and
  `ViewLocator` conventions** are all documented, sampled and debugged against C# first.
  F# works, but every problem is solved one step further from the beaten path.
- **Ecosystem breadth for the specific libraries we need** — AvaloniaEdit, LiveCharts2,
  SSH.NET, Testcontainers, NetArchTest — is C#-first. Interop from F# is possible but
  adds friction at every call site.
- **Team scalability.** The C# hiring and contribution pool for an Avalonia desktop app
  is an order of magnitude larger.
- **The hybrid option** is genuinely attractive for `Basora.Analytics` and the SQL
  lexer — discriminated unions and pattern matching fit plan trees and filter ASTs
  beautifully. It was rejected for MVP 1 only on grounds of build complexity and
  two-idiom maintenance cost with a small team. It remains a reasonable future move for
  `Basora.Analytics` alone, since that project is pure and has a narrow interface.

---

## Consequences

**Positive**

- Source generators remove the ViewModel boilerplate problem entirely.
- Every Avalonia sample, issue thread and Stack Overflow answer applies directly.
- One language, one set of analyzers, one style config.

**Negative**

- The existing F# scaffold is discarded. It is a template with no product logic in it,
  so the loss is a few hours at most.
- We give up F#'s conciseness for the analyzer layer. Mitigated by keeping
  `Basora.Analytics` pure and behind a narrow interface, so it could be swapped later.

**Actions**

- `T-F01` performs the restructure: create `src/` and `tests/`, add the eight projects,
  port `App.axaml` / `MainWindow.axaml` / `ViewLocator` to C#, delete the F# project,
  and update `Basora.slnx`.
- `T-F01` is the **only** task permitted to touch the existing scaffold, and it must
  land before any other task starts. It is the sole Wave-0 serialisation point.
