using System.Text.Json;
using System.Text.Json.Serialization;

namespace GhostRdp.Core.Profiles;

public sealed record ProfileStoreRecoveryResult(
    IReadOnlyList<ComputerProfile> Profiles,
    string? PreservedOriginalFilePath,
    string BackupFilePath);

public sealed class ComputerProfileStore
{
    private const int OldestSupportedStoreSchemaVersion = 1;

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
        BackupFilePath = FilePath + ".bak";
    }

    public string FilePath { get; }

    public string BackupFilePath { get; }

    public IReadOnlyList<ComputerProfile> Load()
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }

        return LoadFromPath(FilePath);
    }

    public bool HasRecoverableBackup()
    {
        if (!File.Exists(BackupFilePath))
        {
            return false;
        }

        try
        {
            _ = LoadFromPath(BackupFilePath);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return false;
        }
    }

    public ProfileStoreRecoveryResult RestoreBackup()
    {
        if (!File.Exists(BackupFilePath))
        {
            throw new FileNotFoundException("No saved-computer backup is available.", BackupFilePath);
        }

        var recoveredProfiles = LoadFromPath(BackupFilePath)
            .Select(profile => profile.Clone())
            .ToList();

        if (File.Exists(FilePath))
        {
            try
            {
                _ = LoadFromPath(FilePath);
                throw new InvalidOperationException("The current saved-computer file is valid and does not require recovery.");
            }
            catch (InvalidDataException)
            {
                // The explicit recovery path is only needed when the current store cannot be safely loaded.
            }
        }

        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException("Profile store directory could not be determined.");
        Directory.CreateDirectory(directory);

        var temporaryPath = CreateTemporaryPath(directory, Path.GetFileName(FilePath), "restore");
        string? preservedOriginalPath = null;
        var originalMoved = false;

        try
        {
            var restoredDocument = new ProfileStoreDocument
            {
                Computers = recoveredProfiles.Select(profile => profile.Clone()).ToList()
            };
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(restoredDocument, SerializerOptions));
            _ = LoadFromPath(temporaryPath);

            if (File.Exists(FilePath))
            {
                preservedOriginalPath = CreatePreservedOriginalPath(directory);
                File.Move(FilePath, preservedOriginalPath);
                originalMoved = true;
            }

            File.Move(temporaryPath, FilePath);
        }
        catch
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            if (originalMoved
                && preservedOriginalPath is not null
                && File.Exists(preservedOriginalPath)
                && !File.Exists(FilePath))
            {
                try
                {
                    File.Move(preservedOriginalPath, FilePath);
                    preservedOriginalPath = null;
                }
                catch (IOException)
                {
                    // Preserve the original at its recovery path when Windows cannot move it back immediately.
                }
                catch (UnauthorizedAccessException)
                {
                    // Preserve the original at its recovery path when Windows cannot move it back immediately.
                }
            }

            throw;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        return new ProfileStoreRecoveryResult(recoveredProfiles, preservedOriginalPath, BackupFilePath);
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

        string? previousJson = null;
        if (File.Exists(FilePath))
        {
            previousJson = File.ReadAllText(FilePath);
            _ = LoadFromJson(previousJson);
        }

        var document = new ProfileStoreDocument { Computers = computerList };
        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException("Profile store directory could not be determined.");
        Directory.CreateDirectory(directory);

        var temporaryPath = CreateTemporaryPath(directory, Path.GetFileName(FilePath), "save");
        var backupTemporaryPath = CreateTemporaryPath(directory, Path.GetFileName(BackupFilePath), "backup");
        try
        {
            if (previousJson is not null)
            {
                File.WriteAllText(backupTemporaryPath, previousJson);
                _ = LoadFromPath(backupTemporaryPath);
                File.Move(backupTemporaryPath, BackupFilePath, true);
            }

            var json = JsonSerializer.Serialize(document, SerializerOptions);
            File.WriteAllText(temporaryPath, json);
            _ = LoadFromPath(temporaryPath);
            File.Move(temporaryPath, FilePath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            if (File.Exists(backupTemporaryPath))
            {
                File.Delete(backupTemporaryPath);
            }
        }
    }

    public static string GetDefaultFilePath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "Ghost RDP", "computers.json");
    }

    private IReadOnlyList<ComputerProfile> LoadFromPath(string path)
    {
        try
        {
            return LoadFromJson(File.ReadAllText(path));
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Profile store contains invalid JSON and was not modified.", exception);
        }
    }

    private static IReadOnlyList<ComputerProfile> LoadFromJson(string json)
    {
        try
        {
            var document = JsonSerializer.Deserialize<ProfileStoreDocument>(json, SerializerOptions)
                ?? throw new InvalidDataException("Profile store is empty or invalid.");

            if (document.SchemaVersion is < OldestSupportedStoreSchemaVersion or > ProfileStoreDocument.CurrentSchemaVersion)
            {
                throw new InvalidDataException($"Unsupported profile store schema version: {document.SchemaVersion}.");
            }

            document.Computers ??= [];
            foreach (var profile in document.Computers)
            {
                if (profile.SchemaVersion is < 1 or > ComputerProfile.CurrentSchemaVersion)
                {
                    throw new InvalidDataException($"Unsupported profile schema version: {profile.SchemaVersion}.");
                }

                profile.MigrateToCurrentSchema();
            }

            document.SchemaVersion = ProfileStoreDocument.CurrentSchemaVersion;
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

    private string CreatePreservedOriginalPath(string directory)
    {
        var fileName = Path.GetFileNameWithoutExtension(FilePath);
        var extension = Path.GetExtension(FilePath);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'", System.Globalization.CultureInfo.InvariantCulture);
        return Path.Combine(directory, $"{fileName}.preserved-{timestamp}-{Guid.NewGuid():N}{extension}");
    }

    private static string CreateTemporaryPath(string directory, string fileName, string purpose) =>
        Path.Combine(directory, $".{fileName}.{purpose}.{Guid.NewGuid():N}.tmp");
}
