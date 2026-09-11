using System.Text;

namespace GhostRdp.Core.Runtime;

public sealed class TemporaryRdpFile : IDisposable
{
    private int _disposed;

    private TemporaryRdpFile(string sessionDirectory, string filePath)
    {
        SessionDirectory = sessionDirectory;
        FilePath = filePath;
    }

    public string SessionDirectory { get; }

    public string FilePath { get; }

    public static string DefaultRootDirectory => Path.Combine(Path.GetTempPath(), "GhostRdp");

    public static TemporaryRdpFile Create(RdpConnectionRequest request, string? rootDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var content = RdpFileSerializer.Serialize(request);
        var root = ResolveRootDirectory(rootDirectory);
        Directory.CreateDirectory(root);

        var sessionDirectory = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sessionDirectory);
        var filePath = Path.Combine(sessionDirectory, $"{Guid.NewGuid():N}.rdp");

        try
        {
            using var stream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read,
                4096,
                FileOptions.WriteThrough);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.Write(content);
            writer.Flush();
            stream.Flush(true);
            return new TemporaryRdpFile(sessionDirectory, filePath);
        }
        catch
        {
            TryDeleteDirectory(sessionDirectory);
            throw;
        }
    }

    public static int CleanupStaleSessions(TimeSpan minimumAge, string? rootDirectory = null)
    {
        if (minimumAge < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumAge));
        }

        var root = ResolveRootDirectory(rootDirectory);
        if (!Directory.Exists(root))
        {
            return 0;
        }

        var deleted = 0;
        var now = DateTime.UtcNow;
        IEnumerable<string> directories;
        try
        {
            directories = Directory.EnumerateDirectories(root).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return 0;
        }

        foreach (var directory in directories)
        {
            try
            {
                var lastWrite = Directory.GetLastWriteTimeUtc(directory);
                if (now - lastWrite < minimumAge)
                {
                    continue;
                }

                Directory.Delete(directory, true);
                deleted++;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Best-effort cleanup must not block application startup or connection attempts.
            }
        }

        return deleted;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        TryDeleteDirectory(SessionDirectory);
    }

    private static string ResolveRootDirectory(string? rootDirectory)
    {
        var root = string.IsNullOrWhiteSpace(rootDirectory) ? DefaultRootDirectory : rootDirectory;
        return Path.GetFullPath(root);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Cleanup is retried by stale-session cleanup on a later application run.
        }
    }
}
