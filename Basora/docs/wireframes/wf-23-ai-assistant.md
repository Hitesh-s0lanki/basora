# wf-23 — AI Database Assistant

> Task: `T-AI03`. MVP 3. Side panel, dockable right, plus inline entry points.

---

## 1. Purpose

Answer questions about *this* database, using its real schema, plans and statistics. The
one job it must do well: **be grounded**. A model that guesses table names is worse than
no assistant, because it wastes the user's time and burns their trust in one exchange.

The hard product rule from `../01-product-overview.md` section 8 applies without
exception: **AI proposes, humans dispose. Generated SQL is never executed silently.**

---

## 2. Entry points

- `Ctrl+Shift+I` toggles the panel.
- "Ask about this" on a table, a query, a plan finding, or an error.
- "Explain this query" in the editor context menu.
- "Why is this slow?" on a plan finding in [wf-19](wf-19-visual-explain.md).
- Natural-language input in the SQL editor prefixed with `--?`.

---

## 3. Layout

```text
+------------------------------------------+
| AI Assistant          [context] [x] [_]  |
+------------------------------------------+
| Context: production - public schema      |
|   142 tables - EXPLAIN - statistics      |
|   [ Review what is sent ]                |
+------------------------------------------+
|                                          |
|  YOU                                     |
|  Why is this query slow?                 |
|  SELECT * FROM orders                    |
|   WHERE customer_id = 100;               |
|                                          |
|  ASSISTANT                            AI |
|  The query takes 2.41 s because          |
|  PostgreSQL is doing a sequential scan    |
|  on orders (428M rows) to return 14.     |
|                                          |
|  There is no index on orders.customer_id.|
|  The closest existing index is           |
|  idx_orders_created_at, which does not   |
|  help this predicate.                    |
|                                          |
|  +-- Suggested SQL ------------------+   |
|  | CREATE INDEX CONCURRENTLY         |   |
|  |   idx_orders_customer_id          |   |
|  |   ON orders (customer_id);        |   |
|  +-----------------------------------+   |
|  [ Insert in editor ] [ Copy ]           |
|  [ Check migration safety ]              |
|                                          |
|  ~ I have not run this. Review it first. |
|                                          |
|  Sources: EXPLAIN plan, orders schema,   |
|           pg_stat_user_indexes           |
|                                          |
+------------------------------------------+
| [ Ask about your database...        ] [>]|
| [x] Schema  [x] Plans  [ ] Sample rows   |
+------------------------------------------+
```

---

## 4. Component inventory

### Context bar — always visible, always first

States the connection, schema scope, and exactly which context categories are enabled.
**"Review what is sent"** opens a panel showing the literal payload — the schema DDL,
statistics and plan JSON — before the first request of a session. Users should be able to
verify our claims rather than trust them.

Per `../06-security-and-credentials.md` section 6: schema, plans and statistics may be
sent with opt-in. **Result rows and cell values require a separate, explicit,
per-request confirmation** and are off by default. Nothing from `ISecretStore` is ever
sent.

### Conversation

Messages alternate; assistant messages are marked with an `AI` badge in `status.ai`
purple and are visually distinct from application output throughout the product. This is
a consistency rule, not a decoration: a user must never be unsure whether a claim came
from the database or from a model.

### Generated SQL blocks

Rendered in a bordered block with syntax highlighting and three actions: **Insert in
editor**, **Copy**, and **Check migration safety** (which routes to the analyzer from
[wf-11](wf-11-structure-editor.md)). There is deliberately **no Run button**. Running is
done by the user, in the editor, through the normal safety ladder.

A standing caption states that the SQL has not been executed.

### Sources

Every answer lists which context it drew on (this table's schema, this plan, these
statistics). A claim with no source is a claim the user should distrust, and showing
sources makes that checkable.

### Tool surface

The model is given read-only tools rather than a dump of the database:

```text
list_schemas · list_tables · describe_table · list_indexes · get_table_statistics
explain_query (never ANALYZE) · get_slow_queries · get_locks · get_database_health
search_objects
```

