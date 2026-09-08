# Epic 01 — Security and Connections

Two epic letters, one file: `T-S*` (security primitives) and `T-C*` (connections) are
tightly coupled and share reviewers.

Spec: [06-security-and-credentials](../06-security-and-credentials.md),
[05-postgresql-data-layer](../05-postgresql-data-layer.md) sections 1–2,
[wf-01](../wireframes/wf-01-connection-manager.md), [wf-02](../wireframes/wf-02-connection-editor.md).

---

### T-S01 — ISecretStore and OS keystore backends

**Wave:** 2 | **Size:** L | **Spec:** [06-security](../06-security-and-credentials.md) section 1

**Depends on:** T-F04
**Blocks:** T-C03, T-C06, T-S03

**Owns:** `src/Basora.Security/Secrets/**`, `tests/Basora.Security.Tests/Secrets/**`

**Deliverables**
- `WindowsCredentialStore` (Credential Manager via P/Invoke, `CRED_TYPE_GENERIC`).
- `MacKeychainStore`, `LinuxSecretServiceStore` (libsecret over D-Bus).
- Backend selection at startup with a health check, and `BackendName` /
  `IsHardwareBacked` reported to the UI.
- A conformance test suite runnable against any backend.

**Acceptance**
- [ ] Conformance suite passes on all three platforms: set, get, overwrite, delete,
      missing key, corrupt entry, very long value, unicode value.
- [ ] An unavailable backend reports a specific reason, never a silent downgrade.
- [ ] A secret written by Basora is visible in the OS credential UI.
- [ ] No plaintext secret is ever written to a file by this component.

---

### T-S02 — Encrypted fallback vault

**Wave:** 2 | **Size:** M | **Spec:** [06-security](../06-security-and-credentials.md) section 1

**Depends on:** T-F04
**Blocks:** T-C03

**Owns:** `src/Basora.Security/Vault/**`, `tests/Basora.Security.Tests/Vault/**`

**Deliverables**
- Argon2id (or PBKDF2-SHA256 at 600k+ iterations) key derivation from a master passphrase.
- AES-256-GCM per entry with a unique nonce and authenticated tag.
- In-memory unlock with idle auto-lock (default 30 min) and lock on OS sleep.
- Key material zeroed on lock and exit.

**Acceptance**
- [ ] A tampered vault file is detected by the auth tag and refuses to decrypt.
- [ ] Wrong passphrase fails without revealing whether the file is valid.
- [ ] Auto-lock fires on the idle timer and on sleep.
- [ ] A memory-dump test finds no plaintext passphrase after lock.

---

### T-S03 — SSH tunnel manager

**Wave:** 3 | **Size:** L | **Spec:** [06-security](../06-security-and-credentials.md) section 3

**Depends on:** T-S01, T-F04
**Blocks:** T-C01 (tunnelled connections), T-C06

**Owns:** `src/Basora.Security/Ssh/**`, `tests/Basora.Security.Tests/Ssh/**`

**Deliverables**
- Local port forwarding bound to `127.0.0.1` with an ephemeral port.
- Password, private key (with passphrase from `ISecretStore`) and ssh-agent auth.
- OpenSSH-format and ed25519 key support (verify library capability; surface the
  constraint explicitly if unsupported rather than failing at connect time).
- Host-key verification with a persisted known-hosts store and loud refusal on mismatch.
- Keepalives and lifetime tied to the session.

**Acceptance**
- [ ] The listener binds only to loopback — asserted by a test attempting an external
      connection.
- [ ] A changed host key is refused with a clear message and is not auto-accepted.
- [ ] All three auth methods work against an SSH server container.
- [ ] Closing the session closes the tunnel and frees the port.
- [ ] A dropped tunnel surfaces as a session state change, not a crash.

---

### T-S04 — Log redaction and enrichers

**Wave:** 2 | **Size:** S | **Spec:** [06-security](../06-security-and-credentials.md) section 4

**Depends on:** T-F10
**Blocks:** nothing (but is a merge gate for anything that logs)

**Owns:** `src/Basora.Security/Redaction/**`, `tests/Basora.Security.Tests/Redaction/**`

