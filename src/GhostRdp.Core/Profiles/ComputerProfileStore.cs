using System.Text.Json;
using System.Text.Json.Serialization;

namespace GhostRdp.Core.Profiles;

public sealed class ComputerProfileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ComputerProfileStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Profile store path is required.", nameof(filePath));
        }

        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public IReadOnlyList<ComputerProfile> Load()
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var document = JsonSerializer.Deserialize<ProfileStoreDocument>(json, SerializerOptions)
                ?? throw new InvalidDataException("Profile store is empty or invalid.");

            if (document.SchemaVersion != ProfileStoreDocument.CurrentSchemaVersion)
            {
                throw new InvalidDataException($"Unsupported profile store schema version: {document.SchemaVersion}.");
            }

            document.Computers ??= [];
            var validation = ComputerProfileValidator.ValidateCollection(document.Computers);
            if (!validation.IsValid)
            {
                throw new InvalidDataException(validation.Error);
            }

            return document.Computers;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Profile store contains invalid JSON and was not modified.", exception);
        }
    }

    public void Save(IEnumerable<ComputerProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        var computerList = profiles.ToList();
        var validation = ComputerProfileValidator.ValidateCollection(computerList);
        if (!validation.IsValid)
        {
            throw new InvalidDataException(validation.Error);
        }

        var document = new ProfileStoreDocument { Computers = computerList };
        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException("Profile store directory could not be determined.");
        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            var json = JsonSerializer.Serialize(document, SerializerOptions);
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
        return Path.Combine(root, "Ghost RDP", "computers.json");
    }
}
