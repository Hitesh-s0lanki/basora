# wf-02 — Connection Editor

> Task: `T-C06`. Dialog, not a document.

---

## 1. Purpose

Create or edit one connection profile: target, credentials, TLS, SSH tunnel, environment
and safety defaults. The job it must do well is **making the security posture of a
connection legible** — a user should be able to tell, at a glance, whether this
connection is encrypted, verified, tunnelled, and how their password is stored.

Most tools bury this. We put it on the first tab.

---

## 2. Entry points

- **New** from [wf-01](wf-01-connection-manager.md), blank or URI-pre-filled.
- **Edit** from a connection card, the connection tab context menu, or the status bar
  environment pill.
- A failed connection's "Edit connection" action, which opens on the relevant tab with
  the offending field focused.

---

## 3. Layout

```text
+---------------------------------------------------------------------------+
|  Edit Connection                                                     [X]  |
+---------------------------------------------------------------------------+
|  [ General ] [ Security ] [ SSH Tunnel ] [ Advanced ]                     |
+---------------------------------------------------------------------------+
|                                                                           |
|  Paste a connection URI to fill this form                                 |
|  [ postgresql://...                                          ] [ Parse ]  |
|                                                                           |
|  Name          [ Production                                    ]          |
|  Environment   ( ) Local  ( ) Development  ( ) Staging  (o) Production    |
|                # This connection will require typed confirmation for      |
|                  destructive operations.                                  |
|                                                                           |
|  Host          [ postgres.company.com              ]  Port [ 5432 ]       |
|  Database      [ production                        ]  [ Browse... ]       |
|  Username      [ app_readonly                      ]                      |
|  Password      [ ****************                  ]  [ Show ]            |
|                (o) Save in Windows Credential Manager (OS-backed)         |
|                ( ) Always prompt me                                       |
|                                                                           |
|  Colour        [#] (defaults to environment colour)                       |
|                                                                           |
+---------------------------------------------------------------------------+
|  [ Test Connection ]                                     [Cancel] [Save]  |
+---------------------------------------------------------------------------+
|  v  Connected in 84 ms - PostgreSQL 16.2 - role app_readonly              |
|     TLS 1.3, certificate verified (VerifyFull) - 14 databases available   |
+---------------------------------------------------------------------------+
```

### Security tab

```text
|  SSL Mode      [ Verify Full                            v ]               |
|                v Encrypted AND the server identity is verified.           |
|                  This is the safest option.                               |
|                                                                           |
|  Root cert     [ /etc/ssl/certs/company-ca.pem          ] [ Browse ]      |
|  Client cert   [                                        ] [ Browse ]      |
|  Client key    [                                        ] [ Browse ]      |
|                                                                           |
|  Pinned fingerprint                                                        |
|    SHA256:9f2a...c41d   [ Clear ]                                          |
|                                                                           |
|  ------------------------------------------------------------------       |
|  Safety                                                                    |
|  [x] Safe mode (auto-LIMIT unbounded SELECT, escalate confirmations)       |
|  [x] Open sessions read-only until I explicitly enable writes              |
|  [x] Record this connection in the audit log                               |
|  [ ] Allow the AI assistant to use this connection    (off for Production) |
```

The mode description text changes with the selection and is blunt:

| Mode | Description shown |
|---|---|
| Disable | **Not encrypted.** Anyone on the network path can read your data and credentials. |
| Allow / Prefer | May or may not be encrypted; the server decides. Not verified. |
| Require | Encrypted, **but the server is not verified** — vulnerable to an active machine-in-the-middle. |
| Verify CA | Encrypted and the certificate chain is verified, but not the hostname. |
| Verify Full | Encrypted and the server identity is fully verified. Safest. |

Selecting `Disable`, `Allow`, `Prefer` or `Require` while Environment is Production
shows an inline `status.warning` row. It does not block.

### SSH tunnel tab

```text
|  [x] Connect through an SSH tunnel                                        |
|                                                                           |
|  SSH host      [ bastion.company.com          ]  Port [ 22 ]              |
|  SSH user      [ hitesh                       ]                           |
|  Auth          ( ) Password  (o) Private key  ( ) SSH agent               |
|  Key file      [ ~/.ssh/id_ed25519            ] [ Browse ]                |
|  Passphrase    [ ****                         ]                           |
|                                                                           |
|  Forward to    [ db.internal ] : [ 5432 ]   (defaults to the host above)  |
|                                                                           |
|  Host key      SHA256:aQ4f...8x2   accepted 2026-08-14   [ Forget ]       |
|                                                                           |
|  Basora listens on 127.0.0.1 only. The tunnel closes with the connection. |
```

### Advanced tab

Connect timeout, command timeout, application name, default schema, search_path,
timezone display (server / UTC / local), max query-channel connections, statement
timeout, `lock_timeout`, tags, and folder.

