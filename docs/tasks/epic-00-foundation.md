# Epic 00 — Foundation

Waves 0–2. Everything else depends on this epic. It is deliberately small and
deliberately boring: its only job is to make the other 63 tasks independent.

---

### T-F01 — Solution restructure to C# multi-project

**Wave:** 0 | **Size:** M | **Spec:** [ADR-0001](../adr/0001-language-and-ui-stack.md), [02-architecture](../02-architecture.md) section 2

**Depends on:** nothing
**Blocks:** everything

**Owns:** the entire repository (this is the only task with that licence)

**Deliverables**
- `src/` and `tests/` directory structure.
- Eight projects: `Basora.App`, `Basora.UI`, `Basora.Core`, `Basora.PostgreSQL`,
  `Basora.Analytics`, `Basora.AI`, `Basora.Security`, `Basora.Infrastructure`.
- Five test projects plus `Basora.TestKit` (empty shells).
- `App.axaml`, `MainWindow.axaml`, `ViewLocator`, `Program.cs` ported to C#, preserving
  the working `WithDeveloperTools()` wiring already in the F# `Program.fs`.
- The F# project deleted; `Basora.slnx` updated.
- Project references wired exactly as the layer rules allow — no more.

**Acceptance**
- [ ] `dotnet build` clean in Debug and Release, zero warnings.
- [ ] `dotnet run` opens a window on Windows, macOS and Linux.
- [ ] `Basora.Core` has zero project references and zero non-`System` packages.
- [ ] `Basora.UI` does not reference `Basora.PostgreSQL`.
- [ ] No `.fs` files remain.

---

### T-F02 — Build infrastructure and architecture tests

**Wave:** 0 | **Size:** M | **Spec:** [03-tech-stack](../03-tech-stack.md) section 8, [12-coding-standards](../12-coding-standards.md)

**Depends on:** T-F01
**Blocks:** all of wave 1

**Owns:** `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`,
`tests/Basora.Core.Tests/Architecture/**`

