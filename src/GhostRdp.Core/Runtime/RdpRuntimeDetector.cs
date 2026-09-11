namespace GhostRdp.Core.Runtime;

public static class RdpRuntimeDetector
{
    public static RdpRuntimeStatus Detect()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new RdpRuntimeStatus(false, null, "Microsoft Remote Desktop runtime detection is supported on Windows only.");
        }

        foreach (var candidate in EnumerateCandidates())
        {
            try
            {
                if (File.Exists(candidate))
                {
                    return new RdpRuntimeStatus(true, candidate, "Microsoft Remote Desktop runtime is available.");
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Continue to the next candidate. Detection must never elevate privileges.
            }
            catch (IOException)
            {
                // Continue to the next candidate if the filesystem is temporarily unavailable.
            }
        }

        return new RdpRuntimeStatus(false, null, "Microsoft Remote Desktop runtime (mstsc.exe) was not found. Connect actions must remain unavailable.");
    }

    private static IEnumerable<string> EnumerateCandidates()
    {
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (!string.IsNullOrWhiteSpace(windowsDirectory))
        {
            yield return Path.Combine(windowsDirectory, "System32", "mstsc.exe");
        }

        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            yield break;
        }

        foreach (var entry in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return Path.Combine(entry, "mstsc.exe");
        }
    }
}
