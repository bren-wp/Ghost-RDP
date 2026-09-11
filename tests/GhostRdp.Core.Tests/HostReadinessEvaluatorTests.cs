using GhostRdp.Core.Host;

namespace GhostRdp.Core.Tests;

[TestClass]
public sealed class HostReadinessEvaluatorTests
{
    [TestMethod]
    public void Evaluate_AllRequiredSignalsReady_ReturnsReady()
    {
        var result = HostReadinessEvaluator.Evaluate(CreateReadySnapshot());

        Assert.AreEqual(HostReadinessLevel.Ready, result.Level);
        Assert.AreEqual("Ready for Remote Desktop", result.Title);
    }

    [TestMethod]
    public void Evaluate_UnsupportedEdition_ReturnsUnsupported()
    {
        var snapshot = CreateReadySnapshot() with { EditionSupportsIncomingRdp = false };

        var result = HostReadinessEvaluator.Evaluate(snapshot);

        Assert.AreEqual(HostReadinessLevel.Unsupported, result.Level);
        StringAssert.Contains(result.Title, "does not support incoming RDP");
    }

    [TestMethod]
    public void Evaluate_RdpDisabled_ReturnsNotReady()
    {
        var snapshot = CreateReadySnapshot() with { RdpEnabled = false };

        var result = HostReadinessEvaluator.Evaluate(snapshot);

        Assert.AreEqual(HostReadinessLevel.NotReady, result.Level);
        Assert.AreEqual("Remote Desktop disabled", result.Title);
    }

    [TestMethod]
    public void Evaluate_MissingFirewallRule_ReturnsNotReady()
    {
        var snapshot = CreateReadySnapshot() with { RdpFirewallRuleAvailable = false };

        var result = HostReadinessEvaluator.Evaluate(snapshot);

        Assert.AreEqual(HostReadinessLevel.NotReady, result.Level);
        Assert.AreEqual("Firewall rule unavailable", result.Title);
    }

    [TestMethod]
    public void Evaluate_NlaDisabled_ReturnsWarning()
    {
        var snapshot = CreateReadySnapshot() with { NlaEnabled = false };

        var result = HostReadinessEvaluator.Evaluate(snapshot);

        Assert.AreEqual(HostReadinessLevel.Warning, result.Level);
        StringAssert.Contains(result.Detail, "Network Level Authentication");
    }

    [TestMethod]
    public void Evaluate_UnknownRequiredValue_DoesNotClaimReady()
    {
        var snapshot = CreateReadySnapshot() with { FirewallEnabled = null };

        var result = HostReadinessEvaluator.Evaluate(snapshot);

        Assert.AreEqual(HostReadinessLevel.Unknown, result.Level);
    }

    [TestMethod]
    public void Evaluate_PublicProfile_ReturnsWarning()
    {
        var snapshot = CreateReadySnapshot() with { NetworkProfiles = "Private, Public" };

        var result = HostReadinessEvaluator.Evaluate(snapshot);

        Assert.AreEqual(HostReadinessLevel.Warning, result.Level);
        StringAssert.Contains(result.Title, "public network profile");
    }

    private static HostReadinessSnapshot CreateReadySnapshot() => new()
    {
        ComputerName = "DESKTOP-TEST",
        Hostname = "desktop-test",
        WindowsEdition = "Windows 11 Pro",
        WindowsVersion = "24H2 build 26100",
        CurrentUser = "TEST\\alice",
        EditionSupportsIncomingRdp = true,
        RdpEnabled = true,
        RdpServiceState = HostServiceState.Running,
        RdpPort = 3389,
        NlaEnabled = true,
        FirewallEnabled = true,
        FirewallBlocksAllInbound = false,
        RdpFirewallRuleAvailable = true,
        NetworkProfiles = "Private"
    };
}