**Deliverables**
- Destructuring policies for `ConnectionProfile` and `SshTunnelConfig`.
- A regex sweep for `password=`, `sslpassword=`, `passfile=` and bearer-token shapes.
- A Serilog enricher wired into the pipeline from `T-F10`.
- A support-bundle exporter that excludes secrets and parameter values.

**Acceptance**
- [ ] A corpus of 20+ password shapes is fully redacted before reaching any sink.
- [ ] Query parameter *values* never appear in logs; names and types do.
- [ ] The support bundle contains no secret and shows its contents to the user before
      export.

---

### T-S05 — Audit log

**Wave:** 3 | **Size:** M | **Spec:** [06-security](../06-security-and-credentials.md) section 5

**Depends on:** T-F10
**Blocks:** T-P03

**Owns:** `src/Basora.Security/Audit/**`, `tests/Basora.Security.Tests/Audit/**`

**Deliverables**
- `IAuditLog` with append-only JSONL storage.
- `AuditEntry` recording every mutation, export, backend termination and Production
  connection, with the assessed risk and whether confirmation was bypassed.
- An in-app viewer and exporter; no in-app deletion.
- Retention policy with a floor for Production-tagged connections.

**Acceptance**
- [ ] Every data or schema mutation produces an entry, including failures.
- [ ] Result values are never recorded; row counts are.
- [ ] Entries cannot be deleted from within the app.
- [ ] An audit write failure is surfaced but never blocks or fails the operation.

---

### T-C01 — Session, channels and capability probe

**Wave:** 2 | **Size:** L | **Spec:** [05-postgresql](../05-postgresql-data-layer.md) sections 1–2

**Depends on:** T-F04
**Blocks:** T-E01, T-E02, T-Q04, T-C04

**Owns:** `src/Basora.PostgreSQL/Connection/**`,
`tests/Basora.PostgreSQL.Tests/Connection/**`

**Deliverables**
- `NpgsqlDataSource` per session with connection-string construction from a profile plus
  a just-in-time secret fetch.
- Three channels (`Query`, `Metadata`, `Monitor`) with distinct `application_name`
  suffixes and independent pooling budgets.
- Capability probe populating `ServerCapabilities` in one round trip where possible.
- Reconnection with backoff and state events.
- Read-only session enforcement via `SET SESSION CHARACTERISTICS AS TRANSACTION READ ONLY`.

**Acceptance**
- [ ] Integration tests pass against PostgreSQL 13, 15, 17 and latest.
- [ ] A long-running query on the query channel does not delay a metadata query.
- [ ] Capabilities correctly detect superuser, `pg_stat_statements` installed vs
      readable, and version-gated features.
- [ ] The plaintext password is not retained on any object after the data source is built.
- [ ] Read-only mode is enforced server-side, verified by an attempted write.
- [ ] A killed server produces `Reconnecting`, then recovery, without losing document
      state.

---

### T-C02 — Connection URI and libpq parser

**Wave:** 2 | **Size:** S | **Spec:** [04-domain-model](../04-domain-model.md) section 1, [wf-02](../wireframes/wf-02-connection-editor.md)

**Depends on:** T-F04
**Blocks:** T-C05, T-C06, T-C07

**Owns:** `src/Basora.Core/Services/Connections/UriParser*`,
`tests/Basora.Core.Tests/Connections/UriParser*`

**Deliverables**
- Parser for `postgresql://`, `postgres://` and libpq key/value strings.
- Extraction of the password into a separate return value, never into the profile.
- Reporting of applied fields and ignored parameters.
- Environment inference from the hostname, clearly marked as a guess.

**Acceptance**
- [ ] Handles percent-encoded passwords, IPv6 hosts in brackets, Unix socket paths,
      multi-host strings, and missing components.
- [ ] Unknown query parameters are listed and not silently applied.
- [ ] Fuzzing with malformed input never throws unhandled.
- [ ] The returned profile never contains the password.

---

### T-C03 — Connection registry and persistence

**Wave:** 3 | **Size:** M | **Spec:** [wf-01](../wireframes/wf-01-connection-manager.md) section 7

