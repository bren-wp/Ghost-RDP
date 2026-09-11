using GhostRdp.Core.Host;

namespace GhostRdp.Core.Tests;

[TestClass]
public sealed class FirewallPortMatcherTests
{
    [DataTestMethod]
    [DataRow("3389", 3389)]
    [DataRow("80,3389,443", 3389)]
    [DataRow("3380-3390", 3389)]
    [DataRow("*", 3389)]
    [DataRow("Any", 3389)]
    public void CoversPort_MatchingSpecification_ReturnsTrue(string specification, int port) =>
        Assert.IsTrue(FirewallPortMatcher.CoversPort(specification, port));

    [DataTestMethod]
    [DataRow("3390", 3389)]
    [DataRow("3380-3388", 3389)]
    [DataRow("RPC", 3389)]
    [DataRow("", 3389)]
    public void CoversPort_NonMatchingSpecification_ReturnsFalse(string specification, int port) =>
        Assert.IsFalse(FirewallPortMatcher.CoversPort(specification, port));

    [TestMethod]
    public void CoversPort_InvalidPort_ReturnsFalse() =>
        Assert.IsFalse(FirewallPortMatcher.CoversPort("*", 0));
}
