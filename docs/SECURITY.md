# Security Model

## Credential policy

The default credential policy is **password memory-only**. Passwords and equivalent secrets must not be persisted in JSON, XML, SQLite plaintext, registry plaintext, logs, process arguments, `.rdp` files, settings, installer metadata, or crash reports.

The current `mstsc.exe` integration uses an even narrower boundary: Ghost RDP does not ask for or transport a password at all. Microsoft Remote Desktop/Windows owns credential entry after the user explicitly starts a connection, including credentials requested by an RD Gateway.

Future credential persistence may use Windows Credential Manager, DPAPI, or an appropriate Windows Hello-backed mechanism only after a dedicated security review.

## Runtime dependency and supply-chain policy

Production projects below `src/` intentionally depend only on .NET 8, WPF/Windows Desktop, Windows platform APIs, and other Ghost RDP projects in this repository. Third-party runtime NuGet packages and file-based external assembly references are prohibited by the current policy.

CI scans `src/` project/props/targets files and fails if a runtime `PackageReference` or external file `HintPath` is introduced. Existing Microsoft test packages remain development-only and are not shipped as App, Core, Host, or Setup dependencies.

Adding any third-party runtime component in the future requires an explicit policy change and a separate security, privacy, licensing, maintenance, and supply-chain review. See [DEPENDENCIES.md](DEPENDENCIES.md).

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
- is eligible for stale-session cleanup after 24 hours if Ghost RDP terminates before normal cleanup;
- is cleaned up only from Ghost RDP-owned GUID session directories, while unrelated directories below the temp root are left untouched.

## Remote-access route boundary

Direct/LAN mode makes no network configuration changes. Private VPN/overlay mode also connects directly to the target and assumes an independently configured private route already exists. It does not install, start, reconfigure, authenticate to, monitor, or depend on a third-party VPN SDK.

RD Gateway mode configures only the Microsoft RDP client-side gateway properties for the user-initiated session. Ghost RDP does not configure the RD Gateway server, create gateway accounts, bypass gateway policies, or retain gateway secrets.

## Ghost RDP Host diagnostic boundary

Ghost RDP Host is a visible, read-only diagnostic application. It reads local Windows state from the registry, Service Control Manager, Windows Firewall policy, DNS, and network-interface APIs.

The Host does not request a configuration change and does not enable or disable Remote Desktop, start or stop `TermService`, create or modify firewall rules, change NLA, change the RDP listening port, change network profiles, configure VPN software, forward ports, or use UPnP.

Readiness is deliberately conservative. Unknown or unavailable diagnostics are shown as unknown and cannot produce a green `Ready for Remote Desktop` result. VPN/private-overlay adapter detection is an indicator only and is not treated as proof that a secure route exists.

## Remote exposure policy

Ghost RDP must not automatically forward TCP 3389 on a router, use UPnP to expose RDP, disable Windows Firewall, disable Network Level Authentication, bypass certificate/security checks, weaken UAC, or weaken endpoint security. Remote access from another network should use a private VPN/overlay network or an administrator-managed RD Gateway.

## Accessibility and stability boundary

Accessibility features may change presentation and navigation behavior but never weaken authentication, firewall, NLA, process-launch, or credential boundaries. Windows High Contrast is read as a system setting and only changes application brush resources.

Production App and Host builds catch unexpected WPF dispatcher failures at the application boundary, show a generic user-safe error, and terminate rather than continuing in an unknown UI state. Unobserved background-task exceptions are marked observed; secrets are not included in these user-visible failure messages.

Saved-computer search debounce is UI-only. It does not create a background service, network worker, or credential cache and is stopped/detached when the main window closes.

## Packaging boundary

Release packaging produces self-contained x86, x64, and ARM64 App, Host, and Setup binaries. The normal Setup path is per-user under `%LOCALAPPDATA%\Programs\Ghost RDP` and runs as the current user without an elevation request.

Ghost RDP Setup uses an embedded, architecture-matched payload and transactional staging. Existing program files are moved to a temporary backup only after the replacement payload is extracted and validated; if finalization fails, Setup attempts to restore the prior installation.

Windows Installed Apps invokes the installed `GhostRDP-Setup.exe --uninstall` entry. A separate persistent `uninstall.exe`, `unins*.exe`, service, scheduled task, or hidden uninstaller is not shipped or installed. During removal, the same Setup executable is temporarily copied outside the installation directory so Windows can delete the installed copy after it exits; that temporary helper is not an installed product component and schedules its own cleanup.

Packaging must not create or start a Windows service, create scheduled persistence, change Windows Firewall, expose an RDP port, change NLA, configure a VPN, or change host-readiness state. CI validates PE architecture, hashes, archive content, the absence of static `.rdp` files and separate uninstall executables, runtime self-tests, and real install/uninstall behavior. ARM64 packages are additionally executed on a native Windows ARM64 CI runner.

Current release artifacts are unsigned. Ghost RDP must not claim Authenticode signing until an authorized certificate or signing service actually signs and verifies the binaries. Uninstall preserves the current user's saved computers and UI settings by default; removing that local data requires an explicit interactive choice.

## Release publication integrity

`scripts/release-preflight.ps1` is a release supply-chain guard. Normal CI requires App, Host, and Setup version alignment. Release-preparation validation additionally requires finalized release metadata, and the publication workflow requires the exact `release/v<version>` branch name. A mismatch stops publication before architecture packages are built.

The preflight contains no credential handling, network discovery, signing bypass, or package-download behavior. It validates repository-owned project and Markdown metadata only. Release publication remains contingent on the existing x86/x64/ARM64 security, integrity, runtime, and install/uninstall gates.

## Host policy

Ghost RDP Host is not a stealth agent. If a background service is introduced later, it must be clearly named, visible in Windows service/app management, documented, revocable, and uninstallable. The current architecture intentionally has no such service.

## Logging

Security-sensitive values must be redacted before logging. The shared `SecretSanitizer` is an initial defensive primitive; future structured logging must classify and exclude secret fields at the source.

## Security and dependency regression checks

CI rejects obvious command-line password patterns, RDP password-field patterns, gateway access-token fields, shell-launch patterns, Host mutation patterns, prohibited installer/service/firewall/elevation patterns, third-party runtime `PackageReference`/external assembly references, missing required documentation, and release-version drift before packaging.

Tests and packaging checks cover input validation, profile/schema persistence, settings corruption handling, saved-computer query behavior, temporary `.rdp` cleanup, structured process arguments, command-injection-shaped input, absence of password/token data, RD Gateway serialization, Host readiness evaluation, firewall-port matching, architecture validation, runtime startup self-tests, release artifact integrity, and Setup install/uninstall behavior.

## Documentation synchronization

Security-affecting changes must update README, CHANGELOG, ROADMAP, this document, and every affected dependency/privacy/packaging/release document in the same review cycle. See [Documentation index](README.md).
