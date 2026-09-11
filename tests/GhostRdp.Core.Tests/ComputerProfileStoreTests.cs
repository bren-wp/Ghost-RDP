using GhostRdp.Core.Profiles;

namespace GhostRdp.Core.Tests;

[TestClass]
public sealed class ComputerProfileStoreTests
{
    private string _directory = null!;
    private string _filePath = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"GhostRdp.Tests.{Guid.NewGuid():N}");
        _filePath = Path.Combine(_directory, "computers.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    [TestMethod]
    public void SaveAndLoad_RoundTripsProfileWithoutPasswordField()
    {
        var store = new ComputerProfileStore(_filePath);
        var profile = new ComputerProfile
        {
            DisplayName = "Office PC",
            Host = "office.internal",
            Username = "marko",
            Domain = "WORK",
            Favorite = true,
            Tags = ["office", "private"]
        };

        store.Save([profile]);
        var json = File.ReadAllText(_filePath);
        var loaded = store.Load();

        Assert.AreEqual(1, loaded.Count);
        Assert.AreEqual(profile.Id, loaded[0].Id);
        Assert.AreEqual(ComputerProfile.CurrentSchemaVersion, loaded[0].SchemaVersion);
        Assert.IsFalse(json.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("credential", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Load_CorruptedJson_ThrowsAndDoesNotModifyFile()
    {
        Directory.CreateDirectory(_directory);
        const string corrupted = "{ not valid json";
        File.WriteAllText(_filePath, corrupted);
        var store = new ComputerProfileStore(_filePath);

        Assert.ThrowsException<InvalidDataException>(() => store.Load());
        Assert.AreEqual(corrupted, File.ReadAllText(_filePath));
    }

    [TestMethod]
    public void Load_UnsupportedStoreSchema_IsRejected()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_filePath, "{\"schemaVersion\":99,\"computers\":[]}");
        var store = new ComputerProfileStore(_filePath);

        Assert.ThrowsException<InvalidDataException>(() => store.Load());
    }

    [TestMethod]
    public void Save_DuplicateProfileIds_IsRejected()
    {
        var first = new ComputerProfile { DisplayName = "A", Host = "pc-a" };
        var second = new ComputerProfile { DisplayName = "B", Host = "pc-b", Id = first.Id };
        var store = new ComputerProfileStore(_filePath);

        Assert.ThrowsException<InvalidDataException>(() => store.Save([first, second]));
        Assert.IsFalse(File.Exists(_filePath));
    }
}