---

## 4. Component inventory

**URI parse bar** — accepts `postgresql://`, `postgres://` and libpq key/value strings.
On parse: fills every matching field, moves the password straight into `ISecretStore`,
shows exactly which fields were filled, and highlights anything ambiguous. Unknown query
parameters are listed and **not** silently applied.

**Environment radio** — changing it updates the colour swatch, the safety defaults, and
the explanatory line beneath. Choosing Production auto-enables safe mode, read-only
default, and audit, and disables AI — each individually overridable, but the defaults
flip together.

**Password field** — masked, with Show (revealed for 10 s then re-masked). Beneath it,
the storage choice, naming the actual backend. If the backend is unavailable, the
"Always prompt me" option is pre-selected and the other is disabled with an explanation.

**Database Browse** — connects with the current settings and lists databases, so the
user does not have to remember the exact name. Requires the rest of the form to be valid.

**Test Connection** — **always inline, never a dialog**. Reports: latency, server
version, current role, whether the role is superuser, negotiated TLS version and cipher,
certificate verification outcome, tunnel state, and database count. Failures report the
mapped error from `05-postgresql-data-layer.md` section 10 with a specific next step and
focus the field at fault.

---

## 5. States

| State | Rendering |
|---|---|
| **New (blank)** | Name focused; sensible defaults; Save disabled until required fields are valid |
| **Pre-filled from URI** | Filled fields briefly highlighted; a summary line lists what was set and what was ignored |
| **Editing existing** | Password shows a placeholder meaning "unchanged"; clearing it is an explicit action |
| **Validating** | Test button shows a spinner and becomes Cancel; the form stays editable |
| **Test succeeded** | Green result strip with the details above |
| **Test failed** | Red result strip, mapped message, remediation, focus moved to the offending field |
| **Secret store unavailable** | Storage options collapse to "Always prompt me" with an explanation and a retry |
| **Unsaved changes on close** | Prompt: Save / Discard / Cancel |

---

## 6. Interactions and keyboard

| Key | Action |
|---|---|
| `Ctrl+T` | Test connection |
| `Ctrl+S` / `Enter` | Save (Enter only when no multi-line field is focused) |
| `Escape` | Cancel (prompts if dirty) |
| `Ctrl+Tab` | Next tab in the dialog |
| `Ctrl+V` in the URI bar | Paste and auto-parse |

Validation is inline and immediate: port range, host non-empty, required cert paths
present for the chosen SSL mode, key file readable for the chosen SSH auth. Errors appear
**below** the field, never as tooltips or placeholders.

---

## 7. Data contract

```csharp
public interface IConnectionValidator
{
    ValidationResult Validate(ConnectionProfile profile);
}

public interface IConnectionTester
{
    Task<Result<ConnectionTestReport>> TestAsync(
        ConnectionProfile profile, string? password, CancellationToken ct);
}

public sealed record ConnectionTestReport(
    TimeSpan Latency, string ServerVersion, string CurrentRole, bool IsSuperuser,
    string? TlsVersion, string? TlsCipher, CertificateVerification CertVerification,
    bool TunnelUsed, IReadOnlyList<string> Databases, IReadOnlyList<string> Warnings);

public enum CertificateVerification { NotEncrypted, NotVerified, ChainVerified, FullyVerified }

public interface IConnectionUriParser
{
    Result<ParsedUri> Parse(string input);
}

public sealed record ParsedUri(
    ConnectionProfile Draft, string? Password,
    IReadOnlyList<string> AppliedFields, IReadOnlyList<string> IgnoredParameters);
```

Plus `ISecretStore` and `ISshTunnelManager` from `06-security-and-credentials.md`.

---

## 8. Acceptance criteria

- [ ] URI parsing handles `postgresql://`, `postgres://` and libpq key/value strings,
      including percent-encoded passwords and IPv6 hosts in brackets.
- [ ] A parsed password never lands in the profile object or on disk in plaintext.
- [ ] SSL mode descriptions are shown and are accurate; `Require` explicitly states it
      does not verify the server.
- [ ] Production + weak SSL shows a warning and does not block.
- [ ] Choosing Production flips safe mode, read-only and audit on, and AI off, each
      individually overridable.
- [ ] Test Connection reports latency, version, role, TLS state and tunnel state inline,
      and is cancellable.
- [ ] Test failure focuses the field at fault with a mapped, actionable message.
- [ ] SSH host key is shown on first connect and a later mismatch is refused loudly.
- [ ] "Always prompt me" stores nothing.
- [ ] Secret-store unavailability degrades gracefully with an explanation.
- [ ] Fuzzing the URI parser with malformed input never throws unhandled.
- [ ] Closing with unsaved changes prompts.
