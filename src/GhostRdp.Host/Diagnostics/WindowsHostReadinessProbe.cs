using GhostRdp.Core.Host;

namespace GhostRdp.Host.Diagnostics;

internal static class WindowsHostReadinessProbe
{
    public static HostReadinessSnapshot Capture()
    {
        var diagnostics = new List<string>();

        if (!OperatingSystem.IsWindows())
        {
            diagnostics.Add("Ghost RDP Host readiness diagnostics require Windows.");
            return new HostReadinessSnapshot
            {
                ComputerName = Environment.MachineName,
                Hostname = Environment.MachineName,
                WindowsEdition = Environment.OSVersion.VersionString,
                WindowsVersion = Environment.OSVersion.VersionString,
                CurrentUser = BuildCurrentUser(),
                Diagnostics = diagnostics
            };
        }

        var registry = WindowsRegistryDiagnostics.Capture(diagnostics);
        var network = WindowsNetworkDiagnostics.Capture(diagnostics);
        var serviceState = HostServiceState.Unknown;
        try
        {
            serviceState = WindowsServiceDiagnostics.ReadRemoteDesktopServiceState(diagnostics);
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            diagnostics.Add($"Remote Desktop Services status is unavailable: {exception.Message}");
        }

        FirewallDiagnosticsSnapshot firewall;
        try
        {
            firewall = WindowsFirewallDiagnostics.Capture(registry.RdpPort, diagnostics);
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or NotSupportedException)
        {
            diagnostics.Add($"Windows Firewall diagnostics are unavailable: {exception.Message}");
            firewall = new FirewallDiagnosticsSnapshot(null, null, null, "Unknown");
        }

        return new HostReadinessSnapshot
        {
            ComputerName = Environment.MachineName,
            Hostname = network.Hostname,
            WindowsEdition = registry.WindowsEdition,
            WindowsVersion = registry.WindowsVersion,
            CurrentUser = BuildCurrentUser(),
            EditionSupportsIncomingRdp = registry.EditionSupportsIncomingRdp,
            RdpEnabled = registry.RdpEnabled,
            RdpServiceState = serviceState,
            RdpPort = registry.RdpPort,
            NlaEnabled = registry.NlaEnabled,
            FirewallEnabled = firewall.FirewallEnabled,
            FirewallBlocksAllInbound = firewall.BlocksAllInbound,
            RdpFirewallRuleAvailable = firewall.RdpInboundRuleAvailable,
            NetworkProfiles = firewall.ActiveProfiles,
            LanAddresses = network.LanAddresses,
            PrivateNetworkIndicators = network.PrivateNetworkIndicators,
            Diagnostics = diagnostics
        };
    }

    private static string BuildCurrentUser()
    {
        var domain = Environment.UserDomainName;
        return string.IsNullOrWhiteSpace(domain)
            ? Environment.UserName
            : $"{domain}\\{Environment.UserName}";
    }
}