**Depends on:** T-S01, T-S02, T-F10
**Blocks:** T-C05, T-U03

**Owns:** `src/Basora.Infrastructure/Connections/**`,
`tests/Basora.Infrastructure.Tests/Connections/**`

**Deliverables**
- `IConnectionRegistry` with JSON persistence, folders and ordering.
- Duplicate semantics: a duplicated Production profile resets to `Development` and clears
  its `SecretRef`.
- Delete removes the associated secret from the store.
- Atomic write with defaults on corruption.

**Acceptance**
- [ ] The profiles file contains no secret material.
- [ ] Duplicating a Production connection resets the environment and clears the secret.
- [ ] Deleting a connection deletes its secret.
- [ ] A corrupt profiles file loads as empty without overwriting the file.

---

### T-C04 — Connection tester

**Wave:** 3 | **Size:** S | **Spec:** [wf-02](../wireframes/wf-02-connection-editor.md) section 7

**Depends on:** T-C01
**Blocks:** T-C06

**Owns:** `src/Basora.PostgreSQL/Connection/ConnectionTester*`,
`tests/Basora.PostgreSQL.Tests/ConnectionTester*`

**Deliverables**
- `IConnectionTester` returning latency, server version, current role, superuser status,
  negotiated TLS version and cipher, certificate verification outcome, tunnel state and
  database list.
- Cancellable, with a bounded overall timeout.
- Failure mapping to the `SqlStateCatalog` plus a field-level hint.

**Acceptance**
- [ ] Reports the correct `CertificateVerification` for each `sslmode` against
      containers configured accordingly.
- [ ] Distinguishes DNS failure, connection refused and timeout with different messages.
- [ ] Cancellation returns promptly and leaves no open connection.
- [ ] Never persists anything.

---

### T-C05 — Connection manager screen

**Wave:** 4 | **Size:** M | **Spec:** [wf-01](../wireframes/wf-01-connection-manager.md)

**Depends on:** T-C03, T-C02, T-U01, T-U03, T-F08
**Blocks:** nothing

**Owns:** `src/Basora.UI/Views/Connections/ConnectionManager*`,
`src/Basora.UI/ViewModels/Connections/ConnectionManager*`,
`tests/Basora.UI.Tests/Connections/ConnectionManager*`

**Deliverables** — the screen exactly as specified in wf-01.

**Acceptance** — the full checklist in [wf-01 section 8](../wireframes/wf-01-connection-manager.md).

---

### T-C06 — Connection editor screen

**Wave:** 4 | **Size:** L | **Spec:** [wf-02](../wireframes/wf-02-connection-editor.md)

**Depends on:** T-C02, T-C04, T-S01, T-S03, T-U01, T-F08
**Blocks:** nothing

**Owns:** `src/Basora.UI/Views/Connections/ConnectionEditor*`,
`src/Basora.UI/ViewModels/Connections/ConnectionEditor*`,
`tests/Basora.UI.Tests/Connections/ConnectionEditor*`

**Deliverables** — the four-tab dialog exactly as specified in wf-02, including the SSL
mode descriptions verbatim and the environment-driven safety defaults.

**Acceptance** — the full checklist in [wf-02 section 8](../wireframes/wf-02-connection-editor.md).

---

### T-C07 — Connection importers

**Wave:** 3 | **Size:** M | **Spec:** [wf-01](../wireframes/wf-01-connection-manager.md) section 4

**Depends on:** T-C02, T-C03
**Blocks:** nothing

**Owns:** `src/Basora.Infrastructure/Connections/Importers/**`,
`tests/Basora.Infrastructure.Tests/Importers/**`

**Deliverables**
- Importers for `~/.pgpass`, `~/.pg_service.conf`, `.env` `DATABASE_URL`, TablePlus,
  DBeaver and pgAdmin.
- Each reports availability, discovers without importing, and flags name conflicts and
  whether a password was found.

**Acceptance**
- [ ] Each importer parses real sample files from its source tool.
- [ ] Discovery never writes anything; import is a separate, explicit step.
- [ ] Passwords found during import go straight to `ISecretStore`.
- [ ] A malformed source file yields a clear error, not a crash or a partial import.
