using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GhostRdp.Host.Diagnostics;

internal sealed record NetworkDiagnosticsSnapshot(
    string Hostname,
    IReadOnlyList<string> LanAddresses,
    IReadOnlyList<string> PrivateNetworkIndicators);

internal static class WindowsNetworkDiagnostics
{
    private static readonly string[] PrivateNetworkMarkers = ["tailscale", "wireguard", "zerotier", "vpn"];

    public static NetworkDiagnosticsSnapshot Capture(ICollection<string> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        var hostname = Environment.MachineName;
        try
        {
            hostname = Dns.GetHostName();
        }
        catch (SocketException exception)
        {
            diagnostics.Add($"Hostname lookup failed: {exception.Message}");
        }

        try
        {
            var activeInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(networkInterface =>
                    networkInterface.OperationalStatus == OperationalStatus.Up
                    && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToArray();

            var addresses = activeInterfaces
                .SelectMany(networkInterface => networkInterface.GetIPProperties().UnicastAddresses)
                .Select(unicast => unicast.Address)
                .Where(IsUsefulLocalAddress)
                .Select(address => address.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var privateNetworkIndicators = activeInterfaces
                .Where(IsPrivateNetworkCandidate)
                .Select(networkInterface => $"{networkInterface.Name} · {networkInterface.Description}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new NetworkDiagnosticsSnapshot(hostname, addresses, privateNetworkIndicators);
        }
        catch (NetworkInformationException exception)
        {
            diagnostics.Add($"Network adapter diagnostics failed: {exception.Message}");
            return new NetworkDiagnosticsSnapshot(hostname, Array.Empty<string>(), Array.Empty<string>());
        }
    }

    private static bool IsUsefulLocalAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return true;
        }

        return address.AddressFamily == AddressFamily.InterNetworkV6
            && !address.IsIPv6Multicast
            && !address.IsIPv6LinkLocal;
    }

    private static bool IsPrivateNetworkCandidate(NetworkInterface networkInterface)
    {
        if (networkInterface.NetworkInterfaceType is NetworkInterfaceType.Tunnel or NetworkInterfaceType.Ppp)
        {
            return true;
        }

        var identity = $"{networkInterface.Name} {networkInterface.Description}";
        return PrivateNetworkMarkers.Any(marker => identity.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
