using GhostRdp.Core.Runtime;

namespace GhostRdp.Core.Tests;

[TestClass]
public sealed class TemporaryRdpFileTests
{
    [TestMethod]
    public void Create_WritesUniqueRdpFileAndDisposeRemovesSessionDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "GhostRdp.Tests", Guid.NewGuid().ToString("N"));
        var request = new RdpConnectionRequest("office.example", 3389, "alice", "CORP");
        TemporaryRdpFile? temporaryFile = null;

        try
        {
            temporaryFile = TemporaryRdpFile.Create(request, root);
            var filePath = temporaryFile.FilePath;
            var sessionDirectory = temporaryFile.SessionDirectory;

            Assert.IsTrue(File.Exists(filePath));
            Assert.IsTrue(filePath.EndsWith(".rdp", StringComparison.OrdinalIgnoreCase));
            var content = File.ReadAllText(filePath);
            Assert.IsFalse(content.Contains("password", StringComparison.OrdinalIgnoreCase));

            temporaryFile.Dispose();
            temporaryFile = null;

            Assert.IsFalse(File.Exists(filePath));
            Assert.IsFalse(Directory.Exists(sessionDirectory));
        }
        finally
        {
            temporaryFile?.Dispose();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [TestMethod]
    public void CleanupStaleSessions_RemovesOnlyOldOwnedSessionDirectories()
    {
        var root = Path.Combine(Path.GetTempPath(), "GhostRdp.Tests", Guid.NewGuid().ToString("N"));
        var oldSession = Path.Combine(root, Guid.NewGuid().ToString("N"));
        var recentSession = Path.Combine(root, Guid.NewGuid().ToString("N"));
        var unrelatedDirectory = Path.Combine(root, "unrelated-cache");

        try
        {
            Directory.CreateDirectory(oldSession);
            Directory.CreateDirectory(recentSession);
            Directory.CreateDirectory(unrelatedDirectory);
            File.WriteAllText(Path.Combine(oldSession, "old.rdp"), "test");
            File.WriteAllText(Path.Combine(recentSession, "recent.rdp"), "test");
            File.WriteAllText(Path.Combine(unrelatedDirectory, "keep.txt"), "test");
            Directory.SetLastWriteTimeUtc(oldSession, DateTime.UtcNow.AddDays(-2));
            Directory.SetLastWriteTimeUtc(recentSession, DateTime.UtcNow);
            Directory.SetLastWriteTimeUtc(unrelatedDirectory, DateTime.UtcNow.AddDays(-2));

            var deleted = TemporaryRdpFile.CleanupStaleSessions(TimeSpan.FromHours(24), root);

            Assert.AreEqual(1, deleted);
            Assert.IsFalse(Directory.Exists(oldSession));
            Assert.IsTrue(Directory.Exists(recentSession));
            Assert.IsTrue(Directory.Exists(unrelatedDirectory));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
