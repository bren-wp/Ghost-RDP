namespace GhostRdp.Core.Host;

public enum HostServiceState
{
    Unknown,
    Stopped,
    StartPending,
    StopPending,
    Running,
    ContinuePending,
    PausePending,
    Paused
}

public enum HostReadinessLevel
{
    Unknown,
    Unsupported,
    NotReady,
    Warning,
    Ready
}

public sealed record HostReadinessResult(HostReadinessLevel Level, string Title, string Detail);

public sealed record HostReadinessSnapshot
{
    public string ComputerName { get; init; } = string.Empty;

    public string Hostname { get; init; } = string.Empty;

    public string WindowsEdition { get; init; } = "Unknown";

    public string WindowsVersion { get; init; } = "Unknown";

    public string CurrentUser { get; init; } = string.Empty;

    public bool? EditionSupportsIncomingRdp { get; init; }

    public bool? RdpEnabled { get; init; }

    public HostServiceState RdpServiceState { get; init; } = HostServiceState.Unknown;

    public int? RdpPort { get; init; }

    public bool? NlaEnabled { get; init; }

    public bool? FirewallEnabled { get; init; }

    public bool? FirewallBlocksAllInbound { get; init; }

    public bool? RdpFirewallRuleAvailable { get; init; }

    public string NetworkProfiles { get; init; } = "Unknown";

    public IReadOnlyList<string> LanAddresses { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> PrivateNetworkIndicators { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Diagnostics { get; init; } = Array.Empty<string>();
}
