# 08 — UX Flows

End-to-end journeys that cross screen boundaries. Each wireframe doc describes one
screen in isolation; this document is how they connect. When a flow and a wireframe
disagree, **the flow wins** — it represents the user's actual path.

Notation: `[Screen]` refers to a wireframe doc, `(action)` is a user action,
`-> ` is a transition.

---

## Flow 1 — First run to first query

The single most important flow in the product. A new user must reach a result set in
under two minutes without reading anything.

```text
Launch (no connections exist)
  -> [wf-01 Connection Manager] in first-run state
       Empty state offers three paths, in this priority order:
         1. "Paste a connection URI"      <- fastest, most common
         2. "New connection"              <- manual form
         3. "Import from..."              <- pgpass / TablePlus / DBeaver / .pgpass
  ->
(paste postgresql://user:pass@host:5432/db)
  -> [wf-02 Connection Editor] pre-filled from the URI
       Password extracted to ISecretStore immediately; the field shows dots, and a note
       says where it was stored and which backend is in use.
       Environment is guessed from the hostname (localhost -> Local, contains
       "prod" -> Production) and the guess is clearly marked as a guess.
  ->
(Test Connection)
  -> Inline result, never a dialog:
       success -> server version, current role, latency, SSL state, list of databases
       failure -> mapped error (06 s.10) + the specific next step
  ->
(Connect)
  -> [wf-03 App Shell] opens a connection tab
       Object explorer begins loading on the metadata channel
       A new empty SQL document opens focused, cursor ready
  ->
(type a query, Ctrl+Enter)
  -> [wf-09 Result Viewer] first rows visible in under 500 ms
```

**Failure branches that must be designed, not left to chance:**

- Wrong password -> inline re-prompt on the same screen, connection form state preserved.
- Host unreachable -> distinguish DNS failure, refused, and timeout; each has a
  different next step.
- SSL required by server, disabled in profile -> offer the one-click fix.
- Database does not exist -> list the ones that do and offer to switch.

---

## Flow 2 — Browse, edit, commit

```text
[wf-04 Object Explorer]
(type to filter tree, or Ctrl+P -> [wf-15 Open Anything])
  -> select table "users"
  -> [wf-05 Table Data Grid] opens as a document tab
       Loads first page immediately; row count arrives asynchronously as an estimate,
       refined on demand.
  ->
(edit a cell inline)
  -> Cell paints as data.modified. Pending-change count appears on the tab badge and
     in the status bar. NOTHING has touched the database yet.
  ->
(edit two more cells, delete a row, insert a row)
  -> Pending count = 4
  ->
(Ctrl+Shift+P  Preview SQL)
  -> [wf-07 Pending Changes] shows the generated statements, in order, wrapped in
     BEGIN/COMMIT, with each statement mapped back to the row it came from
       (click a statement -> the grid scrolls to and highlights that row)
  ->
(Commit, Ctrl+S)
  -> Safety engine evaluates (10-safety-rules.md)
       Local      -> executes
       Staging    -> confirm dialog
       Production -> [wf-16 Safety Dialog], type-to-confirm for anything unbounded
  ->
  -> Executes in one transaction
       success -> toast with per-statement affected rows; grid refreshes affected rows
                  only, preserving scroll and selection
       failure -> whole transaction rolled back; the failing statement is highlighted
                  in the preview with its error; ALL pending changes are preserved so
                  the user can fix and retry
```

**Non-negotiable:** a failed commit never loses the user's pending edits.

---

## Flow 3 — Write, run, understand, save

