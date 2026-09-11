using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GhostRdp.Core.Host;
using GhostRdp.Host.Diagnostics;

namespace GhostRdp.Host;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        RefreshDiagnostics();
    }

    private void RefreshDiagnosticsButton_Click(object sender, RoutedEventArgs e) => RefreshDiagnostics();

    private void RefreshDiagnostics()
    {
        HostReadinessSnapshot snapshot;
        try
        {
            snapshot = WindowsHostReadinessProbe.Capture();
        }
        catch (Exception exception)
        {
            snapshot = new HostReadinessSnapshot
            {
                ComputerName = Environment.MachineName,
                Hostname = Environment.MachineName,
                WindowsEdition = Environment.OSVersion.VersionString,
                WindowsVersion = Environment.OSVersion.VersionString,
                CurrentUser = Environment.UserName,
                Diagnostics = [$"Unexpected diagnostic failure: {exception.Message}"]
            };
        }

        var readiness = HostReadinessEvaluator.Evaluate(snapshot);
        RenderSnapshot(snapshot, readiness);
    }

    private void RenderSnapshot(HostReadinessSnapshot snapshot, HostReadinessResult readiness)
    {
        ComputerNameText.Text = ValueOrUnknown(snapshot.ComputerName);
        HostnameText.Text = ValueOrUnknown(snapshot.Hostname);
        WindowsEditionText.Text = ValueOrUnknown(snapshot.WindowsEdition);
        WindowsVersionText.Text = ValueOrUnknown(snapshot.WindowsVersion);
        CurrentUserText.Text = ValueOrUnknown(snapshot.CurrentUser);

        SetBooleanStatus(EditionSupportText, snapshot.EditionSupportsIncomingRdp, "Supported", "Unsupported");
        SetBooleanStatus(RdpEnabledText, snapshot.RdpEnabled, "Enabled", "Disabled");
        SetServiceStatus(RdpServiceText, snapshot.RdpServiceState);
        RdpPortText.Text = snapshot.RdpPort?.ToString(CultureInfo.InvariantCulture) ?? "Unknown";
        RdpPortText.Foreground = snapshot.RdpPort is null ? GetBrush("MutedBrush") : GetBrush("TextBrush");
        SetBooleanStatus(NlaText, snapshot.NlaEnabled, "Enabled", "Disabled");

        SetBooleanStatus(FirewallEnabledText, snapshot.FirewallEnabled, "Enabled on active profiles", "Disabled on an active profile");
        SetBooleanStatus(FirewallBlockAllText, snapshot.FirewallBlocksAllInbound, "Enabled", "Disabled", trueMeansWarning: true);
        SetBooleanStatus(FirewallInboundText, snapshot.RdpFirewallRuleAvailable, "Available for active profiles", "Unavailable");
        NetworkProfilesText.Text = ValueOrUnknown(snapshot.NetworkProfiles);
        NetworkProfilesText.Foreground = snapshot.NetworkProfiles.Contains("Public", StringComparison.OrdinalIgnoreCase)
            ? GetBrush("WarningBrush")
            : GetBrush("TextBrush");

        LanAddressesText.Text = snapshot.LanAddresses.Count == 0
            ? "No active LAN addresses detected"
            : string.Join(Environment.NewLine, snapshot.LanAddresses);
        PrivateNetworkText.Text = snapshot.PrivateNetworkIndicators.Count == 0
            ? "No active VPN/private overlay adapter detected"
            : string.Join(Environment.NewLine, snapshot.PrivateNetworkIndicators);

        DiagnosticsText.Text = snapshot.Diagnostics.Count == 0
            ? "All requested diagnostic sources responded."
            : string.Join(Environment.NewLine, snapshot.Diagnostics.Select(message => $"• {message}"));

        ReadinessTitleText.Text = readiness.Title;
        ReadinessDetailText.Text = readiness.Detail;
        ReadinessBadgeText.Text = readiness.Level.ToString().ToUpperInvariant();
        var readinessBrush = GetReadinessBrush(readiness.Level);
        ReadinessAccentBorder.Background = readinessBrush;
        ReadinessBadgeText.Foreground = readinessBrush;
        LastCheckedText.Text = $"Checked {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}";
    }

    private void SetBooleanStatus(
        TextBlock textBlock,
        bool? value,
        string trueText,
        string falseText,
        bool trueMeansWarning = false)
    {
        textBlock.Text = value switch
        {
            true => trueText,
            false => falseText,
            null => "Unknown"
        };

        textBlock.Foreground = value switch
        {
            true when trueMeansWarning => GetBrush("WarningBrush"),
            true => GetBrush("SuccessBrush"),
            false when trueMeansWarning => GetBrush("SuccessBrush"),
            false => GetBrush("DangerBrush"),
            null => GetBrush("MutedBrush")
        };
    }

    private void SetServiceStatus(TextBlock textBlock, HostServiceState state)
    {
        textBlock.Text = state switch
        {
            HostServiceState.StartPending => "Start pending",
            HostServiceState.StopPending => "Stop pending",
            HostServiceState.ContinuePending => "Continue pending",
            HostServiceState.PausePending => "Pause pending",
            _ => state.ToString()
        };

        textBlock.Foreground = state switch
        {
            HostServiceState.Running => GetBrush("SuccessBrush"),
            HostServiceState.Unknown => GetBrush("MutedBrush"),
            HostServiceState.StartPending or HostServiceState.ContinuePending => GetBrush("WarningBrush"),
            _ => GetBrush("DangerBrush")
        };
    }

    private Brush GetReadinessBrush(HostReadinessLevel level) => level switch
    {
        HostReadinessLevel.Ready => GetBrush("SuccessBrush"),
        HostReadinessLevel.Warning => GetBrush("WarningBrush"),
        HostReadinessLevel.NotReady or HostReadinessLevel.Unsupported => GetBrush("DangerBrush"),
        _ => GetBrush("MutedBrush")
    };

    private Brush GetBrush(string key) => (Brush)FindResource(key);

    private static string ValueOrUnknown(string value) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
}
