using GhostRdp.Core.Runtime;

namespace GhostRdp.Core.Tests;

[TestClass]
public sealed class MstscProcessStartInfoFactoryTests
{
    [TestMethod]
    public void Create_UsesStructuredArgumentListWithoutShellCommandString()
    {
        var executable = Path.GetFullPath(Path.Combine("runtime", "mstsc.exe"));
        var rdpFile = Path.GetFullPath(Path.Combine("temp", "profile & calc.exe.rdp"));

        var startInfo = MstscProcessStartInfoFactory.Create(executable, rdpFile);

        Assert.AreEqual(executable, startInfo.FileName);
        Assert.IsFalse(startInfo.UseShellExecute);
        Assert.AreEqual(string.Empty, startInfo.Arguments);
        Assert.AreEqual(1, startInfo.ArgumentList.Count);
        Assert.AreEqual(rdpFile, startInfo.ArgumentList[0]);
    }

    [TestMethod]
    public void Create_RejectsRelativePaths()
    {
        Assert.ThrowsException<ArgumentException>(() => MstscProcessStartInfoFactory.Create("mstsc.exe", "connection.rdp"));
    }
}
