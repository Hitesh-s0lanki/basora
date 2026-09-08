# 00 — Master Task Board

Read [`README.md`](README.md) first for the ID scheme and the parallelism rules.

---

## 1. Waves at a glance

```text
WAVE 0  SERIAL - everything is blocked until this lands
        T-F01 -> T-F02
        2 tasks, ~2 days, 1 person

WAVE 1  CONTRACTS - 7 tracks in parallel
        T-F03  T-F04  T-F05  T-F06  T-F07  T-U01  T-F09
        7 tasks, ~3 days

WAVE 2  FOUNDATIONS - 12 tracks in parallel
        T-F08  T-F10  T-U02  T-U05  T-S01  T-S02  T-S04
        T-C01  T-C02  T-Q01  T-P01  T-M02  T-D01
        13 tasks, ~5 days

WAVE 3  SERVICES + SCREENS - 20 tracks in parallel  <- the big fan-out
        T-U03 T-U04 T-U07 T-S03 T-S05 T-C03 T-C04 T-C07
        T-E01 T-E02 T-M01 T-D02 T-Q03 T-Q04 T-P02 T-P05
        T-H01 T-H02 T-X01 T-X02
        20 tasks, ~8 days

WAVE 4  FEATURE SCREENS - 14 tracks in parallel
        T-C05 T-C06 T-E03 T-E04 T-M03 T-M05 T-D03 T-D04
        T-Q02 T-Q05 T-Q06 T-P03 T-P04 T-H03
        14 tasks, ~8 days

WAVE 5  COMPOSITION - 9 tracks in parallel
        T-D05 T-D06 T-D07 T-D08 T-M04 T-M06 T-Q07 T-Q08
        T-U06 T-U08 T-X03 T-X04 T-X05
        13 tasks, ~6 days

WAVE 6  MVP-1 HARDENING
        T-Z01..T-Z05  performance, a11y, packaging, docs, release candidate
```

MVP 2 (`T-V*`, `T-N*`) and MVP 3 (`T-AI*`, `T-G*`) are epic-level only until MVP 1
ships. See [`epic-09-mvp2-mvp3-backlog.md`](epic-09-mvp2-mvp3-backlog.md).

---

## 2. Dependency graph (MVP 1)

```text
T-F01 solution restructure
  |
T-F02 build infrastructure
  |
  +--------+--------+--------+--------+--------+--------+
  |        |        |        |        |        |        |
T-F03    T-F04    T-F05    T-F06    T-F07    T-U01    T-F09
results  conn     meta     query    explain  tokens   CI
  |      models   models   models   +safety    |
  |        |        |        |        |        |
  +--------+--------+--------+--------+        |
                    |                          |
      +-------------+-------------+            +----------+
      |             |             |                       |
    T-F08         T-F10         T-U05                    T-U02
    TestKit       infra         keymap                   styles
      |             |             |                       |
      +------+------+------+------+-----------------------+
             |      |      |      |      |      |      |
           T-S01  T-C01  T-Q01  T-P01  T-M02  T-D01  T-S02
           secrets session lexer classif types  grid   vault
             |      |      |      |      |      |
    +--------+------+------+------+------+------+---------------+
    |        |      |      |      |      |      |      |        |
  T-S03    T-C03  T-E01  T-Q04  T-P02  T-M01  T-D02  T-X01   T-U03
  tunnel   registry cat.  exec   engine descr  data   export  shell
    |        |      |      |      |      |      |      |        |
    +--------+------+------+------+------+------+------+--------+
                                  |
    +-----------------------------+-----------------------------+
    |        |        |        |        |        |        |     |
  T-C05    T-C06    T-E03    T-M03    T-D03    T-Q06    T-P03  T-H03
  conn mgr conn ed  tree     structure table   editor   dialogs history
    |        |        |        |        |        |        |     |
    +--------+--------+--------+--------+--------+--------+-----+
                                  |
              +-------------------+-------------------+
              |         |         |         |         |
            T-D05     T-D06     T-D07     T-M04     T-Q07
            changes   preview   filters   DDL edit  results
              |         |         |         |         |
              +---------+---------+---------+---------+
                                  |
                              T-Z01..05
                              hardening
```

---

## 3. Critical path

```text
T-F01 -> T-F02 -> T-F06 -> T-F08 -> T-D01 -> T-D02 -> T-D03 -> T-D05 -> T-D06 -> T-Z01
```

The data-grid chain is the longest. Two consequences worth acting on:

1. **Start `T-D01` (the virtualised grid control) as early as wave 2**, before the
   services it will consume exist. It is built against a fake row source, so it can.
2. If `T-D01` slips, everything else still proceeds — but MVP 1 does not ship. It is the
   task to staff first and watch hardest.

