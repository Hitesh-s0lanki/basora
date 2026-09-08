# wf-18 — Schema Diff and Migration Generator

> Task: `T-V03` (diff), `T-V04` (migration generator). MVP 2. Document.

---

## 1. Purpose

Answer "what is different between these two databases, and what SQL closes the gap?" The
one job it must do well: **generate migration SQL that is correct and ordered**, with the
safety analysis attached, and never apply it without review.

---

## 2. Entry points

- Tools > Compare schemas.
- Right-click a connection > Compare with...
- Command palette: "Compare schemas".
- MVP 3: automatically from a schema-drift alert.

---

## 3. Layout

```text
+-----------------------------------------------------------------------------+
| Source [ Staging   v]  ->  Target [ Production v]   Schema [ public v]      |
| [ Compare ]   Filter: [x]Tables [x]Columns [x]Indexes [x]Constraints [ ]Perms|
+-----------------------------------------------------------------------------+
| DIFFERENCES (14)                    |  DETAIL                               |
|                                     |                                       |
| v TABLES                            |  users.email                          |
|   + notifications        only source|                                       |
|   - legacy_sessions      only target|  Source (Staging)                     |
|   ! users                 modified  |    email varchar(255) NOT NULL        |
|                                     |                                       |
| v COLUMNS                           |  Target (Production)                  |
|   + users.phone           added     |    email varchar(100) NOT NULL        |
|   + users.last_login      added     |                                       |
|   ! users.email          type       |  ~ Widening varchar(100) -> (255)     |
|   - users.legacy_ref     removed    |    does not rewrite the table.        |
|                                     |    Lock: ACCESS EXCLUSIVE (brief)     |
| v INDEXES                           |                                       |
|   + idx_users_phone       added     |  ALTER TABLE public."users"           |
|   ! idx_orders_created   definition |    ALTER COLUMN "email"               |
|                                     |    TYPE varchar(255);                 |
| > CONSTRAINTS (2)                   |                                       |
| > FUNCTIONS (1)                     |                                       |
+-------------------------------------+---------------------------------------+
| [x] Select all   9 of 14 selected                                           |
| [ Generate migration ]  [ Copy SQL ]  [ Save .sql ]  [ Apply to target... ] |
+-----------------------------------------------------------------------------+
```

---

## 4. Component inventory

### Source and target pickers

Any two of: a live connection+schema, a saved snapshot ([`Database Snapshot`], MVP 3), or
a `.sql` schema file. The **direction is explicit and always visible** — "source to
target" means "make target look like source", and the UI states that in words, because
getting the direction backwards is the classic way to destroy the wrong database.

### Difference tree

Grouped by object kind, each entry marked `+` added (in source only), `-` removed (in
target only), `!` modified. Counts per group. Checkboxes select what goes into the
migration; parent/child selection cascades sensibly (selecting a new table selects its
columns and indexes).

### Detail panel

Side-by-side definitions for the selected difference, with an intra-line diff highlight,
plus the generated statement and its `MigrationImpact` (lock level, rewrite verdict,
size, estimated duration, risk).

### Migration generator

Produces an ordered script:

1. Create new types, sequences and functions
2. Create new tables
3. Add columns (nullable first)
4. Backfill placeholders (as commented TODOs — we never invent a backfill)
5. Add constraints as `NOT VALID`, then validate
6. Create indexes `CONCURRENTLY`
7. Drop indexes, constraints, columns and tables **last**
8. Drop types and functions

Destructive statements are grouped at the end, clearly marked, and **deselected by
default**. A schema diff that silently includes `DROP COLUMN` in the middle of a script
is a hazard.

The script includes a header comment recording source, target, generation time and the
Basora version, plus `SET lock_timeout` guidance for production.

### Apply

Runs the selected statements through the safety ladder as one reviewed batch, with
per-statement progress and full rollback on failure where the statements are
transactional (noting that `CREATE INDEX CONCURRENTLY` cannot run inside a transaction —
the UI splits the script accordingly and says why).

---

## 5. States

| State | Rendering |
|---|---|
| **Not configured** | Source/target pickers with Compare disabled |
| **Comparing** | Progress by object kind, cancellable |
| **No differences** | "These schemas are identical" with the comparison scope restated |
| **Differences found** | The tree |
| **Ignored differences** | A separate collapsed group for noise (whitespace in function bodies, column order, comment-only changes), with per-rule toggles |
| **Generating** | Spinner on the migration panel |
| **Applying** | Per-statement progress with the current statement named |
| **Apply failed** | Failing statement highlighted; what was applied before it stated explicitly; the remaining script offered for manual completion |
| **Permission denied** | Objects the role cannot read are listed as "not compared", never as "identical" |

That last one matters: reporting "no differences" when we simply could not see an object
would be actively dangerous.

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Ctrl+Enter` | Compare |
| `Space` | Toggle the selected difference |
| `Ctrl+A` | Select all differences |
| `Ctrl+G` | Generate migration |
| `Ctrl+C` | Copy the generated SQL |
| `Enter` | Show detail for the selected difference |

Swapping source and target is a single button, and it re-runs the comparison rather than
naively inverting the result.

---

## 7. Data contract

```csharp
public interface ISchemaComparer
{
    Task<Result<SchemaComparison>> CompareAsync(
        SchemaSource source, SchemaSource target, CompareOptions options,
        IProgress<CompareProgress> progress, CancellationToken ct);
}

public sealed record SchemaComparison(
    IReadOnlyList<SchemaDifference> Differences,
    IReadOnlyList<SchemaDifference> Ignored,
    IReadOnlyList<string> NotCompared);          // permission-blocked objects

public sealed record SchemaDifference(
    Guid Id, DbObjectKind Kind, DbObjectRef Ref, DifferenceType Type,
    string? SourceDefinition, string? TargetDefinition, string Summary);

public enum DifferenceType { OnlyInSource, OnlyInTarget, Modified }

public interface IMigrationGenerator
{
    Result<MigrationScript> Generate(
        SchemaComparison comparison, IReadOnlyList<Guid> selectedIds, MigrationOptions options);
}

public sealed record MigrationScript(
    IReadOnlyList<MigrationStatement> Statements, string Header, bool RequiresSplitting);

public sealed record MigrationStatement(
    string Sql, StatementKind Kind, bool IsDestructive, bool CanRunInTransaction,
    MigrationImpact? Impact, string? Comment);
```

`ISchemaComparer` and `IMigrationGenerator` are pure over descriptor inputs and are
golden-file tested.

---

## 8. Acceptance criteria

- [ ] Direction is stated in words and the swap button re-runs the comparison.
- [ ] Tables, columns, indexes, constraints, FKs, sequences, types, views, matviews,
      functions and triggers are all compared.
- [ ] Noise differences are separated into an Ignored group with per-rule toggles.
- [ ] Objects blocked by permissions are reported as "not compared", never "identical".
- [ ] Generated statements are ordered per section 4 and destructive ones are grouped
      last and deselected by default.
- [ ] `CREATE INDEX CONCURRENTLY` is split out of the transaction with an explanation.
- [ ] Every statement carries its `MigrationImpact`.
- [ ] Backfills are emitted as commented TODOs, never invented.
- [ ] Apply routes through the safety ladder and reports exactly what was applied on
      failure.
- [ ] Golden-file tests cover every difference type and generated statement shape.
- [ ] All nine states render in both themes.