**Deliverables**
- `Directory.Build.props` with `Nullable`, `ImplicitUsings`, `LangVersion`,
  `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, `AnalysisLevel`.
- Central package management with every current version pinned.
- `.editorconfig` encoding the naming and style rules.
- Architecture tests enforcing the layer rules in `02-architecture.md` section 3, plus:
  every public async method in a `Core` interface takes a `CancellationToken`; no class
  is unsealed without an attribute or comment justifying it.

**Acceptance**
- [ ] A deliberate layer violation fails the build.
- [ ] A `Core` interface method missing a `CancellationToken` fails a test.
- [ ] An inline `PackageReference` version fails the build.
- [ ] `dotnet format --verify-no-changes` passes.

---

### T-F03 — Result, Error and the error catalogue

**Wave:** 1 | **Size:** S | **Spec:** [04-domain-model](../04-domain-model.md) section 11, [05-postgresql](../05-postgresql-data-layer.md) section 10

**Depends on:** T-F02
**Blocks:** T-F04..T-F07, T-F08, T-F10

**Owns:** `src/Basora.Core/Results/**`, `src/Basora.Core/Errors/**`,
`tests/Basora.Core.Tests/Results/**`

**Deliverables**
- `Result`, `Result<T>`, `Error`.
- `BasoraErrorCodes` — the stable code constants.
- `SqlStateCatalog` mapping every SQLSTATE in `05-postgresql-data-layer.md` section 10 to
  a code, a message template and a remediation hint.

**Acceptance**
- [x] `Result<T>` cannot be constructed in an invalid state (success with an error, or
      failure with a value).
- [x] Every SQLSTATE in the spec table maps to a code, message and remediation.
- [x] An unknown SQLSTATE degrades to a generic code without throwing.

---

### T-F04 — Connection domain models and contracts

**Wave:** 1 | **Size:** S | **Spec:** [04-domain-model](../04-domain-model.md) sections 1–2

**Depends on:** T-F03, T-F05, T-F06
**Blocks:** T-C01, T-C02, T-C03, T-S01, T-F08

**Owns:** `src/Basora.Core/Models/Connections/**`,
`src/Basora.Core/Interfaces/Connections/**`, `tests/Basora.Core.Tests/Connections/**`

**Deliverables**
- `ConnectionProfile`, `DeploymentEnvironment`, `SslMode`, `SshTunnelConfig`,
  `SshAuthMethod`, `ConnectionFolder`.
- `ServerCapabilities`, `SessionState`.
- Interfaces: `IDatabaseSession`, `ISessionManager`, `IConnectionRegistry`,
  `IConnectionValidator`, `IConnectionTester`, `IConnectionUriParser`, `ISecretStore`,
  `ISshTunnelManager`, `IQueryChannel`, `IMetadataChannel`, `IMonitorChannel`.
- JSON serialisation contexts for the persisted types.

**Acceptance**
- [ ] `ConnectionProfile` has no property that can hold secret material.
- [ ] A test serialises a fully populated profile and asserts no secret-shaped field.
- [ ] Environment-to-colour defaults match `04-domain-model.md` section 1.

---

### T-F05 — Metadata domain models and contracts

**Wave:** 1 | **Size:** M | **Spec:** [04-domain-model](../04-domain-model.md) sections 3–4

**Depends on:** T-F03
**Blocks:** T-E01, T-E02, T-M01, T-M02, T-F04, T-F06, T-F08

**Owns:** `src/Basora.Core/Models/Metadata/**`,
`src/Basora.Core/Interfaces/Metadata/**`, `tests/Basora.Core.Tests/Metadata/**`

**Deliverables**
- `DbObjectKind`, `DbObjectRef`, `ObjectNode`.
- `TableDescriptor`, `ColumnDescriptor`, `PgType`, `PgTypeCategory`, `IdentityKind`,
  `IndexDescriptor`, `ConstraintDescriptor`, `ConstraintKind`, `ForeignKeyDescriptor`,
  `ReferentialAction`, `Cardinality`, `TriggerDescriptor`, `TableStatistics`.
- Interfaces: `IMetadataService`, `IMetadataCache`, `ITypeCatalog`,
  `IObjectSearchService`, `IDdlGenerator`.
- `SqlIdentifier.Quote` / `QuoteQualified` — the single quoting utility.

**Acceptance**
- [x] `QuoteIdentifier` always quotes, doubles embedded quotes, and rejects null bytes.
- [x] A property test asserts `Quote` output is a valid PostgreSQL identifier for any
      input string.
- [x] `DbObjectRef.QualifiedName` is correct with and without a schema.

---

### T-F06 — Query, filter and change-set models

**Wave:** 1 | **Size:** M | **Spec:** [04-domain-model](../04-domain-model.md) sections 5–7

**Depends on:** T-F03, T-F05, T-F07
**Blocks:** T-Q01, T-Q04, T-D01, T-D02, T-D05, T-D07, T-F04, T-F08

**Owns:** `src/Basora.Core/Models/Query/**`, `src/Basora.Core/Models/Filters/**`,
`src/Basora.Core/Models/Changes/**`, `src/Basora.Core/Interfaces/Query/**`,
`tests/Basora.Core.Tests/Query/**`

**Deliverables**
- `QueryRequest`, `QueryResultSet`, `ResultColumn`, `QueryParameter`, `ResultChunk`,
  `PostgresNotice`.
- `FilterNode`, `FilterGroup`, `FilterCondition`, `FilterOperator`, `LogicalOperator`.
- `ChangeSet`, `PendingChange`, `RowInsert`, `RowUpdate`, `RowDelete`, `CellEdit`,
  `RowIdentityStrategy`, `PaginationMode`.
- Interfaces: `IQueryExecutor`, `ITableDataService`, `IChangeSetService`,
  `IChangeSqlGenerator`, `IFilterSqlBuilder`, `IFilterOperatorCatalog`, `ITransactionScope`.

**Acceptance**
- [ ] The filter AST round-trips through serialisation.
- [ ] `IFilterOperatorCatalog` returns a non-empty operator list for every
      `PgTypeCategory` and never an inapplicable operator.
- [ ] `ValueCount` is correct for `IS NULL` (0), `BETWEEN` (2) and `IN` (n).

---

### T-F07 — Explain, safety and health models

**Wave:** 1 | **Size:** S | **Spec:** [04-domain-model](../04-domain-model.md) sections 9–10

**Depends on:** T-F03
**Blocks:** T-P01, T-P02, T-P05, T-F06, T-F08

**Owns:** `src/Basora.Core/Models/Explain/**`, `src/Basora.Core/Models/Safety/**`,
`src/Basora.Core/Models/Health/**`, `src/Basora.Core/Interfaces/Safety/**`,
`tests/Basora.Core.Tests/Safety/**`

**Deliverables**
- `ExplainPlan`, `ExplainNode`, `PlanFinding`, `FindingSeverity`.
- `SafetyVerdict`, `RiskLevel`, `ConfirmationLevel`, `SafetyFinding`, `StatementKind`,
  `SafetyRequest`, `TableFacts`, `MigrationImpact`.
- `HealthSnapshot`, `MetricValue`, `ThresholdState`, `HealthFinding`.
- Interfaces: `ISafetyEngine`, `ISafetyProbe`, `ISafetyDialogService`,
  `IMigrationSafetyAnalyzer`, `IExplainService`, `IPlanAnalyzer`.
- `LockLevelTable` — the operation-to-lock table from `10-safety-rules.md` section 4, as
  data.

**Acceptance**
- [ ] `RiskLevel` and `ConfirmationLevel` escalation helpers clamp correctly at both ends.
- [ ] The lock-level table covers every operation in the spec table.
- [ ] The environment policy matrix from `10-safety-rules.md` section 6 is encoded as
      data and unit-tested cell by cell.

---

### T-F08 — Basora.TestKit — fakes for every contract

**Wave:** 2 | **Size:** M | **Spec:** [tasks/README](README.md) section 4, [11-testing](../11-testing-strategy.md) section 3

**Depends on:** T-F03, T-F04, T-F05, T-F06, T-F07
**Blocks:** every UI task, and all parallel work in waves 3–5

**Owns:** `tests/Basora.TestKit/**`

**Deliverables**
- An in-memory fake for **every** interface landed in waves 1–2.
- `FakeMetadataService` seeded with a synthetic schema: 5,000 tables in one schema for
  virtualisation tests, plus the "nasty schema" shapes from
  `11-testing-strategy.md` section 4.
- `FakeTableDataService` producing synthetic rows covering every `PgTypeCategory`,
  including NULLs, long text, deep `jsonb` and `bytea`.
- Deterministic seeding (fixed RNG seed) so tests and screenshots are reproducible.
- A `TimeProvider` test double.

**Acceptance**
- [ ] Every `Core` interface has a fake — asserted by a reflection test that fails when a
      new interface is added without one.
- [ ] The fake schema contains every object kind, index kind and constraint kind.
- [ ] Building the 5,000-table fake tree takes under 100 ms.

---

### T-F09 — CI pipeline and packaging skeleton

**Wave:** 1 | **Size:** M | **Spec:** [11-testing](../11-testing-strategy.md) section 9

**Depends on:** T-F02
**Blocks:** T-Z03

**Owns:** `.github/workflows/**`, `build/**`, `src/Basora.App/Composition/**`,
`src/Basora.App/Program.cs`

**Deliverables**
- PR workflow: restore, build, format check, architecture tests, unit tests, integration
  tests (PG 17), headless UI smoke.
- Nightly workflow: full PostgreSQL version matrix, all three OS runners.
- Composition root: `Program.cs` calling `Add<Module>()` extension methods in a fixed
  order, with each module owning its own file per the shared-file protocol.
- Unsigned packaging scripts for all three platforms (signing lands in `T-Z03`).

**Acceptance**
- [ ] PR workflow completes in under 10 minutes.
- [ ] A failing architecture test blocks the merge.
- [ ] `Program.cs` is the only file editing the module call order, and each module's
      registration lives in its own file.
- [ ] An unsigned artefact is produced for Windows, macOS and Linux.

---

### T-F10 — Infrastructure: settings, logging, workspace

**Wave:** 2 | **Size:** M | **Spec:** [02-architecture](../02-architecture.md) section 6, [wf-24](../wireframes/wf-24-settings.md)

**Depends on:** T-F03
**Blocks:** T-U03, T-U04, T-U08, T-H01

**Owns:** `src/Basora.Infrastructure/**`, `tests/Basora.Infrastructure.Tests/**`

**Deliverables**
- `ISettingsService` + `ISettingsCatalog` with declarative `SettingKey<T>` definitions.
- Serilog configuration with file rolling and the redaction hook point (`T-S04` fills it).
- `IWorkspaceService` — debounced save, atomic write, versioned schema, defaults on
  corruption.
- Platform path resolution for config, logs, data and cache directories.

**Acceptance**
- [ ] A corrupt settings file falls back to defaults **without overwriting the file**.
- [ ] Workspace save is debounced and atomic (write-temp-then-rename); a kill mid-write
      never corrupts the file.
- [ ] Settings changes raise `Changed` and apply without restart.
- [ ] Paths resolve correctly on all three platforms.
