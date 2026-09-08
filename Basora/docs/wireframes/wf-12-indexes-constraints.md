# wf-12 — Indexes, Constraints, Relations and Triggers

> Task: `T-M05`. Four sub-tabs of [wf-05](wf-05-table-document.md), specified together
> because they share one layout pattern and one data source.

---

## 1. Purpose

Everything attached to a table that is not a column. The one job these tabs must do well:
**show usage, not just definitions.** An index list that does not show scan counts is a
catalogue; one that does is a diagnosis, and it is the on-ramp to Index Intelligence.

---

## 2. Entry points

- Indexes / Constraints / Relations / Triggers sub-tabs of a table document.
- "Show indexes" from an index finding in the health dashboard.
- Clicking an index name in a query plan.

---

## 3. Layout

### Indexes

```text
+-----------------------------------------------------------------------------+
| [+ Index] [- Drop] [Reindex]                                    [ Refresh ] |
+-------------------+--------+-----------------+--------+--------+------------+
| Name              | Method | Columns         | Unique | Size   | Scans      |
+-------------------+--------+-----------------+--------+--------+------------+
| users_pkey        | btree  | id              | yes PK | 24 MB  | 1,482,301  |
| idx_users_email   | btree  | email           | yes    | 31 MB  |    82,312  |
| idx_users_status  | btree  | status          | no     | 18 MB  |     4,110  |
| idx_users_phone   | btree  | phone           | no     |  8 GB  |         0  | ~ unused
| idx_users_st_cr   | btree  | status, created | no     | 22 MB  |       ...  | ~ duplicate of idx_users_status
+-------------------+--------+-----------------+--------+--------+------------+
| 5 indexes - 8.1 GB total - 2 findings                                       |
+-----------------------------------------------------------------------------+
| ~ idx_users_phone: 8 GB, 0 scans since stats reset (14 days ago).           |
|   Consider dropping it.  [ View SQL ]  [ Drop... ]                          |
+-----------------------------------------------------------------------------+
```

### Constraints

```text
| Name                  | Type        | Definition                   | Valid  |
| users_pkey            | PRIMARY KEY | (id)                         | yes    |
| users_email_key       | UNIQUE      | (email)                      | yes    |
| users_status_check    | CHECK       | status IN ('active','off')   | yes    |
| users_org_id_fkey     | FOREIGN KEY | (org_id) -> orgs(id) CASCADE | NOT VALID |
```

### Relations

```text
   REFERENCED BY (children)              REFERENCES (parents)
   users                                 users
    |-- orders.customer_id      1:N       |-- organizations.id     N:1
    |-- addresses.user_id       1:N       +-- plans.id             N:1
    |-- sessions.user_id        1:N
    +-- payments.user_id        1:N

   [ Show as diagram ]  (opens wf-17)
```

### Triggers

```text
| Name             | Timing        | Events         | Function          | Enabled |
| trg_users_audit  | AFTER ROW     | INSERT, UPDATE | audit.log_change  | yes     |
| trg_users_touch  | BEFORE ROW    | UPDATE         | public.touch_ts   | yes     |
```

---

## 4. Component inventory

### Shared pattern

All four tabs use the same shape: a toolbar, a sortable list, a footer with totals and a
finding count, and a **findings strip** beneath that surfaces problems with a
recommendation and an action. Selecting a row shows its full definition in a detail
panel (`pg_get_indexdef`, `pg_get_constraintdef`, `pg_get_triggerdef`).

### Index findings (the value-add)

| Finding | Condition | Recommendation |
|---|---|---|
| Unused | 0 scans since the last stats reset, and the reset is old enough to trust | Consider dropping; shows the reclaimed size |
| Duplicate | Identical column list, method and predicate to another index | Drop one; names which |
| Redundant | Column list is a strict prefix of another index's | Usually droppable; explains why "usually" |
| Invalid | `indisvalid = false` — a failed `CONCURRENTLY` build | Drop and rebuild |
| Bloated | Estimated bloat above threshold | `REINDEX CONCURRENTLY` |
| Never analyzed | Stats absent | Run `ANALYZE` before trusting the numbers |

