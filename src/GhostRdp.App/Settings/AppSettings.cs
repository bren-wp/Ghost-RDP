namespace GhostRdp.App.Settings;

public enum StartupView
{
    Home = 0,
    Computers = 1,
    QuickConnect = 2
}

public enum ComputerSortPreference
{
    Name = 0,
    Host = 1,
    FavoritesFirst = 2
}

public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public StartupView StartupView { get; set; } = StartupView.Home;

    public ComputerSortPreference DefaultComputerSort { get; set; } = ComputerSortPreference.Name;

    public bool RememberLastView { get; set; } = true;

    public StartupView LastView { get; set; } = StartupView.Home;

    public AppSettings Clone() => new()
    {
        SchemaVersion = SchemaVersion,
        StartupView = StartupView,
        DefaultComputerSort = DefaultComputerSort,
        RememberLastView = RememberLastView,
        LastView = LastView
    };

    public static AppSettings CreateDefault() => new();
}
