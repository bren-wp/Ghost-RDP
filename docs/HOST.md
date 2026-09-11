# Ghost RDP Host

Ghost RDP Host is a separate, visible Windows application that performs **read-only** readiness diagnostics for incoming Remote Desktop. It is not a stealth agent, it does not install persistence, and it does not change Windows configuration.

## Implemented readiness checks

The Host window reports:

- computer name and DNS hostname;
- Windows edition, display version, and build;
- current Windows user;
- whether the installed Windows edition is a supported incoming RDP host edition;
- whether Windows is configured to allow incoming Remote Desktop;
- `TermService` (Remote Desktop Services) runtime state;
- configured RDP listening port;
- Network Level Authentication state;
- active Windows Firewall profiles;
- whether Windows Firewall is enabled on all active profiles;
- whether any active profile has `BlockAllInboundTraffic` enabled;
- whether an enabled inbound TCP allow rule covering the configured RDP port is present for every active profile;
- active non-loopback LAN addresses;
- active tunnel/PPP adapters and adapters whose identity indicates Tailscale, WireGuard, ZeroTier, or VPN usage.

The VPN/private-overlay result is deliberately described as an **adapter indicator**, not proof that a secure route to the machine is usable.

## Runtime sources

Ghost RDP Host uses Windows-owned sources rather than simulated status values:

| Diagnostic | Runtime source |
| --- | --- |
| Windows edition/version | `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion` |
| RDP enabled | `HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\fDenyTSConnections` |
| RDP port | `HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp\PortNumber` |
| NLA | `HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp\UserAuthentication` |
| Remote Desktop Services | Windows Service Control Manager, service name `TermService` |
| Firewall/profile state | Windows Firewall `HNetCfg.FwPolicy2` / `INetFwPolicy2` |
| Firewall inbound rule | Windows Firewall rule collection, evaluated by protocol, direction, action, profile, service/port |
| Hostname/LAN addresses | .NET DNS and `NetworkInterface` APIs |
| VPN/private-network indicators | active interface type/name/description only |

Windows Home editions are reported as unsupported for incoming Remote Desktop hosting. Professional, Enterprise, Education, and Windows Server families are treated as supported when the edition can be identified reliably. Unknown editions remain `Unknown` instead of being guessed.

## Readiness rules

`Ready for Remote Desktop` is shown only when all required checks are known and pass:

1. Windows edition supports incoming RDP;
2. incoming Remote Desktop is enabled;
3. `TermService` is running;
4. the RDP port is known and valid;
5. NLA is enabled;
6. Windows Firewall is enabled on active profiles;
7. active profiles are not configured to block all inbound traffic;
8. an enabled inbound TCP allow rule covers the configured RDP port for each active profile.

NLA disabled or a Public network profile produces a warning rather than a green ready state. Missing diagnostic data produces `Unknown`, never `Ready`.

## Read-only policy

Ghost RDP Host does **not**:

- enable or disable Remote Desktop;
- start or stop `TermService`;
- enable/disable Windows Firewall;
- create, delete, or modify firewall rules;
- enable or disable NLA;
- change the RDP port;
- configure VPN software;
- configure router port forwarding or UPnP;
- expose TCP 3389 to the Internet.

Configuration changes remain explicit Windows/admin actions outside the Host diagnostic flow.
