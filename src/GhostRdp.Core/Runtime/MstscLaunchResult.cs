namespace GhostRdp.Core.Runtime;

public sealed record MstscLaunchResult(bool Success, int? ProcessId, string Message)
{
    public static MstscLaunchResult Failure(string message) => new(false, null, message);

    public static MstscLaunchResult Started(int processId) => new(
        true,
        processId,
        "Microsoft Remote Desktop opened. Windows owns credential entry for this connection.");
}
