# Remote Access Model

Ghost RDP is designed to manage Remote Desktop connections to computers the user controls. It does not automatically expose Windows Remote Desktop to the public Internet and does not provision a third-party network path.

## Connection routes

Ghost RDP exposes three explicit connection routes in saved computers and Quick Connect.

### Direct / LAN

Microsoft Remote Desktop connects directly to the validated target host/IP and port. Ghost RDP does not change the local or remote network to make that address reachable.

### Private VPN / overlay

The RDP transport is still a direct connection to the target host/IP and port. The label records the user's intended route and makes the security boundary clear: a private VPN or overlay must already be connected and routing the target address before Ghost RDP starts the session.

Ghost RDP does not install, start, sign in to, configure, modify, or depend on a third-party VPN/overlay SDK. A detected tunnel adapter on Ghost RDP Host remains an informational indicator rather than proof that a usable route exists.

### RD Gateway

When RD Gateway is selected, a validated gateway hostname is required. Ghost RDP writes explicit Microsoft RDP gateway properties to the temporary `.rdp` file:

- `gatewayhostname:s:<host>`;
- `gatewayusagemethod:i:1` to use the specified gateway;
- `gatewayprofileusagemethod:i:1` to use the explicit gateway settings;
- `gatewaycredentialssource:i:4` so Windows selects the credential source at logon;
- `promptcredentialonce:i:0` so Ghost RDP does not force gateway and target credentials to be shared.

The gateway hostname is connection metadata, not a credential. Ghost RDP never stores or writes the RD Gateway password. Microsoft Remote Desktop/Windows owns both gateway and target credential prompts.

## Profile schema v2

Profile/store schema v2 adds the remote-access route and optional RD Gateway hostname. Existing schema-v1 profiles are migrated in memory to the Direct route when read. Loading a v1 file does not rewrite it; the current schema is written only after a later explicit profile save.

## Host readiness versus reachability

Ghost RDP Host can inspect local incoming-RDP readiness, including the configured RDP port, service state, NLA, active firewall profiles, and inbound firewall rule coverage.

A green Host readiness result means the required **local Windows** checks passed. It does not prove that another device can reach the computer across NAT, a corporate perimeter, carrier-grade NAT, an RD Gateway policy boundary, or a private overlay.

## Remote-network recommendation

For access from another network, prefer one of these administrator-controlled designs:

1. a private VPN or overlay network that places the client and host on a trusted private path;
2. an administrator-managed Remote Desktop Gateway (RD Gateway).

Ghost RDP models both choices explicitly but does not provision either network path.

## Intentionally unsupported shortcuts

Ghost RDP does not automatically:

- forward TCP 3389 on a router;
- use UPnP to create router mappings;
- disable or weaken Windows Firewall;
- disable Network Level Authentication;
- change the RDP listener port;
- bypass certificate or authentication warnings;
- install or reconfigure VPN/overlay software;
- configure an RD Gateway server;
- configure hidden persistence or a stealth remote-access agent.

Remote-route labels and Host diagnostics describe intent and observable local state. They must not be interpreted as proof of end-to-end authorization, reachability, or successful authentication.

## Runtime dependency policy

Remote-access functionality uses Ghost RDP validation/serialization code plus Windows-owned `mstsc.exe`. No third-party RDP engine, relay client, VPN SDK, gateway SDK, or network-tunneling runtime package is included. See [DEPENDENCIES.md](DEPENDENCIES.md).

## Documentation synchronization

Changes to routes, `.rdp` properties, reachability claims, or network assumptions must update README, CHANGELOG, ROADMAP, Architecture, Security, Privacy, Windows, Release documentation, and this document together. See [Documentation index](README.md).
