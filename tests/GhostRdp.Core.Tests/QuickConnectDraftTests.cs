using GhostRdp.Core.Profiles;

namespace GhostRdp.Core.Tests;

[TestClass]
public sealed class QuickConnectDraftTests
{
    [TestMethod]
    public void Validate_DoesNotCreateSavedProfileOrFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"GhostRdp.QuickConnect.{Guid.NewGuid():N}");
        var profilePath = Path.Combine(directory, "computers.json");
        var draft = new QuickConnectDraft
        {
            Host = "192.168.10.20",
            Port = 3389,
            Username = "user",
            RemoteAccessMode = RemoteAccessMode.PrivateNetwork
        };

        var validation = draft.Validate();

        Assert.IsTrue(validation.IsValid);
        Assert.IsFalse(File.Exists(profilePath));
        Assert.IsFalse(Directory.Exists(directory));
    }

    [TestMethod]
    public void CreateProfile_IsExplicitAndCopiesRemoteAccessRoute()
    {
        var draft = new QuickConnectDraft
        {
            Host = "server.example.test",
            Port = 3390,
            Username = "marko",
            Domain = "WORK",
            RemoteAccessMode = RemoteAccessMode.RdGateway,
            GatewayHost = "gateway.example.test"
        };

        var profile = draft.CreateProfile("Server");

        Assert.AreNotEqual(Guid.Empty, profile.Id);
        Assert.AreEqual("Server", profile.DisplayName);
        Assert.AreEqual(draft.Host, profile.Host);
        Assert.AreEqual(draft.Port, profile.Port);
        Assert.AreEqual(RemoteAccessMode.RdGateway, profile.RemoteAccessMode);
        Assert.AreEqual("gateway.example.test", profile.GatewayHost);
    }

    [TestMethod]
    public void Validate_RdGatewayWithoutGatewayHost_IsRejected()
    {
        var draft = new QuickConnectDraft
        {
            Host = "server.example.test",
            RemoteAccessMode = RemoteAccessMode.RdGateway
        };

        var validation = draft.Validate();

        Assert.IsFalse(validation.IsValid);
        StringAssert.Contains(validation.Error ?? string.Empty, "RD Gateway");
    }
}
