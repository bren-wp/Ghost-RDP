# Windows Platform Notes

## Baseline

The initial client and host target .NET 8 on Windows with WPF. Windows 10 and Windows 11 are the intended desktop environments for the first milestone.

## Microsoft Remote Desktop runtime

Ghost RDP detects `mstsc.exe` by checking the Windows System32 location and entries on `PATH`. Detection does not imply that a remote endpoint is reachable.

If `mstsc.exe` is absent, Connect actions remain disabled and the application reports the runtime as unavailable.

## Connect behavior

A user-initiated Connect action validates the host/IP, port, username, and domain, creates a random temporary `.rdp` file, and starts the detected `mstsc.exe` directly. The temporary file path is passed as a structured process argument rather than as a shell command string.

The `.rdp` file contains no password. Microsoft Remote Desktop/Windows owns credential entry and authentication. Ghost RDP reports only that the Microsoft RDP process was started; it does not claim that login or the remote desktop session succeeded.

Temporary session directories are cleaned when the Microsoft RDP process exits. Directories left after an abnormal Ghost RDP termination are eligible for stale cleanup after 24 hours.

## Incoming RDP support

Windows edition, Remote Desktop Services configuration, firewall rules, NLA state, and policy settings determine whether a computer can accept incoming RDP. The current Host foundation does not yet claim these states. Full readiness diagnostics are the next milestone.
