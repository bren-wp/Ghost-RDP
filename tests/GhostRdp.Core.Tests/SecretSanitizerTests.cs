using GhostRdp.Core.Security;

namespace GhostRdp.Core.Tests;

[TestClass]
public sealed class SecretSanitizerTests
{
    [DataTestMethod]
    [DataRow("password=hunter2", "password=[REDACTED]")]
    [DataRow("token=abc123 next", "token=[REDACTED] next")]
    [DataRow("pairing-code=998877; host=pc", "pairing-code=[REDACTED]; host=pc")]
    public void Sanitize_RedactsSecretAssignments(string input, string expected) => Assert.AreEqual(expected, SecretSanitizer.Sanitize(input));
}
