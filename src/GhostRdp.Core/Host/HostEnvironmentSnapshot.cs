namespace GhostRdp.Core.Host;

public sealed record HostEnvironmentSnapshot(string ComputerName, string OperatingSystem, string CurrentUser)
{
    public static HostEnvironmentSnapshot Capture() => new(
        Environment.MachineName,
        Environment.OSVersion.VersionString,
        Environment.UserName);
}
