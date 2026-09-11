using System.Text.Json;
using System.Text.Json.Serialization;

namespace GhostRdp.App.Settings;

public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AppSettingsStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Settings path is required.", nameof(filePath));
        }

        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public AppSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return AppSettings.CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions)
                ?? throw new InvalidDataException("Settings file is empty or invalid.");
            Validate(settings);
            return settings;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Settings file contains invalid JSON and was not modified.", exception);
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Validate(settings);

        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException("Settings directory could not be determined.");
        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            var json = JsonSerializer.Serialize(settings, SerializerOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, FilePath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public static string GetDefaultFilePath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "Ghost RDP", "settings.json");
    }

    private static void Validate(AppSettings settings)
    {
        if (settings.SchemaVersion != AppSettings.CurrentSchemaVersion)
        {
            throw new InvalidDataException($"Unsupported settings schema version: {settings.SchemaVersion}.");
        }

        if (!Enum.IsDefined(settings.StartupView) || !Enum.IsDefined(settings.LastView))
        {
            throw new InvalidDataException("Settings contain an invalid startup or last-view value.");
        }

        if (!Enum.IsDefined(settings.DefaultComputerSort))
        {
            throw new InvalidDataException("Settings contain an invalid computer-sort value.");
        }
    }
}
