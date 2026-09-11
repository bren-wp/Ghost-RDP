namespace GhostRdp.Core.Runtime;

public sealed record RdpRuntimeStatus(bool IsAvailable, string? ExecutablePath, string Message);
