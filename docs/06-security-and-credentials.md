# 06 — Security and Credentials

Owner project: **`Basora.Security`**.

The governing rule for the entire document:

> **Credentials are never written to disk in plaintext, never logged, never placed in a
> connection string that outlives the connection attempt, and never included in an
> exported profile, a crash dump, a support bundle, or an AI request.**

---

## 1. Secret storage

### Contract

```csharp
public interface ISecretStore
{
    string BackendName { get; }
    bool IsHardwareBacked { get; }
    Task<Result<string>> GetAsync(string secretRef, CancellationToken ct);
    Task<Result<string>> SetAsync(string? secretRef, string value, CancellationToken ct);
    Task<Result> DeleteAsync(string secretRef, CancellationToken ct);
    Task<Result> HealthCheckAsync(CancellationToken ct);
}
```

`ConnectionProfile` stores only a `SecretRef` (an opaque string such as
`basora:conn:{guid}:password`). The profile JSON on disk therefore contains no secret
material and is safe to share, diff, or commit — a property we advertise.

### Backends by platform

| Platform | Backend | API |
|---|---|---|
| Windows | Credential Manager | `CredRead`/`CredWrite` via P/Invoke, `CRED_TYPE_GENERIC` |
| macOS | Keychain | `security` framework via P/Invoke, or the `security` CLI as fallback |
| Linux | Secret Service | libsecret over D-Bus (GNOME Keyring, KWallet) |
| Any (fallback) | Encrypted file vault | See below |

**Windows note:** DPAPI (`ProtectedData`) is simpler but ties the secret to the user
profile with no UI for the user to inspect or revoke. Credential Manager is visible and
manageable, so prefer it; DPAPI is the encryption used *inside* the fallback vault.

### Fallback vault

Linux without a Secret Service daemon is common (headless, minimal desktops, WSL). The
fallback is an encrypted file at `{data}/secrets.vault`:

- Key derived from a user-supplied master passphrase via **Argon2id** (or PBKDF2-
  SHA256 with at least 600,000 iterations if an Argon2 dependency is unacceptable).
- **AES-256-GCM** per entry with a unique nonce; the auth tag detects tampering.
- Unlocked once per app session, held in memory only, zeroed on lock/exit.
- Auto-lock after a configurable idle period (default 30 minutes) and on OS sleep.

The user must be told, in the connection editor, which backend is in use and whether it
is OS-backed. Silently downgrading to a weaker store is a trust failure.

### In-memory handling

- Fetch a secret **immediately before** building a connection string; do not cache the
  plaintext on the profile or the ViewModel.
- Prefer `SecureString`-equivalent handling where the platform supports it; where it
  does not (most of .NET), minimise lifetime and overwrite the buffer after use.
- Npgsql needs the password as a `string` in the connection string. Accept that, build
  the `NpgsqlDataSource` once, and drop the reference. Do not keep the connection string
  on a long-lived object.

### Optional: no storage at all

Every connection may be marked **"always prompt for password"**. Nothing is persisted;
the user is asked at connect time, and the value lives only for that session. Many
production users require exactly this.

---

## 2. TLS / SSL

`SslMode` maps to Npgsql as follows:

| Basora | Npgsql `SslMode` | Encryption | Server cert verified | Hostname verified |
|---|---|---|---|---|
| `Disable` | `Disable` | no | no | no |
| `Allow` | `Allow` | maybe | no | no |
| `Prefer` (default) | `Prefer` | maybe | no | no |
| `Require` | `Require` | yes | no | no |
| `VerifyCa` | `VerifyCA` | yes | yes | no |
| `VerifyFull` | `VerifyFull` | yes | yes | yes |

Product rules:

1. The UI must state plainly that `Require` **encrypts but does not authenticate** the
   server, and is therefore vulnerable to an active MITM. Most tools hide this; we say it.
2. A **Production** environment profile using `Disable`, `Allow` or `Prefer` shows a
   persistent warning badge on the connection card and in the status bar. It is not
   blocked — some users legitimately connect over a private network or a tunnel — but it
   is never silent.
