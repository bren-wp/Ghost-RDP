# Security Model

## Credential policy

The default credential policy is **password memory-only**. Passwords and equivalent secrets must not be persisted in JSON, XML, SQLite plaintext, registry plaintext, logs, process arguments, `.rdp` files, or crash reports.

Future credential persistence may use Windows Credential Manager, DPAPI, or an appropriate Windows Hello-backed mechanism only after a dedicated security review.

## Process launch policy

Ghost RDP must never build a shell command string from user-controlled values. When Microsoft RDP integration is added, it must use `ProcessStartInfo.ArgumentList` or an equivalently structured process API with `UseShellExecute = false` where applicable.

A password must never be sent to `mstsc.exe` on the command line.

## Remote exposure policy

Ghost RDP must not automatically:

- forward TCP 3389 on a router;
- use UPnP to expose RDP;
- disable Windows Firewall;
- disable Network Level Authentication;
- bypass certificate or security checks;
- weaken UAC or endpoint security.

Remote access from another network should use a private VPN/overlay network or an administrator-managed RD Gateway.

## Host policy

Ghost RDP Host is not a stealth agent. If a background service is introduced later, it must be clearly named, visible in Windows service/app management, documented, revocable, and uninstallable.

## Logging

Security-sensitive values must be redacted before logging. The shared `SecretSanitizer` is an initial defensive primitive; future structured logging must classify and exclude secret fields at the source.

## Security regression checks

CI rejects obvious command-line password patterns, shell-launch patterns, and missing security documentation. Tests cover input validation and secret sanitization. Additional regression tests are added with each connection/profile milestone.