```text
[wf-08 SQL Editor]
(type "SELECT * FROM us")
  -> Autocomplete: tables matching, ranked by schema relevance and recent use
(Tab to accept -> "users")
(type " WHERE ")
  -> Autocomplete now offers columns of users, because the editor tracks FROM context
  ->
(Ctrl+Enter)
  -> Statement under the cursor executes (not the whole document)
  -> [wf-09 Result Viewer] streams rows; status bar shows a live row counter and a
     Cancel button
  ->
  -> Query completes: duration, rows, and (if plan capture is on) a one-line verdict:
       "Seq Scan on orders, 4.2M rows scanned to return 14"   <- MVP 2
  ->
(click the verdict)
  -> [wf-19 Visual Explain] with the expensive node pre-selected
  ->
(the plan suggests an index)
  -> [wf-18 Migration Generator] with the CREATE INDEX CONCURRENTLY pre-filled
  -> Migration Safety Analyzer runs before anything is offered for execution
  ->
Back in the editor:
(Ctrl+D)  -> save to [wf-10 Favorites], folder picker, name defaulted from the query
```

---

## Flow 4 — Import a CSV

```text
[wf-04 Object Explorer] -> right-click table -> Import Data
  or  File -> Import
  -> [wf-13 Import Wizard]

Step 1  Source        pick file; detect delimiter, encoding, quoting, header row
                      show the raw first 20 lines so the user can confirm the detection
Step 2  Target        existing table, or "create new table from file"
                      (the latter infers types and shows the CREATE TABLE for review)
Step 3  Mapping       CSV column -> table column, auto-matched by name (case/underscore
                      insensitive), unmatched columns explicitly marked skip or default
Step 4  Validate      dry-run type conversion over the whole file (streamed, cancellable)
                      errors listed as: line number, column, offending value, reason
                      user chooses: abort / skip bad rows / stop at first error
Step 5  Options       transaction (default on), truncate first (off, requires confirm),
                      on-conflict behaviour, batch size, identity handling
Step 6  Preview       the first 100 rows as they will land, plus the COPY statement
Step 7  Import        progress by rows, cancellable, cancel = full rollback
        -> summary: rows imported, rows skipped, duration, link to the audit entry
```

**Design note:** step 4 exists because "22P02 invalid input syntax for type integer" on
row 40,000 of an import that then rolls back is the worst experience in every existing
tool. Validate before writing.

---

## Flow 5 — Dangerous operation in production

```text
[wf-08 SQL Editor] on a Production-tagged connection
(type "DELETE FROM users;" then Ctrl+Enter)
  ->
Safety engine (before anything reaches the server):
   1. Classify statement          -> Delete
   2. Detect missing WHERE        -> finding: unbounded.delete, Critical
   3. Estimate affected rows      -> EXPLAIN (no ANALYZE) on the equivalent SELECT
                                     -> ~18,421,932 rows
   4. Apply environment policy    -> Production + Critical = TypeToConfirm
  ->
[wf-16 Safety Dialog]
   Shows: environment, database, table, operation, estimated rows, the exact statement.
   Requires typing:  DELETE users
   Offers, in this order:
     [Cancel]  (default, focused)
     [Add a WHERE clause]        <- returns to editor with cursor placed
     [Run inside a transaction]  <- executes with BEGIN, leaves it open for review
     [Continue anyway]
  ->
(Continue anyway)
  -> Executes. Audit entry written with ConfirmationBypassed = false and the assessed
     risk recorded.
  -> Result banner stays visible until dismissed, showing affected rows and offering
     ROLLBACK if a transaction is still open.
```

**Rule:** the destructive option is never the default focus, never the primary-styled
button, and never reachable by pressing Enter.

---

## Flow 6 — Production is slow, diagnose it (MVP 2)

The flagship story. This is what Basora is for.