3. `VerifyCa`/`VerifyFull` take a root certificate path; client certificate + key paths
   are supported for mutual TLS.
4. On a certificate validation failure, show the actual chain problem (expired, unknown
   issuer, hostname mismatch, with the presented subject/SAN), not "SSL error". Offer a
   per-profile "trust this certificate" that pins the SHA-256 fingerprint — never a
   global "ignore certificate errors" switch.

---

## 3. SSH tunnelling

### Design

A local port forward: Basora listens on `127.0.0.1:<ephemeral>`, forwards over SSH to
`remoteHost:remotePort` (defaulting to the profile's host and port), and the Npgsql
connection targets the local endpoint.

```text
Basora  ->  127.0.0.1:54321  ==SSH==>  bastion.company.com:22  -->  db.internal:5432
```

```csharp
public interface ISshTunnelManager
{
    Task<Result<TunnelHandle>> OpenAsync(SshTunnelConfig config, string remoteHost,
                                         int remotePort, CancellationToken ct);
    Task CloseAsync(Guid tunnelId, CancellationToken ct);
    IReadOnlyList<TunnelHandle> Active { get; }
}

public sealed record TunnelHandle(Guid Id, string LocalHost, int LocalPort,
                                  SshTunnelConfig Config, DateTimeOffset OpenedAt);
```

### Requirements

- Bind to **`127.0.0.1` only**, never `0.0.0.0`. Binding to all interfaces would expose
  someone's production database to their local network.
- Allocate an ephemeral port; never a fixed one (collisions across profiles).
- Tunnel lifetime is tied to the session. Closing the connection closes the tunnel;
  a dropped tunnel moves the session to `Reconnecting`.
- Support password, private key (with passphrase from `ISecretStore`), and **ssh-agent**.
  Agent support matters — many teams forbid keys on disk.
- Support OpenSSH-format keys (`-----BEGIN OPENSSH PRIVATE KEY-----`), not only PEM.
  Verify the chosen SSH library handles this and modern key types (ed25519); if not,
  that constraint must be surfaced at `T-S03` rather than discovered by a user.
- **Host key verification is on by default.** On first connect, show the fingerprint and
  ask; store the accepted fingerprint per host. On a later mismatch, refuse and explain
  loudly — a changed host key is exactly the scenario this protects against.
- Keepalives so idle tunnels are not dropped by the bastion.
- Optional read of `~/.ssh/config` for host aliases, `ProxyJump` and `IdentityFile`
  (nice-to-have, `T-S03b`, not MVP-1 blocking).

---

## 4. Logging and redaction

Serilog is configured with an enricher that scrubs before any sink writes.

**Never logged:** passwords, passphrases, private key material, connection strings
containing a password, SSH host keys, AI provider API keys, or result-set cell values.

**Redaction implementation:**

- A destructuring policy for `ConnectionProfile` and `SshTunnelConfig` that emits only
  non-secret fields.
- A regex sweep over rendered messages for `password=`, `Password=`, `passfile=`,
  `sslpassword=` and bearer-token shapes, replacing the value with `***`.
- SQL text is logged at Debug only, and **parameter values are never logged** — a
  parameter is exactly where personal data lives. Log parameter *names and types*.

**Log location:** `{app-data}/logs/basora-.log`, rolling daily, 7 files retained, with a
"Reveal log folder" action in Help. A support bundle exports logs + settings + profiles
**with secrets and parameter values already excluded**, and shows the user the exact
file contents before they send it anywhere.

---

## 5. Audit log

Distinct from the debug log: a durable, user-facing record of what was done.

```csharp
public sealed record AuditEntry
{
    public required Guid Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string OsUser { get; init; }
    public required Guid ConnectionId { get; init; }
    public required string ConnectionName { get; init; }
    public required DeploymentEnvironment Environment { get; init; }
    public required string Database { get; init; }
    public required AuditAction Action { get; init; }
    public required string Statement { get; init; }
    public long? AffectedRows { get; init; }
    public bool Succeeded { get; init; }
    public string? ErrorCode { get; init; }
    public TimeSpan Duration { get; init; }
    public RiskLevel AssessedRisk { get; init; }
    public bool ConfirmationBypassed { get; init; }
}

public enum AuditAction
{
    Connect, Disconnect, Query, DataModification, SchemaModification,
    PermissionChange, Import, Export, Backup, Restore, TerminateBackend
}
```

Rules:

- **Append-only**, local file, one line of JSON per entry.
- Records **every mutation** (data or schema), every export, every backend termination,
  and every connection to a Production-tagged profile — regardless of whether the query
  succeeded.
- Never records `SELECT` result *values*, only the statement and row count.
- Viewable and exportable in-app. Not deletable from within the app.
- Retention is configurable but has a floor for Production-tagged connections.

This is what turns "someone truncated the orders table" into an answerable question, and
it is the foundation for the enterprise audit feature later.

---

## 6. AI data boundary (MVP 3, constrained now)

When the AI assistant is used, state precisely what leaves the machine.

**May be sent** (with explicit per-connection opt-in):
schema DDL, column names and types, index definitions, `EXPLAIN` output, table
statistics (row counts, sizes, scan counts), the user's own SQL text, and error messages.

**Never sent without a separate, explicit, per-request confirmation:**
result-set rows, cell values, `pg_stat_statements` query text from *other* users'
sessions, hostnames, usernames, or connection strings.

**Never sent at all:** passwords, keys, or anything from `ISecretStore`.

Requirements:

- A visible indicator whenever AI is enabled for a connection, and an explicit
  **"AI disabled"** default for any profile tagged Production.
- A **preview panel** showing the exact context payload before the first request of a
  session, so the user can see what we are actually sending.
- A per-connection kill switch and a global one.
- The AI tool surface exposed to the model contains **no mutating operation**. Generated
  SQL is returned as text for review. This is enforced at the tool-definition level, not
  by prompting.

---

## 7. Application-level protections

| Threat | Mitigation |
|---|---|
| SQL injection via UI-built queries | All values parameterised; identifiers quoted through one utility (`05` section 6) |
| Malicious CSV on import | Never `COPY ... FROM` a client path server-side; stream through the client. Neutralise leading `=`, `+`, `-`, `@` on **export** to prevent CSV formula injection in spreadsheets |
| Malicious connection URI (pasted) | Parse strictly, reject unknown parameters, never pass through unrecognised libpq options that could change auth behaviour |
| Workspace file tampering | Validate on load; a malformed workspace degrades to defaults rather than crashing |
| Secrets in crash dumps | Disable full-memory dumps by default; scrub the diagnostic bundle |
| Untrusted `pg_stat_statements` text rendered as UI | Treat all database-sourced strings as data; never interpret as markup or execute |
| Clipboard leakage | "Copy row" of a masked/sensitive column warns once; no automatic clipboard writes |

---

## 8. Data masking (MVP 3, principle set now)

For cloning production into development, masking strategies must be **deterministic per
run but non-reversible**: the same input maps to the same fake value within one clone
(so joins survive) but the mapping is not recoverable.

| Data | Strategy |
|---|---|
| Email | `user_{hash}@example.test` |
| Phone | Format-preserving random |
| Name | Dictionary substitution |
| Address | Locale-appropriate fake |
| Payment identifiers | Full replacement, never partial — a "last 4 preserved" scheme is still identifying |
| Free text | Redact entirely by default; per-column opt-out |

Masking configuration is per column and must be reviewed before a clone runs. Default
for an unclassified column in a production clone is **exclude the table**, not "copy it
and hope". Failing closed is the only defensible default here.

---

## 9. Testing

- `ISecretStore` conformance suite run against every backend, including the fallback
  vault: set/get/delete/overwrite/missing-key/corrupt-entry.
- Redaction tests: a table of inputs containing passwords in a dozen shapes, asserting
  none survive to a sink.
- Tunnel tests: an SSH server container, verifying local-only binding, host-key
  mismatch refusal, and tunnel teardown on session close.
- TLS tests: containers configured for each `sslmode`, plus an expired-cert and a
  wrong-hostname case asserting the specific error text.
- A test asserting `ConnectionProfile` serialisation contains no field whose name or
  value looks like a secret.
