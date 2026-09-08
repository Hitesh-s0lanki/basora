# Epic 04 — SQL Editor and Query Execution

Spec: [wf-08](../wireframes/wf-08-sql-editor.md), [wf-09](../wireframes/wf-09-result-viewer.md),
[05-postgresql-data-layer](../05-postgresql-data-layer.md) sections 4–5,
[03-tech-stack](../03-tech-stack.md) section 5.

---

### T-Q01 — SQL lexer and statement splitter

**Wave:** 2 | **Size:** M | **Spec:** [03-tech-stack](../03-tech-stack.md) section 5, [05-postgresql](../05-postgresql-data-layer.md) section 4

**Depends on:** T-F06
**Blocks:** T-Q02, T-Q03, T-Q04, T-P01

> The foundation for completion, formatting, statement splitting and the entire safety
> engine. Small in code, high in consequence — get it right.

**Owns:** `src/Basora.Core/Sql/Lexer/**`, `tests/Basora.Core.Tests/Sql/Lexer/**`

**Deliverables**
- A hand-written tokenizer handling: identifiers (quoted and unquoted), string literals
  with doubled quotes, `E'...'` escape strings, `U&'...'` unicode strings, dollar-quoted
  bodies (`$$` and `$tag$`), line comments, nested block comments, numbers, operators,
  parameters (`$1`, `:named`), and PostgreSQL-specific operators (`::`, `->`, `->>`,
  `#>`, `@>`, `||`).
- `SplitStatements` and `StatementAt(caretOffset)`.
- A shallow parser for the statement head sufficient to identify kind, target objects,
  `FROM` relations and aliases.

**Acceptance**
- [ ] A `plpgsql` function body containing semicolons, comments and nested dollar quotes
      splits as **one** statement.
- [ ] `-- DROP TABLE x` and `'DROP TABLE x'` are not classified as drops.
- [ ] Nested block comments terminate correctly.
- [ ] `StatementAt` returns the correct statement for every caret position in a
      multi-statement document, including whitespace between statements.
- [ ] Alias resolution is correct for `FROM a x JOIN b y ON ...`.
- [ ] Fuzzing with random input never throws and always terminates.

---

### T-Q02 — Context-aware completion provider

**Wave:** 4 | **Size:** L | **Spec:** [wf-08](../wireframes/wf-08-sql-editor.md) section 4

**Depends on:** T-Q01, T-E02
**Blocks:** T-Q06

**Owns:** `src/Basora.Core/Sql/Completion/**`,
`src/Basora.PostgreSQL/Queries/CompletionSource*`,
`tests/Basora.Core.Tests/Sql/Completion/**`

**Deliverables**
- Context detection per the wf-08 table: after `FROM`/`JOIN`, after clause keywords,
  after `alias.`, after `schema.`, inside a function call, after `::`, at statement start.
- Ranking: exact prefix, relations in the statement, recency, frequency, alphabetical.
- Fuzzy subsequence matching with match indices for highlighting.
- Served from the metadata cache with a non-blocking background refresh.

**Acceptance**
- [ ] After `FROM`, only relations are offered; after `alias.`, only that relation's
      columns.
- [ ] Aliases are resolved correctly, including in multi-join statements and subqueries
      one level deep.
- [ ] Completion returns in under 100 ms from a warm cache (measured against a
      5,000-table fake).
- [ ] A cache miss does not block typing; results update in place.
- [ ] Ranking is verified by fixture tests, not by eyeballing.

---

### T-Q03 — SQL formatter

**Wave:** 3 | **Size:** M | **Spec:** [wf-08](../wireframes/wf-08-sql-editor.md), [idea.md](../idea.md) section 17

**Depends on:** T-Q01
**Blocks:** T-Q06

**Owns:** `src/Basora.Core/Sql/Formatting/**`,
`tests/Basora.Core.Tests/Sql/Formatting/**`

**Deliverables**
- Token-stream formatter over the lexer: clause-per-line, configurable indent, keyword
  case, comma placement, alignment of `SELECT` lists and `JOIN` conditions.
- Minify mode.
- Options bound to the settings catalog.

**Acceptance**
- [ ] **Idempotent** — formatting twice equals formatting once, asserted over a corpus.
- [ ] Comments are preserved with their position and content.
- [ ] String literals and dollar-quoted bodies are never reformatted internally.
- [ ] A 2,000-line document formats in under 200 ms.
- [ ] Golden-file tests cover CTEs, window functions, nested subqueries, `CASE`, and DDL.

---

### T-Q04 — Query executor, streaming and cancellation

**Wave:** 3 | **Size:** L | **Spec:** [05-postgresql](../05-postgresql-data-layer.md) section 4

**Depends on:** T-C01, T-F06
**Blocks:** T-D02, T-Q06, T-Q07

**Owns:** `src/Basora.PostgreSQL/Queries/QueryExecutor*`,
`src/Basora.PostgreSQL/Queries/ResultReader*`,
`tests/Basora.PostgreSQL.Tests/Queries/**`

**Deliverables**
- `IAsyncEnumerable<ResultChunk>` streaming with adaptive chunk sizes (200 first, growing
  to 2,000).
- Row cap and memory ceiling with `IsTruncated` reporting.
- Cancellation that returns to the UI immediately and discards the connection rather than
  reusing it.