The safety chain (`T-P01 -> T-P02 -> T-P03`) is the second-longest and is a hard
prerequisite for shipping *anything* that writes, so it cannot be deferred to the end.

---

## 4. Full MVP-1 task list

| ID | Title | Wave | Size | Epic |
|---|---|---|---|---|
| T-F01 | Solution restructure to C# multi-project | 0 | M | [00](epic-00-foundation.md) |
| T-F02 | Build infrastructure and architecture tests | 0 | M | [00](epic-00-foundation.md) |
| T-F03 | Result, Error and the error catalogue | 1 | S | [00](epic-00-foundation.md) |
| T-F04 | Connection domain models and contracts | 1 | S | [00](epic-00-foundation.md) |
| T-F05 | Metadata domain models and contracts | 1 | M | [00](epic-00-foundation.md) |
| T-F06 | Query, filter and change-set models | 1 | M | [00](epic-00-foundation.md) |
| T-F07 | Explain, safety and health models | 1 | S | [00](epic-00-foundation.md) |
| T-F08 | Basora.TestKit — fakes for every contract | 2 | M | [00](epic-00-foundation.md) |
| T-F09 | CI pipeline and packaging skeleton | 1 | M | [00](epic-00-foundation.md) |
| T-F10 | Infrastructure: settings, logging, workspace | 2 | M | [00](epic-00-foundation.md) |
| T-U01 | Design tokens and theme dictionaries | 1 | M | [07](epic-07-shell-ux.md) |
| T-U02 | Control styles and theme completeness test | 2 | M | [07](epic-07-shell-ux.md) |
| T-U03 | Application shell | 3 | L | [07](epic-07-shell-ux.md) |
| T-U04 | Document host, tabs and workspace restore | 3 | M | [07](epic-07-shell-ux.md) |
| T-U05 | Command registry and keymap infrastructure | 2 | M | [07](epic-07-shell-ux.md) |
| T-U06 | Command palette and Open Anything | 5 | M | [07](epic-07-shell-ux.md) |
| T-U07 | Dialog, toast and notification services | 3 | S | [07](epic-07-shell-ux.md) |
| T-U08 | Settings catalog and settings screen | 5 | M | [07](epic-07-shell-ux.md) |
| T-S01 | ISecretStore and OS keystore backends | 2 | L | [01](epic-01-connection.md) |
| T-S02 | Encrypted fallback vault | 2 | M | [01](epic-01-connection.md) |
| T-S03 | SSH tunnel manager | 3 | L | [01](epic-01-connection.md) |
| T-S04 | Log redaction and enrichers | 2 | S | [01](epic-01-connection.md) |
| T-S05 | Audit log | 3 | M | [01](epic-01-connection.md) |
| T-C01 | Session, channels and capability probe | 2 | L | [01](epic-01-connection.md) |
| T-C02 | Connection URI and libpq parser | 2 | S | [01](epic-01-connection.md) |
| T-C03 | Connection registry and persistence | 3 | M | [01](epic-01-connection.md) |
| T-C04 | Connection tester | 3 | S | [01](epic-01-connection.md) |
| T-C05 | Connection manager screen | 4 | M | [01](epic-01-connection.md) |
| T-C06 | Connection editor screen | 4 | L | [01](epic-01-connection.md) |
| T-C07 | Connection importers | 3 | M | [01](epic-01-connection.md) |
| T-E01 | Catalog query library | 3 | L | [02](epic-02-explorer.md) |
| T-E02 | Metadata service and cache | 3 | M | [02](epic-02-explorer.md) |
| T-E03 | Object explorer tree | 4 | L | [02](epic-02-explorer.md) |
| T-E04 | Server-side object search | 4 | M | [02](epic-02-explorer.md) |
| T-M01 | Table descriptor assembly | 3 | M | [02](epic-02-explorer.md) |
| T-M02 | Type catalog and PgType mapping | 2 | M | [02](epic-02-explorer.md) |
| T-M03 | Structure viewer | 4 | M | [02](epic-02-explorer.md) |
| T-M04 | Structure editor and DDL generator | 5 | L | [02](epic-02-explorer.md) |
| T-M05 | Indexes, constraints, relations, triggers | 4 | M | [02](epic-02-explorer.md) |
| T-M06 | Index analyzer rules | 5 | M | [02](epic-02-explorer.md) |
| T-D01 | Virtualised data grid control | 2 | L | [03](epic-03-data-grid.md) |
| T-D02 | Table data service and pagination | 3 | L | [03](epic-03-data-grid.md) |
| T-D03 | Table document | 4 | L | [03](epic-03-data-grid.md) |
| T-D04 | Typed cell editors | 4 | L | [03](epic-03-data-grid.md) |
| T-D05 | Change set service and row identity | 5 | L | [03](epic-03-data-grid.md) |
| T-D06 | Change SQL generator and preview screen | 5 | L | [03](epic-03-data-grid.md) |
| T-D07 | Filter AST, SQL builder and builder screen | 5 | L | [03](epic-03-data-grid.md) |
| T-D08 | Value inspector, copy and paste | 5 | M | [03](epic-03-data-grid.md) |
| T-Q01 | SQL lexer and statement splitter | 2 | M | [04](epic-04-sql-editor.md) |
| T-Q02 | Context-aware completion provider | 4 | L | [04](epic-04-sql-editor.md) |
| T-Q03 | SQL formatter | 3 | M | [04](epic-04-sql-editor.md) |
| T-Q04 | Query executor, streaming and cancellation | 3 | L | [04](epic-04-sql-editor.md) |
| T-Q05 | Error mapping and caret positioning | 4 | M | [04](epic-04-sql-editor.md) |
| T-Q06 | SQL editor screen | 4 | L | [04](epic-04-sql-editor.md) |
| T-Q07 | Result viewer | 5 | L | [04](epic-04-sql-editor.md) |
| T-Q08 | Transaction scope and controls | 5 | M | [04](epic-04-sql-editor.md) |
| T-H01 | Query history store | 3 | M | [05](epic-05-history-io.md) |
| T-H02 | Favorites store | 3 | S | [05](epic-05-history-io.md) |
| T-H03 | History and favorites panels | 4 | M | [05](epic-05-history-io.md) |
| T-X01 | Export service and format writers | 3 | L | [05](epic-05-history-io.md) |
| T-X02 | Import detection and validation | 3 | L | [05](epic-05-history-io.md) |
| T-X03 | Import wizard screen | 5 | L | [05](epic-05-history-io.md) |
| T-X04 | COPY import execution | 5 | M | [05](epic-05-history-io.md) |
| T-X05 | Export dialog screen | 5 | M | [05](epic-05-history-io.md) |
| T-P01 | Statement classifier | 2 | M | [06](epic-06-safety.md) |
| T-P02 | Safety rule engine | 3 | L | [06](epic-06-safety.md) |
| T-P03 | Safety and confirmation dialogs | 4 | M | [06](epic-06-safety.md) |
| T-P04 | Safe mode and read-only enforcement | 4 | M | [06](epic-06-safety.md) |
| T-P05 | Migration impact analyzer | 3 | M | [06](epic-06-safety.md) |
| T-Z01 | Performance budgets and profiling | 6 | M | [08](epic-08-hardening.md) |
| T-Z02 | Accessibility and keyboard audit | 6 | M | [08](epic-08-hardening.md) |
| T-Z03 | Cross-platform packaging and signing | 6 | L | [08](epic-08-hardening.md) |
| T-Z04 | Error-path and resilience sweep | 6 | M | [08](epic-08-hardening.md) |
| T-Z05 | Release candidate and manual test pass | 6 | M | [08](epic-08-hardening.md) |

