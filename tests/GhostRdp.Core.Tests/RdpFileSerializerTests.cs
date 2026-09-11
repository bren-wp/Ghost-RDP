using GhostRdp.Core.Profiles;
using GhostRdp.Core.Runtime;

namespace GhostRdp.Core.Tests;

[TestClass]
public sealed class RdpFileSerializerTests
{
    [TestMethod]
    public void Serialize_WritesAddressUsernameAndStrictAuthenticationSettings()
    {
        var request = new RdpConnectionRequest("office.example", 3390, "alice", "CORP");

        var content = RdpFileSerializer.Serialize(request);

        StringAssert.Contains(content, "full address:s:office.example:3390\r\n");
        StringAssert.Contains(content, "username:s:CORP\\alice\r\n");
        StringAssert.Contains(content, "prompt for credentials:i:1\r\n");
        StringAssert.Contains(content, "authentication level:i:1\r\n");
        StringAssert.Contains(content, "enablecredsspsupport:i:1\r\n");
        Assert.IsFalse(content.Contains("authentication level:i:0", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(content.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(content.Contains("gatewayhostname", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Serialize_RdGateway_WritesExplicitGatewayWithoutPasswordOrCredentialMaterial()
    {
        var request = new RdpConnectionRequest(
            "office.internal",
            3389,
            "alice",
            "CORP",
            RemoteAccessMode.RdGateway,
            "gateway.example.test");

        var content = RdpFileSerializer.Serialize(request);

        StringAssert.Contains(content, "gatewayhostname:s:gateway.example.test\r\n");
        StringAssert.Contains(content, "gatewayusagemethod:i:1\r\n");
        StringAssert.Contains(content, "gatewayprofileusagemethod:i:1\r\n");
        StringAssert.Contains(content, "gatewaycredentialssource:i:4\r\n");
        StringAssert.Contains(content, "promptcredentialonce:i:0\r\n");
        Assert.IsFalse(content.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(content.Contains("gatewayaccesstoken", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Serialize_PrivateNetwork_DoesNotInventGatewaySettings()
    {
        var request = new RdpConnectionRequest(
            "100.90.80.70",
            3389,
            string.Empty,
            string.Empty,
            RemoteAccessMode.PrivateNetwork);

        var content = RdpFileSerializer.Serialize(request);

        StringAssert.Contains(content, "full address:s:100.90.80.70:3389\r\n");
        Assert.IsFalse(content.Contains("gateway", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Serialize_RdGateway_RejectsInvalidGatewayHost()
    {
        var request = new RdpConnectionRequest(
            "office.internal",
            3389,
            string.Empty,
            string.Empty,
            RemoteAccessMode.RdGateway,
            "gateway.example & calc.exe");

        Assert.ThrowsException<ArgumentException>(() => RdpFileSerializer.Serialize(request));
    }

    [TestMethod]
    public void Serialize_BracketsIpv6AddressBeforeAppendingPort()
    {
        var request = new RdpConnectionRequest("2001:db8::10", 3389, string.Empty, string.Empty);

        var content = RdpFileSerializer.Serialize(request);

        StringAssert.Contains(content, "full address:s:[2001:db8::10]:3389\r\n");
    }

    [TestMethod]
    public void Serialize_RejectsShellLikeInvalidHostInsteadOfEmbeddingIt()
    {
        var request = new RdpConnectionRequest("server.example & calc.exe", 3389, string.Empty, string.Empty);

        Assert.ThrowsException<ArgumentException>(() => RdpFileSerializer.Serialize(request));
    }
}