- Multi-result-set handling via `NextResultAsync`.
- Notice capture (`RAISE NOTICE`) surfaced per result set.
- Three timing measurements: total elapsed, server execution, rows fetched.

**Acceptance**
- [ ] First chunk is emitted as soon as available; time-to-first-row under 500 ms.
- [ ] 1M rows stream with flat memory and no UI thread involvement.
- [ ] Cancellation is acknowledged in under 100 ms and the connection is not reused.
- [ ] Row cap and memory ceiling both trigger truncation correctly.
- [ ] `RAISE NOTICE` output is captured and attributed to the right statement.
- [ ] A multi-statement batch produces one result set per statement in order.

---

### T-Q05 — Error mapping and caret positioning

**Wave:** 4 | **Size:** M | **Spec:** [05-postgresql](../05-postgresql-data-layer.md) section 10

**Depends on:** T-Q01, T-F03
**Blocks:** T-Q06

**Owns:** `src/Basora.PostgreSQL/Errors/**`, `tests/Basora.PostgreSQL.Tests/Errors/**`

**Deliverables**
- `PostgresException` to `Error` mapping using `SqlStateCatalog`.
- Position mapping: the exception's 1-based **byte** offset into the statement, converted
  to a UTF-16 caret offset in the full document, accounting for the statement's offset.
- Near-name suggestions for `42P01` / `42703` from the metadata cache.
- Constraint and column extraction from `IncludeErrorDetail` fields for `23xxx` errors.

**Acceptance**
- [ ] A syntax error in the third statement of a document highlights the correct position,
      verified with multi-byte UTF-8 content before the error.
- [ ] `42501` produces the exact `GRANT` statement that would fix it.
- [ ] `42P01` suggests the closest existing relation names.
- [ ] `23505` names the constraint and the conflicting values.
- [ ] `57014` is rendered as "Cancelled", not as an error.

---

### T-Q06 — SQL editor screen

**Wave:** 4 | **Size:** L | **Spec:** [wf-08](../wireframes/wf-08-sql-editor.md)

**Depends on:** T-Q01, T-Q02, T-Q03, T-Q04, T-Q05, T-U03, T-U04, T-U05
**Blocks:** T-Q07, T-Q08

**Owns:** `src/Basora.UI/Views/Editor/**`, `src/Basora.UI/ViewModels/Editor/**`,
`src/Basora.UI/Controls/SqlEditor/**`, `tests/Basora.UI.Tests/Editor/**`

**Deliverables**
- AvaloniaEdit host with PostgreSQL highlighting derived from design tokens, folding,
  multi-cursor, find/replace, bracket matching including dollar quotes.
- Current-statement gutter marker.
- Completion popup wired to `ICompletionProvider`.
- Toolbar, parameter bar, status strip, Messages tab.
- Document persistence so unsaved text survives a force-kill.

**Acceptance** — the full checklist in [wf-08 section 8](../wireframes/wf-08-sql-editor.md).
Hard gates: the editor stays editable while a query runs, and `Escape` closes completion
before cancelling the query.

---

### T-Q07 — Result viewer

**Wave:** 5 | **Size:** L | **Spec:** [wf-09](../wireframes/wf-09-result-viewer.md)

**Depends on:** T-Q06, T-D01, T-Q04
**Blocks:** nothing

**Owns:** `src/Basora.UI/Views/Results/**`, `src/Basora.UI/ViewModels/Results/**`,
`src/Basora.Core/Services/Results/ResultBuffer*`,
`tests/Basora.UI.Tests/Results/**`

**Deliverables**
- `IResultBuffer` — a windowed store spilling above a threshold to a temp file.
- Grid, JSON, Text and (stub for MVP 2) Chart view modes.
- Result-set tabs, non-row result cards, truncation banner, search, client-side filter
  clearly distinguished from a server `WHERE`.
- Editable results when the set resolves to a single keyed table.

**Acceptance** — the full checklist in [wf-09 section 8](../wireframes/wf-09-result-viewer.md).
Hard gate: sorting a truncated result warns and offers a server-side re-run rather than
silently sorting partial data.

---

### T-Q08 — Transaction scope and controls

**Wave:** 5 | **Size:** M | **Spec:** [05-postgresql](../05-postgresql-data-layer.md) section 5

**Depends on:** T-Q06, T-C01
**Blocks:** nothing

**Owns:** `src/Basora.PostgreSQL/Queries/TransactionScope*`,
`src/Basora.UI/ViewModels/Editor/TransactionViewModel*`,
`tests/Basora.PostgreSQL.Tests/Transactions/**`

**Deliverables**
- `ITransactionScope` pinning one connection, with begin, commit, rollback, savepoint and
  rollback-to.
- Status-bar integration: live elapsed time, statement count, and a warning tint past the
  age threshold.
- Prompt on tab close with an open transaction; never a silent commit.
- Statement log for the transaction inspector.

**Acceptance**
- [ ] An open transaction pins its connection and the channel reports it unavailable.
- [ ] Elapsed time and statement count are live in the status bar.
- [ ] Closing a tab with an open transaction prompts and defaults to rollback.
- [ ] Savepoints work, including rollback-to followed by further statements.
- [ ] A transaction older than the threshold shows the idle-in-transaction warning.
