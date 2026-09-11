using GhostRdp.App.Settings;

namespace GhostRdp.App.Tests;

[TestClass]
public sealed class AppSettingsStoreTests
{
    private string _directory = null!;
    private string _filePath = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "GhostRdp.App.Tests", Guid.NewGuid().ToString("N"));
        _filePath = Path.Combine(_directory, "settings.json");
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
    public void Load_MissingFile_ReturnsSafeDefaults()
    {
        var settings = new AppSettingsStore(_filePath).Load();

        Assert.AreEqual(StartupView.Home, settings.StartupView);
        Assert.AreEqual(ComputerSortPreference.Name, settings.DefaultComputerSort);
        Assert.IsTrue(settings.RememberLastView);
        Assert.IsFalse(File.Exists(_filePath));
    }

    [TestMethod]
    public void SaveAndLoad_RoundTripsOnlyNonSensitivePreferences()
    {
        var store = new AppSettingsStore(_filePath);
        var settings = new AppSettings
        {
            StartupView = StartupView.Computers,
            DefaultComputerSort = ComputerSortPreference.FavoritesFirst,
            RememberLastView = true,
            LastView = StartupView.QuickConnect
        };

        store.Save(settings);
        var loaded = store.Load();
        var json = File.ReadAllText(_filePath);

        Assert.AreEqual(settings.StartupView, loaded.StartupView);
        Assert.AreEqual(settings.DefaultComputerSort, loaded.DefaultComputerSort);
        Assert.AreEqual(settings.LastView, loaded.LastView);
        Assert.IsFalse(json.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("credential", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains("host", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Load_CorruptedJson_ThrowsWithoutRewritingFile()
    {
        Directory.CreateDirectory(_directory);
        const string corrupted = "{ definitely-not-json";
        File.WriteAllText(_filePath, corrupted);
        var store = new AppSettingsStore(_filePath);

        Assert.ThrowsException<InvalidDataException>(() => store.Load());
        Assert.AreEqual(corrupted, File.ReadAllText(_filePath));
    }

    [TestMethod]
    public void Load_UnsupportedFutureSchema_ThrowsWithoutRewritingFile()
    {
        Directory.CreateDirectory(_directory);
        const string future = "{\"schemaVersion\":99,\"startupView\":\"Home\",\"defaultComputerSort\":\"Name\",\"rememberLastView\":true,\"lastView\":\"Home\"}";
        File.WriteAllText(_filePath, future);
        var store = new AppSettingsStore(_filePath);

        Assert.ThrowsException<InvalidDataException>(() => store.Load());
        Assert.AreEqual(future, File.ReadAllText(_filePath));
    }
}
