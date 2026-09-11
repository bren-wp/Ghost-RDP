# Remote Access Model

Ghost RDP is intended to let users reach computers they control from another location without automatically publishing Windows RDP to the public Internet.

## Preferred model A: private network or overlay

Ghost RDP should work with an IP address or hostname already reachable through a private network such as WireGuard, Tailscale, ZeroTier, or a user-managed VPN. Ghost RDP does not need to control those products to use the private address they provide.

## Preferred model B: Remote Desktop Gateway

A later milestone may add configuration for an administrator-managed RD Gateway over HTTPS/TLS, including domain credentials where required.

## Explicitly out of scope

Ghost RDP must not create an improvised tunneling protocol merely to imitate remote-support products. It must not automatically expose port 3389, change router configuration, disable security controls, or silently pair devices.

Any future device enrollment must be user initiated, visible, explicit, short-lived where practical, and revocable.
