# 11 — Testing Strategy

---

## 1. Shape of the pyramid

```text
              /\        UI smoke        ~5%   Avalonia.Headless, critical paths only
             /  \
            /----\      Integration     ~25%  Testcontainers, real PostgreSQL
           /      \
          /--------\    Unit           ~70%  Core, Analytics, Safety, SQL lexer
         /__________\
```

The ratio is deliberate: the parts of Basora that are most likely to be wrong are pure
logic (safety classification, SQL generation, plan analysis, filter-to-SQL) and catalog
queries against real servers. Neither needs a window.

---

## 2. What gets tested where

| Project | Test type | Backing |
|---|---|---|
| `Basora.Core` | Unit | None. Pure by construction |
| `Basora.Analytics` | Unit | Fixture stat records, golden findings |
| `Basora.Security` | Unit + integration | Real keystores per platform; SSH container |
| `Basora.PostgreSQL` | Integration | Testcontainers, version matrix |
| `Basora.Infrastructure` | Unit | Temp directories |
| `Basora.UI` (ViewModels) | Unit | Fakes for every `Core` interface |
| `Basora.UI` (Views) | Headless smoke | Avalonia.Headless |

---

## 3. Unit testing rules

- **Hand-written fakes over mocks** for the `Core` interfaces. Each interface ships with
  an `InMemory*` implementation in a shared `Basora.TestKit` project — this is the same
  artefact that unblocks parallel feature work, so it is not test-only overhead.
- No test touches the network, the clock, the filesystem or the OS keystore unless that
  is the thing under test. `TimeProvider` is injected everywhere.
- One assertion concept per test; name tests `Method_Condition_ExpectedResult`.

### Golden-file (snapshot) tests

Used with `Verify` wherever we generate text a human will read:

- Generated DDL from the structure editor
- Generated `UPDATE`/`INSERT`/`DELETE` from pending changes
- Generated `WHERE` clauses from the filter AST
- Migration SQL from schema diff
- Formatted SQL from the formatter
- Parsed `EXPLAIN` trees

A diff in these files is always intentional and always reviewed. This catches the class
of bug where generated SQL changes subtly and no assertion notices.

---

## 4. Integration testing

### Server version matrix

```text
PostgreSQL 13  (minimum supported)
PostgreSQL 15
PostgreSQL 17
PostgreSQL latest
```

Every catalog query and every metadata mapping runs against all four. This is the whole
reason the matrix exists: `pg_stat_user_indexes` columns, `attgenerated`, partitioning
catalogs, and `pg_stat_statements` column names have all changed across versions.

Locally, developers run against one version by default (`BASORA_TEST_PG=17`); CI runs the
full matrix.

### The seed schema

One shared fixture schema, deliberately nasty, so that "it works on my simple table" is
never the standard. It must contain:

- a partitioned table with several partitions
- identity columns (`ALWAYS` and `BY DEFAULT`) and a legacy `serial`
- generated (stored) columns
- an enum type, a domain, a composite type
- array columns, `jsonb`, `tstzrange`, `inet`, `bytea`, `uuid`
- a partial index, an expression index, an `INCLUDE` index, a GIN index on `jsonb`
- a multi-column FK, a self-referencing FK, a deferrable constraint, a `NOT VALID`
  constraint
- a view, a materialized view with an index, a table with a dependent view
- a `plpgsql` function whose dollar-quoted body contains semicolons and the literal text
  `DROP TABLE users;`
- a table with mixed-case and space-containing identifiers (`"Order Items"`)
- a table with 1,000,000 rows for pagination and streaming tests (generated, not
  checked in)
- an unlogged table, a table in a non-public schema, and a schema the test role cannot
  read

**Rule: if a metadata feature is not represented in the seed, it is not tested.** Adding
support for a new object kind means adding it to the seed in the same PR.

### Concurrency and failure tests

- Cancel a long query mid-stream; assert the UI-facing task completes fast, the
  connection is discarded, and the pool recovers.
- Kill the container mid-query; assert reconnect behaviour and that no data is lost.
- Two sessions editing the same row; assert optimistic concurrency reports "0 rows
  affected" rather than overwriting.
- Fill the pool; assert queueing rather than failure, and that the metadata channel is
  unaffected.

---

## 5. UI testing

`Avalonia.Headless.XUnit` for a deliberately small set of smoke tests:

- The app starts, the shell renders, both themes apply without a missing-resource
  exception.
- Every `View` resolves for its `ViewModel` through `ViewLocator`.
- **Theme completeness:** every semantic token referenced by any XAML exists in both
  `Light.axaml` and `Dark.axaml`. This is a test, not a review item.
- Every registered command has a non-null handler and, if it declares a shortcut, no
  conflicting binding within its context.
- The data grid renders 10,000 fake rows and virtualises (asserted by realised-container
  count, not by timing).

We deliberately do **not** attempt broad UI automation. It is slow, brittle, and the
logic worth testing lives in ViewModels, which are tested as plain objects.

---

## 6. Performance budgets

Tracked as tests that fail the build when exceeded, run on a fixed CI machine class.

| Scenario | Budget |
|---|---|
| Cold app start to interactive | < 2.0 s |
| Connect + object tree first paint (200 tables) | < 1.5 s |
| First result row rendered after server responds | < 500 ms |
| Scroll 1M-row result | 60 fps, no allocation spike per frame |
| Memory for a 50,000-row x 20-column result | < 250 MB |
| Autocomplete popup after `Ctrl+Space` | < 100 ms |
| SQL format of a 2,000-line document | < 200 ms |
| Cancel acknowledged in UI | < 100 ms |

A regression in these is treated as a bug, not a tradeoff.

---

## 7. Security testing

Covered in detail in `06-security-and-credentials.md` section 9. Summary:

- Redaction corpus: passwords in a dozen shapes must not reach any log sink.
- `ISecretStore` conformance suite across all backends including the fallback vault.
- Serialised `ConnectionProfile` contains no secret-shaped field.
- SSH tunnel binds only to loopback; host-key mismatch is refused.
- TLS failure modes produce specific, correct messages.
- Fuzz the connection-URI parser and the SQL lexer with malformed input; neither may
  throw an unhandled exception.

---

## 8. Safety-engine testing

The highest-value suite in the repo (see `10-safety-rules.md` section 11). Called out
separately here because it must be maintained as a **labelled corpus file**, not as
scattered `[Fact]` methods — that way adding a rule means adding rows, and the corpus
doubles as documentation of exactly what we detect.

---

## 9. CI pipeline

```text
PR:
  restore -> build (warnings as errors) -> format check -> architecture tests
    -> unit tests -> integration tests (PG 17 only) -> headless UI smoke
    -> performance budgets

main (nightly):
  full PG version matrix -> all platform keystore tests (win/mac/linux runners)
    -> packaging smoke on all three OSes
```

**Merge gates:** build clean, all tests green, architecture tests green, coverage on
`Basora.Core` and `Basora.Analytics` at or above 85% (line), no new analyzer warnings.

Coverage is *not* gated on `Basora.UI` — chasing coverage in view code produces
worthless tests.

---

## 10. Manual test checklist

Some things resist automation and are checked before each release:

- Connect to a real remote PostgreSQL over SSH with each auth method.
- Both themes, all three densities, at 100% / 150% / 200% OS scaling.
- Keyboard-only run through Flows 1, 2, 3 and 5 from `08-ux-flows.md` — no mouse.
- Screen reader pass over the connection editor and the data grid.
- Import a 500 MB CSV; cancel halfway; assert nothing was written.
- Run against a database with 5,000 tables; assert the tree stays responsive.
- Pull the network cable mid-query.