```text
(open Basora, select Production)
  -> [wf-20 Health Dashboard] loads automatically for Production connections
       Connections 72/200 · Cache hit 98.2% · DB size 182 GB · Active 8 · Waiting 3
       Findings: 3 slow queries · 2 blocking · 7 unused indexes · 4 bloated tables
  ->
(click "2 blocking")
  -> [wf-21 Activity and Locks] with the blocking tree expanded
       PID 8211 blocks 8221 blocks 8232
       Each node: user, duration, wait event, and the statement
  ->
(the root blocker is an idle-in-transaction session held for 40 minutes)
  -> Actions: Copy query · Explain · Cancel backend · Terminate backend
     Terminate requires confirmation and is audited.
  ->
Separately, (click "3 slow queries")
  -> [wf-22 Slow Queries] from pg_stat_statements, ranked by total time (not mean —
     a 2 ms query called 40M times is the real problem)
  ->
(select one -> Explain)
  -> [wf-19 Visual Explain] heat-mapped by self-time
       Finding: "Seq Scan on orders, filter customer_id, 4.2M rows removed by filter"
  ->
(Suggest fix)
  -> "CREATE INDEX CONCURRENTLY idx_orders_customer_id ON orders (customer_id)"
  -> [wf-18 Migration] + Safety Analyzer:
       orders: 428M rows, 182 GB, 14 existing indexes
       CONCURRENTLY -> ShareUpdateExclusive, does not block writes
       Estimated build time and the warning that it may fail and leave an INVALID index
  ->
(Apply)  -> executes with progress
  -> Offer: re-run the original query and compare -> [wf-19] side-by-side
       2.41 s -> 184 ms
```

Every step in this chain is a click from the previous one. If a user has to go find the
next screen themselves, the flow has failed.

---

## Flow 7 — Change a table's structure

```text
[wf-05 Table Document] -> Structure tab -> [wf-11 Structure Editor]
(add column "phone varchar(20)", set nullable, add a default)
  -> Nothing is applied. The editor accumulates a structure change set, exactly like
     the data grid accumulates row changes.
  ->
(Preview SQL)
  -> ALTER TABLE public."users" ADD COLUMN "phone" varchar(20);
  -> Migration Safety Analyzer runs here too:
       table size, row estimate, lock level acquired by each statement, and whether the
       operation rewrites the table (adding a nullable column with no default does not;
       adding NOT NULL with a volatile default does)
  ->
(Apply) -> safety ladder -> execute -> metadata cache invalidated -> tree and open
           documents for this table refresh
```

---

## Flow 8 — Recover from a crash or restart

```text
Launch (workspace state exists)
  -> Restore: connection tabs, document tabs, editor text (including unsaved), scroll
     positions, active filters, expanded tree nodes.
  -> Connections are NOT auto-opened for Production-tagged profiles; they show as
     "click to reconnect". Auto-connecting to production on launch is a footgun.
  -> Pending data changes are NOT restored. On the previous exit the user was warned;
     on restore the tab shows a note explaining they were discarded.
```

---

## Flow 9 — Work across environments

```text
Connection tabs:  [Production] [Staging] [Local]
  Each tab carries its environment colour on its top border.
  The status bar always shows the active connection, environment and database.

(run the same query in two tabs)
  -> Results are independent; nothing is shared between connection tabs except
     favorites and history (history records which connection each entry came from and
     can be filtered by it).

(MVP 3) (Compare -> Schema Diff)
  -> [wf-18] Production vs Staging
     Tables/columns/indexes/constraints added, removed, modified
     -> generated reconciliation SQL, direction-selectable, never auto-applied
```

---

## Cross-cutting interaction rules

1. **Ctrl+Enter always means "run the thing I am looking at."** Statement under cursor
   in the editor; refresh in a grid; apply in a wizard step.
2. **Escape always cancels the innermost thing** — completion popup, then dialog, then
   running query. It never closes a tab or loses work.
3. **Nothing modal that can be inline.** Test-connection results, validation errors and
   query errors are inline. Modals are reserved for decisions with consequences.
4. **Every long operation is cancellable and shows what it is doing**, naming the object
   ("loading columns for public.orders"), not just "Loading...".
5. **Navigation is never a dead end.** Every finding links to the thing it is about;
   every object links to the things that reference it.
6. **The user's text is sacred.** Editor content survives crashes, disconnects, failed
   commits and accidental tab closes (closed tabs are restorable with Ctrl+Shift+T).
