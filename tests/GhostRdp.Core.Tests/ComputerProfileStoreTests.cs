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
    public void SaveAndLoad_RoundTripsGatewayProfileWithoutPasswordField()
    {
        var store = new ComputerProfileStore(_filePath);
        var profile = new ComputerProfile
        {
            DisplayName = "Office PC",
            Host = "office.internal",
            Username = "marko",
            Domain = "WORK",
            RemoteAccessMode = RemoteAccessMode.RdGateway,
            GatewayHost = "gateway.example.test",
            Favorite = true,
            Tags = ["office", "private"]
        };

        store.Save([profile]);
        var json = File.ReadAllText(_filePath);
        var loaded = store.Load();

        Assert.AreEqual(1, loaded.Count);
        Assert.AreEqual(profile.Id, loaded[0].Id);
        Assert.AreEqual(ComputerProfile.CurrentSchemaVersion, loaded[0].SchemaVersion);
        Assert.AreEqual(RemoteAccessMode.RdGateway, loaded[0].RemoteAccessMode);
        Assert.AreEqual("gateway.example.test", loaded[0].GatewayHost);
        Assert.IsFalse(json.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("credential", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(File.Exists(store.BackupFilePath));
    }

    [TestMethod]
    public void Save_SecondWrite_PreservesPreviousValidatedStoreAsBackup()
    {
        var store = new ComputerProfileStore(_filePath);
        var first = new ComputerProfile { DisplayName = "First", Host = "first.internal" };
        var second = new ComputerProfile { DisplayName = "Second", Host = "second.internal" };

        store.Save([first]);
        var firstJson = File.ReadAllText(_filePath);
        store.Save([second]);

        Assert.IsTrue(File.Exists(store.BackupFilePath));
        Assert.AreEqual(firstJson, File.ReadAllText(store.BackupFilePath));
        Assert.IsTrue(store.HasRecoverableBackup());

        var backupStore = new ComputerProfileStore(store.BackupFilePath);
        var backupProfiles = backupStore.Load();
        Assert.AreEqual(1, backupProfiles.Count);
        Assert.AreEqual(first.Id, backupProfiles[0].Id);
        Assert.AreEqual("First", backupProfiles[0].DisplayName);
    }

    [TestMethod]
    public void Save_CorruptedPrimary_ThrowsAndPreservesPrimaryAndBackup()
    {
        var store = new ComputerProfileStore(_filePath);
        var first = new ComputerProfile { DisplayName = "First", Host = "first.internal" };
        var second = new ComputerProfile { DisplayName = "Second", Host = "second.internal" };
        var third = new ComputerProfile { DisplayName = "Third", Host = "third.internal" };

        store.Save([first]);
        store.Save([second]);
        var backupBefore = File.ReadAllText(store.BackupFilePath);
        const string corrupted = "{ not valid json";
        File.WriteAllText(_filePath, corrupted);

        Assert.ThrowsException<InvalidDataException>(() => store.Save([third]));
        Assert.AreEqual(corrupted, File.ReadAllText(_filePath));
        Assert.AreEqual(backupBefore, File.ReadAllText(store.BackupFilePath));
    }

    [TestMethod]
    public void RestoreBackup_CorruptedPrimary_PreservesOriginalAndRestoresValidatedBackup()
    {
        var store = new ComputerProfileStore(_filePath);
        var first = new ComputerProfile { DisplayName = "First", Host = "first.internal" };
        var second = new ComputerProfile { DisplayName = "Second", Host = "second.internal" };

        store.Save([first]);
        store.Save([second]);
        const string corrupted = "{ broken current store";
        File.WriteAllText(_filePath, corrupted);

        Assert.IsTrue(store.HasRecoverableBackup());
        var recovery = store.RestoreBackup();

        Assert.AreEqual(store.BackupFilePath, recovery.BackupFilePath);
        Assert.IsNotNull(recovery.PreservedOriginalFilePath);
        Assert.IsTrue(File.Exists(recovery.PreservedOriginalFilePath));
        Assert.AreEqual(corrupted, File.ReadAllText(recovery.PreservedOriginalFilePath));
        Assert.AreEqual(1, recovery.Profiles.Count);
        Assert.AreEqual(first.Id, recovery.Profiles[0].Id);

        var restored = store.Load();
        Assert.AreEqual(1, restored.Count);
        Assert.AreEqual(first.Id, restored[0].Id);
        Assert.IsTrue(store.HasRecoverableBackup());
    }

    [TestMethod]
    public void RestoreBackup_CorruptedBackup_IsRejectedWithoutModifyingPrimary()
    {
        var store = new ComputerProfileStore(_filePath);
        var first = new ComputerProfile { DisplayName = "First", Host = "first.internal" };
        var second = new ComputerProfile { DisplayName = "Second", Host = "second.internal" };

        store.Save([first]);
        store.Save([second]);
        const string corruptedPrimary = "{ broken primary";
        const string corruptedBackup = "{ broken backup";
        File.WriteAllText(_filePath, corruptedPrimary);
        File.WriteAllText(store.BackupFilePath, corruptedBackup);

        Assert.IsFalse(store.HasRecoverableBackup());
        Assert.ThrowsException<InvalidDataException>(() => store.RestoreBackup());
        Assert.AreEqual(corruptedPrimary, File.ReadAllText(_filePath));
        Assert.AreEqual(corruptedBackup, File.ReadAllText(store.BackupFilePath));
    }

    [TestMethod]
    public void RestoreBackup_ValidPrimary_IsRejectedWithoutRollingBackData()
    {
        var store = new ComputerProfileStore(_filePath);
        var first = new ComputerProfile { DisplayName = "First", Host = "first.internal" };
        var second = new ComputerProfile { DisplayName = "Second", Host = "second.internal" };

        store.Save([first]);
        store.Save([second]);
        var currentJson = File.ReadAllText(_filePath);

        Assert.ThrowsException<InvalidOperationException>(() => store.RestoreBackup());
        Assert.AreEqual(currentJson, File.ReadAllText(_filePath));
        var loaded = store.Load();
        Assert.AreEqual(second.Id, loaded.Single().Id);
    }

    [TestMethod]
    public void Load_V1Store_MigratesInMemoryWithoutModifyingSourceFile()
    {
        Directory.CreateDirectory(_directory);
        var id = Guid.NewGuid();
        var legacy = $$"""
            {
              "schemaVersion": 1,
              "computers": [
                {
                  "schemaVersion": 1,
                  "id": "{{id}}",
                  "displayName": "Legacy PC",
                  "host": "legacy.internal",
                  "port": 3389,
                  "username": "legacy",
                  "domain": "WORK",
                  "notes": "",
                  "favorite": false,
                  "tags": []
                }
              ]
            }
            """;
        File.WriteAllText(_filePath, legacy);
        var store = new ComputerProfileStore(_filePath);

        var loaded = store.Load();

        Assert.AreEqual(1, loaded.Count);
        Assert.AreEqual(ComputerProfile.CurrentSchemaVersion, loaded[0].SchemaVersion);
        Assert.AreEqual(RemoteAccessMode.Direct, loaded[0].RemoteAccessMode);
        Assert.AreEqual(string.Empty, loaded[0].GatewayHost);
        Assert.AreEqual(legacy, File.ReadAllText(_filePath));
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
    public void Load_UnsupportedProfileSchema_IsRejected()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            _filePath,
            "{\"schemaVersion\":2,\"computers\":[{\"schemaVersion\":99,\"id\":\"11111111-1111-1111-1111-111111111111\",\"displayName\":\"PC\",\"host\":\"pc\",\"port\":3389,\"username\":\"\",\"domain\":\"\",\"remoteAccessMode\":\"Direct\",\"gatewayHost\":\"\",\"notes\":\"\",\"favorite\":false,\"tags\":[]}]}");
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