**73 tasks for MVP 1.**

---

## 5. Suggested staffing

The plan is written so that team size changes throughput but not structure.

| Team size | Approach |
|---|---|
| **1** | Follow waves strictly; within a wave, do the critical-path task first (`T-D*` chain), then whatever unblocks the most |
| **2–3** | One owns the grid chain end to end, one owns query/editor, one owns connections + safety. Shell and themes are shared early, then stable |
| **4–6** | One track per epic letter. Wave 3 and 4 saturate at about six people |
| **Agent fan-out** | Wave 3 supports ~20 concurrent workers, wave 4 ~14, wave 5 ~13. Beyond that, tasks contend for the same files |

The ceiling on parallelism is not the plan — it is wave 0 and wave 1, which are narrow
by nature. Getting through them quickly is worth disproportionate effort.

---

## 6. What ships when

| Milestone | Tasks complete | You can |
|---|---|---|
| **Walking skeleton** | Waves 0–2 | Build, test, and see a themed empty shell |
| **First connection** | + T-C01, T-C03, T-C05, T-C06, T-U03 | Connect to a real database |
| **First read** | + T-E01, T-E02, T-E03, T-D01, T-D02, T-D03 | Browse and read data |
| **First query** | + T-Q01, T-Q04, T-Q06, T-Q07 | Write and run SQL |
| **First safe write** | + T-P01, T-P02, T-P03, T-D05, T-D06 | Edit data with review |
| **MVP 1** | All of waves 0–6 | Replace an existing client |

Each of these is a genuinely demoable state, deliberately — a plan whose first
demonstrable output is the finished product is a plan nobody can course-correct.
