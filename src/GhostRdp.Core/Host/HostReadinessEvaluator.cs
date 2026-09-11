namespace GhostRdp.Core.Host;

public static class HostReadinessEvaluator
{
    public static HostReadinessResult Evaluate(HostReadinessSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.EditionSupportsIncomingRdp == false)
        {
            return new HostReadinessResult(
                HostReadinessLevel.Unsupported,
                "Windows edition does not support incoming RDP",
                "This Windows edition can use Remote Desktop as a client but is not a supported Remote Desktop host.");
        }

        if (snapshot.RdpEnabled == false)
        {
            return new HostReadinessResult(
                HostReadinessLevel.NotReady,
                "Remote Desktop disabled",
                "Windows is currently configured to deny incoming Remote Desktop connections.");
        }

        if (snapshot.RdpServiceState is HostServiceState.Stopped or HostServiceState.StopPending)
        {
            return new HostReadinessResult(
                HostReadinessLevel.NotReady,
                "Remote Desktop service not running",
                "The Windows Remote Desktop Services service is not currently running.");
        }

        if (snapshot.FirewallEnabled == false)
        {
            return new HostReadinessResult(
                HostReadinessLevel.Warning,
                "Windows Firewall disabled",
                "Remote Desktop may be reachable, but the active Windows Firewall profile is disabled. Ghost RDP does not change this setting.");
        }

        if (snapshot.FirewallBlocksAllInbound == true)
        {
            return new HostReadinessResult(
                HostReadinessLevel.NotReady,
                "Firewall blocks inbound traffic",
                "At least one active firewall profile is configured to block all inbound traffic, so firewall exceptions are ignored.");
        }

        if (snapshot.RdpFirewallRuleAvailable == false)
        {
            return new HostReadinessResult(
                HostReadinessLevel.NotReady,
                "Firewall rule unavailable",
                "No enabled inbound TCP allow rule covering the configured RDP port was found for every active firewall profile.");
        }

        if (snapshot.EditionSupportsIncomingRdp is null
            || snapshot.RdpEnabled is null
            || snapshot.RdpServiceState == HostServiceState.Unknown
            || snapshot.RdpPort is null
            || snapshot.NlaEnabled is null
            || snapshot.FirewallEnabled is null
            || snapshot.FirewallBlocksAllInbound is null
            || snapshot.RdpFirewallRuleAvailable is null)
        {
            return new HostReadinessResult(
                HostReadinessLevel.Unknown,
                "Readiness could not be fully verified",
                "One or more required Windows diagnostics are unavailable. Unknown values are not treated as ready.");
        }

        if (snapshot.RdpServiceState != HostServiceState.Running)
        {
            return new HostReadinessResult(
                HostReadinessLevel.NotReady,
                "Remote Desktop service is changing state",
                $"Remote Desktop Services is currently {FormatServiceState(snapshot.RdpServiceState)}.");
        }

        if (snapshot.NlaEnabled == false)
        {
            return new HostReadinessResult(
                HostReadinessLevel.Warning,
                "Remote Desktop ready with security warning",
                "The required host checks passed, but Network Level Authentication is disabled. Ghost RDP does not weaken or change NLA.");
        }

        if (ContainsPublicProfile(snapshot.NetworkProfiles))
        {
            return new HostReadinessResult(
                HostReadinessLevel.Warning,
                "Remote Desktop ready on a public network profile",
                "The required host checks passed, but a Public network profile is active. Confirm that this exposure is intentional and protected by your network design.");
        }

        return new HostReadinessResult(
            HostReadinessLevel.Ready,
            "Ready for Remote Desktop",
            "Windows edition, Remote Desktop configuration, service state, NLA, configured port, and active firewall checks passed.");
    }

    private static bool ContainsPublicProfile(string profiles) =>
        profiles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(profile => profile.Equals("Public", StringComparison.OrdinalIgnoreCase));

    private static string FormatServiceState(HostServiceState state) =>
        state.ToString().Replace("Pending", " pending", StringComparison.Ordinal).ToLowerInvariant();
}
