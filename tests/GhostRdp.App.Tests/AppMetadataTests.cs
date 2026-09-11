namespace GhostRdp.App.Tests;

[TestClass]
public sealed class AppMetadataTests
{
    [TestMethod]
    public void AboutText_UsesGhostRdpBrandAndPrivacyStatement()
    {
        var text = AppMetadata.BuildAboutText();

        StringAssert.Contains(text, "Ghost RDP");
        StringAssert.Contains(text, "No telemetry");
        Assert.IsFalse(text.Contains("Ghost FTP", StringComparison.OrdinalIgnoreCase));
    }
}