Enforced at the tool-definition level: **no mutating tool exists**, so a prompt-injected
instruction has nothing to call. `execute_readonly_query` is deliberately excluded from
MVP 3 — it is the obvious next step and it is the one that needs the most thought.

### Presets

Quick actions that fill the input: "Explain this table", "Explain my database", "Optimise
this query", "Review this migration", "What changed recently?"

### Streaming

Responses render token by token with a stop control. A slow model must never block the
rest of the app.

---

## 5. States

| State | Rendering |
|---|---|
| **Not configured** | "Add an AI provider key in Settings" with a direct link, and a summary of what would be sent |
| **Disabled for this connection** | Explains that AI is off for Production connections by default, and how to enable it deliberately |
| **Ready** | Empty conversation with preset prompts |
| **Thinking** | Streaming indicator with a Stop control |
| **Streaming** | Partial response rendering live |
| **Complete** | Full response with sources and actions |
| **Provider error** | Rate limit, auth failure or timeout reported distinctly, each with the right remedy, and Retry |
| **Context too large** | Explains that the schema exceeds the context budget and offers to scope to specific schemas or tables |
| **Refused / uncertain** | The model's uncertainty is surfaced verbatim, not hidden behind a fabricated answer |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Ctrl+Shift+I` | Toggle the panel |
| `Enter` | Send |
| `Shift+Enter` | New line |
| `Escape` | Stop generation |
| `Ctrl+Shift+C` | Copy the last response |
| `Up` | Recall the previous prompt |

Conversations are per connection, persisted locally, and clearable. A global kill switch
in Settings disables AI everywhere immediately.

---

## 7. Data contract

```csharp
public interface IAiProvider
{
    string Name { get; }
    bool IsConfigured { get; }
    IAsyncEnumerable<AiResponseChunk> StreamAsync(
        AiConversation conversation, IReadOnlyList<AiTool> tools, CancellationToken ct);
}

public interface IDatabaseContextBuilder
{
    Task<Result<AiContext>> BuildAsync(
        IDatabaseSession session, ContextScope scope, CancellationToken ct);

    ContextPreview Preview(AiContext context);      // exactly what will be sent
}

public sealed record ContextScope(
    bool IncludeSchema, bool IncludeStatistics, bool IncludePlans,
    bool IncludeSampleRows,                          // default false, per-request opt-in
    IReadOnlyList<DbObjectRef>? FocusObjects, int TokenBudget);

public sealed record AiContext(
    string SchemaDdl, string? Statistics, string? PlanJson,
    IReadOnlyList<string> IncludedObjects, int EstimatedTokens);

public interface IAiToolRegistry
{
    IReadOnlyList<AiTool> ReadOnlyTools { get; }     // there is no mutating equivalent
}
```

`IDatabaseContextBuilder` is the security boundary and gets the most test attention: a
test asserts that a context built with `IncludeSampleRows = false` contains **no cell
value from any table**.

---

## 8. Acceptance criteria

- [ ] AI is disabled by default for Production connections and requires deliberate
      enablement.
- [ ] The context bar always states what is being sent, and "Review what is sent" shows
      the literal payload before the first request.
- [ ] Sample rows are off by default and require a per-request confirmation.
- [ ] No secret material is ever included in a context — asserted by test.
- [ ] The tool surface contains no mutating operation — asserted by test.
- [ ] There is no Run button on generated SQL anywhere in the panel.
- [ ] AI-generated content is visually distinct (`status.ai`) everywhere it appears.
- [ ] Every answer lists its sources.
- [ ] Responses stream and can be stopped; the app stays responsive throughout.
- [ ] Rate-limit, auth and timeout errors are distinguished with the right remedy.
- [ ] Context overflow offers scoping rather than silently truncating.
- [ ] A global kill switch disables AI immediately across the app.
- [ ] Conversations persist per connection and are clearable.
- [ ] All nine states render in both themes.