Every finding states the **stats reset time** alongside it. "0 scans" means nothing if
statistics were reset an hour ago, and presenting it without that context would be
misleading. **We never offer to drop an index automatically.**

### Create index dialog

Columns (ordered, with sort direction and `NULLS FIRST/LAST`), method, unique, `INCLUDE`
columns, partial predicate, `CONCURRENTLY` (**default on**, with an explanation of the
trade-off), tablespace, and fill factor. Generates the SQL for review and routes through
the safety ladder.

### Constraint actions

Add (PK, unique, check, FK, exclusion), drop, validate a `NOT VALID` constraint. FK
creation offers `NOT VALID` + `VALIDATE` as the production-safe path by default.

### Relations

Two trees: children (tables referencing this one) and parents. Each entry shows the FK
name, the columns on both sides, the cardinality, and the `ON DELETE`/`ON UPDATE`
actions. Clicking navigates to the related table document. "Show as diagram" opens
[wf-17](wf-17-er-diagram.md) scoped to this table's neighbourhood.

---

## 5. States

| State | Rendering |
|---|---|
| **Loading** | Skeleton rows with real headers |
| **Empty** | Per tab: "No indexes besides the primary key", "No constraints", "No tables reference this one", "No triggers" — each with a create action where applicable |
| **Stats unavailable** | Scan counts show `-` with a tooltip explaining the role lacks access to `pg_stat_user_indexes`, and findings that depend on them are suppressed rather than shown as false |
| **Never analyzed** | Banner: statistics have never been collected; findings suppressed; offers `ANALYZE` |
| **Permission denied** | Names the privilege and the `GRANT` |
| **Error** | Inline strip with SQLSTATE and Retry |
| **Stale** | Footer shows the stats reset time and the metadata cache age |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Enter` | Show full definition |
| `Ctrl+C` | Copy the definition |
| `Delete` | Drop (safety ladder; `TypeToConfirm` for a used index on Production) |
| `Ctrl+Shift+A` | Create (index / constraint / trigger, per tab) |
| `F5` | Refresh |
| `Enter` on a relation | Open the related table document |

---

## 7. Data contract

```csharp
public interface IIndexAnalyzer
{
    IReadOnlyList<IndexFinding> Analyze(
        TableDescriptor table, IndexStatisticsContext context);
}

public sealed record IndexStatisticsContext(
    DateTimeOffset? StatsResetAt, bool StatsAvailable, bool EverAnalyzed);

public sealed record IndexFinding(
    string IndexName, IndexFindingKind Kind, FindingSeverity Severity,
    string Message, string? Recommendation, string? SuggestedSql, long? ReclaimableBytes);

public enum IndexFindingKind { Unused, Duplicate, Redundant, Invalid, Bloated, NotAnalyzed }

public interface IDdlGenerator
{
    Result<string> CreateIndex(CreateIndexSpec spec);
    Result<string> DropIndex(DbObjectRef index, bool concurrently, bool cascade);
    Result<string> AddConstraint(AddConstraintSpec spec);
    Result<string> DropConstraint(DbObjectRef table, string name, bool cascade);
}
```

`IIndexAnalyzer` lives in `Basora.Analytics`, is **pure**, and takes statistics as input
— so it is fully unit-testable with fixture data and needs no database.

---

## 8. Acceptance criteria

- [ ] Indexes show method, columns (in order), uniqueness, size and scan count.
- [ ] Partial, expression and `INCLUDE` indexes render their full definition correctly.
- [ ] Unused, duplicate, redundant and invalid findings are detected accurately.
- [ ] Every scan-count-based finding shows the stats reset time.
- [ ] When statistics are unavailable, counts show `-` and dependent findings are
      suppressed, never guessed.
- [ ] No index is ever dropped automatically; every recommendation requires user action.
- [ ] Create index defaults to `CONCURRENTLY` with the trade-off explained.
- [ ] FK creation offers `NOT VALID` + `VALIDATE` as the production-safe path.
- [ ] `NOT VALID` constraints are marked and offer Validate.
- [ ] Relations show both directions with cardinality and referential actions, and
      navigate on click.
- [ ] Internal FK triggers are hidden from the trigger list.
- [ ] Drop routes through the safety ladder.
- [ ] All seven states render in both themes.
