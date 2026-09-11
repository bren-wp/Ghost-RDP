# Security Model

## Credential policy

The default credential policy is **password memory-only**. Passwords and equivalent secrets must not be persisted in JSON, XML, SQLite plaintext, registry plaintext, logs, process arguments, `.rdp` files, settings, or crash reports.

The current `mstsc.exe` integration uses an even narrower boundary: Ghost RDP does not ask for or transport a password at all. Microsoft Remote Desktop/Windows owns credential entry after the user explicitly starts a connection, including credentials requested by an RD Gateway.

Future credential persistence may use Windows Credential Manager, DPAPI, or an appropriate Windows Hello-backed mechanism only after a dedicated security review.

## Local UI settings

`settings.json` is a schema-versioned local preference file. It stores startup view, default saved-computer sort, optional last-view memory, and the last eligible view. It does not store hosts, usernames, domains, gateway hosts, passwords, tokens, or credential material.

A missing settings file yields in-memory defaults. Invalid JSON and unsupported future schema versions are rejected without rewriting the source during load. Automatic last-view persistence is disabled after a settings-load failure until the user explicitly saves or resets settings.

## Process launch policy

Ghost RDP never builds a shell command string from user-controlled connection values. Microsoft RDP is launched through `ProcessStartInfo` with `UseShellExecute = false`, and the random temporary `.rdp` path is passed as a single `ArgumentList` element.

A password is never sent to `mstsc.exe` on the command line. Ghost RDP does not invoke `cmd.exe`, PowerShell, or another shell to start an RDP session.

## Temporary `.rdp` files

A user-initiated connection creates a unique session directory below the current user's temporary directory and a random `.rdp` filename. The file contains only validated connection metadata needed by Microsoft Remote Desktop.

The generated file:

- contains no password, gateway password, access token, or equivalent secret;
- requires server authentication rather than permitting a failed server-authentication result to continue;
- keeps CredSSP enabled;
- requests Windows-owned credential prompting;
- for RD Gateway, includes only the validated gateway hostname and documented routing/credential-source settings;
- does not force target and gateway credentials to be shared;
- is deleted after the Microsoft RDP process exits;
- is eligible for stale-session cleanup after 24 hours if Ghost RDP terminates before normal cleanup.

## Remote-access route boundary

Direct/LAN mode makes no network configuration changes. Private VPN/overlay mode also connects directly to the target and assumes an independently configured private route already exists. It does not install, start, reconfigure, authenticate to, or monitor third-party VPN software.

RD Gateway mode configures only the Microsoft RDP client-side gateway properties for the user-initiated session. Ghost RDP does not configure the RD Gateway server, create gateway accounts, bypass gateway policies, or retain gateway secrets.

## Ghost RDP Host diagnostic boundary

Ghost RDP Host is a visible, read-only diagnostic application. It reads local Windows state from the registry, Service Control Manager, Windows Firewall policy, DNS, and network-interface APIs.

The Host does not request a configuration change and does not:

- enable or disable Remote Desktop;
- start or stop `TermService`;
- create, delete, enable, or disable firewall rules;
- change the Windows Firewall profile state;
- change NLA;
- change the RDP listening port;
- change network profiles;
- configure VPN software, port forwarding, or UPnP.

Readiness is deliberately conservative. Unknown or unavailable diagnostics are shown as unknown and cannot produce a green `Ready for Remote Desktop` result. VPN/private-overlay adapter detection is an indicator only and is not treated as proof that a secure route exists.

## Remote exposure policy

Ghost RDP must not automatically:

- forward TCP 3389 on a router;
- use UPnP to expose RDP;
- disable Windows Firewall;
- disable Network Level Authentication;
- bypass certificate or security checks;
- weaken UAC or endpoint security.

Remote access from another network should use a private VPN/overlay network or an administrator-managed RD Gateway.

## Accessibility boundary

Accessibility features may change presentation and navigation behavior but never weaken authentication, firewall, NLA, process-launch, or credential boundaries. Windows High Contrast is read as a system setting and only changes application brush resources.

## Packaging boundary

Release packaging publishes self-contained x64 App and Host executables and wraps them in a per-user Inno Setup installer. The installer defaults to the current user's local application-data Programs directory and does not require administrator privileges for the normal install path.

Packaging must not create or start a Windows service, create scheduled persistence, change Windows Firewall, expose an RDP port, change NLA, configure a VPN, or change host readiness state. CI scans both installer source and release-package scripts for prohibited firewall/service/elevation patterns, validates the generated artifacts and SHA-256 manifest, and performs a silent install/uninstall smoke test.

Current development artifacts are unsigned. Ghost RDP must not claim Authenticode signing until an authorized certificate or signing service actually signs the binaries. Uninstall removes installed program files and shortcuts but intentionally leaves the current user's saved-computer and UI-settings data untouched.

## Host policy

Ghost RDP Host is not a stealth agent. If a background service is introduced later, it must be clearly named, visible in Windows service/app management, documented, revocable, and uninstallable.

## Logging

Security-sensitive values must be redacted before logging. The shared `SecretSanitizer` is an initial defensive primitive; future structured logging must classify and exclude secret fields at the source.

## Security regression checks

CI rejects obvious command-line password patterns, RDP password-field patterns, gateway access-token fields, shell-launch patterns, Host mutation patterns, prohibited installer/service/firewall/elevation patterns, and missing security documentation. Tests cover input validation, profile persistence and schema migration, settings persistence and corrupt/future-schema handling, temporary `.rdp` cleanup, structured process arguments, command-injection-shaped input, absence of password/token data from generated `.rdp` content, RD Gateway serialization, Host readiness evaluation, firewall-port matching, release artifact validation, and installer install/uninstall behavior.
