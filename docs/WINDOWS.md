# Windows Platform Notes

## Baseline

The initial client and host target .NET 8 on Windows with WPF. Windows 10 and Windows 11 are the intended desktop environments for the first milestone.

## Microsoft Remote Desktop runtime

Ghost RDP detects `mstsc.exe` by checking the Windows System32 location and entries on `PATH`. Detection does not launch the executable and does not imply that a remote endpoint is reachable.

If `mstsc.exe` is absent, the application reports the runtime as unavailable. Future Connect actions must stay disabled in that state.

## Incoming RDP support

Windows edition, Remote Desktop Services configuration, firewall rules, NLA state, and policy settings determine whether a computer can accept incoming RDP. The initial Host foundation does not claim these states. Full readiness diagnostics are a later milestone.
