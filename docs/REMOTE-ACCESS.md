# Remote Access Model

Ghost RDP is designed to manage Remote Desktop connections to computers the user controls. It does not automatically expose Windows Remote Desktop to the public Internet.

## Current local-network behavior

The Windows client can launch Microsoft Remote Desktop for a validated host/IP and port. Ghost RDP Host can inspect local incoming-RDP readiness, including the configured RDP port, service state, NLA, active firewall profiles, and inbound firewall rule coverage.

A green Host readiness result means the required **local Windows** checks passed. It does not prove that another device can reach the computer across NAT, a corporate perimeter, carrier-grade NAT, or another external network boundary.

## Remote-network recommendation

For access from another network, prefer one of these administrator-controlled designs:

1. a private VPN or overlay network that places the client and host on a trusted private path;
2. an administrator-managed Remote Desktop Gateway (RD Gateway).

Ghost RDP does not configure either path automatically in the current milestone. Detected VPN/tunnel adapters are informational indicators only and are not treated as proof of reachability or security.

## Intentionally unsupported shortcuts

Ghost RDP does not automatically:

- forward TCP 3389 on a router;
- use UPnP to create router mappings;
- disable or weaken Windows Firewall;
- disable Network Level Authentication;
- change the RDP listener port;
- bypass certificate or authentication warnings;
- configure hidden persistence or a stealth remote-access agent.

Future VPN/private-overlay and RD Gateway-aware UX must preserve these boundaries and clearly distinguish local host readiness from end-to-end remote reachability.
