using System.Globalization;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace GhostRdp.Host.Diagnostics;

internal sealed record RegistryRdpSnapshot(
    string WindowsEdition,
    string WindowsVersion,
    bool? EditionSupportsIncomingRdp,
    bool? RdpEnabled,
    int? RdpPort,
    bool? NlaEnabled);

internal static class WindowsRegistryDiagnostics
{
    private const string WindowsVersionPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
    private const string TerminalServerPath = @"SYSTEM\CurrentControlSet\Control\Terminal Server";
    private const string RdpTcpPath = @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp";

    public static RegistryRdpSnapshot Capture(ICollection<string> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        if (!OperatingSystem.IsWindows())
        {
            diagnostics.Add("Windows registry diagnostics are unavailable on this platform.");
            return UnknownSnapshot();
        }

        try
        {
            using var localMachine = RegistryKey.OpenBaseKey(
                RegistryHive.LocalMachine,
                Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32);

            using var versionKey = localMachine.OpenSubKey(WindowsVersionPath, false);
            var productName = GetString(versionKey, "ProductName") ?? Environment.OSVersion.VersionString;
            var editionId = GetString(versionKey, "EditionID") ?? string.Empty;
            var displayVersion = GetString(versionKey, "DisplayVersion") ?? GetString(versionKey, "ReleaseId");
            var build = GetString(versionKey, "CurrentBuildNumber") ?? GetString(versionKey, "CurrentBuild");
            var updateBuildRevision = GetInteger(versionKey, "UBR");

            using var terminalServerKey = localMachine.OpenSubKey(TerminalServerPath, false);
            var denyConnections = GetInteger(terminalServerKey, "fDenyTSConnections");
            bool? rdpEnabled = denyConnections switch
            {
                0 => true,
                1 => false,
                _ => null
            };

            using var rdpTcpKey = localMachine.OpenSubKey(RdpTcpPath, false);
            var portNumber = GetInteger(rdpTcpKey, "PortNumber");
            int? rdpPort = portNumber is >= 1 and <= 65535 ? portNumber : null;
            var userAuthentication = GetInteger(rdpTcpKey, "UserAuthentication");
            bool? nlaEnabled = userAuthentication switch
            {
                1 => true,
                0 => false,
                _ => null
            };

            return new RegistryRdpSnapshot(
                productName,
                BuildVersionLabel(displayVersion, build, updateBuildRevision),
                SupportsIncomingRdp(productName, editionId),
                rdpEnabled,
                rdpPort,
                nlaEnabled);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or SecurityException)
        {
            diagnostics.Add($"Windows registry diagnostics could not be read: {exception.Message}");
            return UnknownSnapshot();
        }
    }

    private static RegistryRdpSnapshot UnknownSnapshot() => new(
        Environment.OSVersion.VersionString,
        Environment.OSVersion.VersionString,
        null,
        null,
        null,
        null);

    private static string? GetString(RegistryKey? key, string name) => key?.GetValue(name) as string;

    private static int? GetInteger(RegistryKey? key, string name)
    {
        var value = key?.GetValue(name);
        if (value is null)
        {
            return null;
        }

        try
        {
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            return null;
        }
    }

    private static string BuildVersionLabel(string? displayVersion, string? build, int? updateBuildRevision)
    {
        var version = string.IsNullOrWhiteSpace(displayVersion) ? "Version unknown" : displayVersion;
        if (string.IsNullOrWhiteSpace(build))
        {
            return version;
        }

        return updateBuildRevision is null
            ? $"{version} · build {build}"
            : $"{version} · build {build}.{updateBuildRevision.Value.ToString(CultureInfo.InvariantCulture)}";
    }

    private static bool? SupportsIncomingRdp(string productName, string editionId)
    {
        if (productName.Contains("Windows Server", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (productName.Contains("Home", StringComparison.OrdinalIgnoreCase)
            || editionId.Contains("Core", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string[] supportedEditionMarkers = ["Professional", "Enterprise", "Education"];
        if (supportedEditionMarkers.Any(marker => editionId.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return null;
    }
}
