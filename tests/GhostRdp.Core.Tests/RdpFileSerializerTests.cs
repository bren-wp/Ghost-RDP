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
