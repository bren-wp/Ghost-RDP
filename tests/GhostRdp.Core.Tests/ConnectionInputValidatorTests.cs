using GhostRdp.Core.Validation;

namespace GhostRdp.Core.Tests;

[TestClass]
public sealed class ConnectionInputValidatorTests
{
    [DataTestMethod]
    [DataRow(1)]
    [DataRow(3389)]
    [DataRow(65535)]
    public void Port_InRange_IsValid(int port) => Assert.IsTrue(ConnectionInputValidator.ValidatePort(port).IsValid);

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(65536)]
    [DataRow(-1)]
    public void Port_OutOfRange_IsRejected(int port) => Assert.IsFalse(ConnectionInputValidator.ValidatePort(port).IsValid);

    [DataTestMethod]
    [DataRow("desktop.example.test")]
    [DataRow("10.10.0.15")]
    [DataRow("2001:db8::10")]
    [DataRow("[2001:db8::10]")]
    [DataRow("localhost")]
    public void Host_ValidValues_AreAccepted(string host) => Assert.IsTrue(ConnectionInputValidator.ValidateHost(host).IsValid);

    [DataTestMethod]
    [DataRow("server; calc.exe")]
    [DataRow("server && whoami")]
    [DataRow("host|powershell")]
    [DataRow(" host")]
    [DataRow("host ")]
    [DataRow("-bad.example")]
    public void Host_InjectionOrMalformedValues_AreRejected(string host) => Assert.IsFalse(ConnectionInputValidator.ValidateHost(host).IsValid);
}
