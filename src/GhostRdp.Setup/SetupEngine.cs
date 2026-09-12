using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;

namespace GhostRdp.Setup;

public static class SetupEngine
{
    private const string PayloadResourceName = "GhostRdp.Setup.Payload.zip";
    private const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\GhostRDP";
    private const int MoveFileDelayUntilReboot = 0x4;

    private static readonly string[] RequiredPayloadFiles =
    [
        "GhostRDP.exe",
        "GhostRDP-Host.exe",
        "LICENSE.txt"
    ];

    public static string DefaultInstallDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Ghost RDP");

    public static string LocalDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ghost RDP");

    public static bool ValidateEmbeddedPayload()
    {
        try
        {
            using var stream = OpenPayloadStream();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            var entries = archive.Entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
                .Select(entry => entry.FullName.Replace('/', '\\'))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return RequiredPayloadFiles.All(entries.Contains);
        }
        catch
        {
            return false;
        }
    }

    public static void Install(string installDirectory)
    {
        var targetDirectory = NormalizeInstallDirectory(installDirectory);
        EnsureInstallTargetCanBeReplaced(targetDirectory);
        EnsureProductProcessesAreClosed();

        var parentDirectory = Directory.GetParent(targetDirectory)?.FullName
            ?? throw new InvalidOperationException("The installation folder is not valid.");
        Directory.CreateDirectory(parentDirectory);

        var stagingDirectory = Path.Combine(parentDirectory, $".ghost-rdp-install-{Guid.NewGuid():N}");
        var backupDirectory = Path.Combine(parentDirectory, $".ghost-rdp-backup-{Guid.NewGuid():N}");
        var previousInstallationMoved = false;

        try
        {
            Directory.CreateDirectory(stagingDirectory);
            ExtractPayload(stagingDirectory);
            CopySetupExecutable(stagingDirectory);
            ValidateInstalledPayload(stagingDirectory);

            if (Directory.Exists(targetDirectory))
            {
                Directory.Move(targetDirectory, backupDirectory);
                previousInstallationMoved = true;
            }

            Directory.Move(stagingDirectory, targetDirectory);

            try
            {
                CreateStartMenuShortcuts(targetDirectory);
                RegisterUninstall(targetDirectory);
            }
            catch
            {
                TryDeleteDirectory(targetDirectory);
                if (previousInstallationMoved && Directory.Exists(backupDirectory))
                {
                    Directory.Move(backupDirectory, targetDirectory);
                }

                throw;
            }

            TryDeleteDirectory(backupDirectory);
        }
        catch
        {
            TryDeleteDirectory(stagingDirectory);
            if (!Directory.Exists(targetDirectory) && previousInstallationMoved && Directory.Exists(backupDirectory))
            {
                try
                {
                    Directory.Move(backupDirectory, targetDirectory);
                }
                catch (IOException)
                {
                    // Preserve the backup if Windows cannot restore it immediately.
                }
                catch (UnauthorizedAccessException)
                {
                    // Preserve the backup if Windows cannot restore it immediately.
                }
            }

            throw;
        }
    }

    public static void BeginUninstall(string installDirectory, bool removeLocalData, bool quiet)
    {
        var targetDirectory = NormalizeInstallDirectory(installDirectory);
        EnsureRegisteredUninstallTarget(targetDirectory);
        EnsureProductProcessesAreClosed();

        var currentExecutable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExecutable) || !File.Exists(currentExecutable))
        {
            throw new InvalidOperationException("Ghost RDP Setup could not locate its executable.");
        }

        var helperDirectory = Path.Combine(Path.GetTempPath(), "GhostRDP", "Uninstall", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(helperDirectory);
        var helperPath = Path.Combine(helperDirectory, "GhostRDP-Setup.exe");
        File.Copy(currentExecutable, helperPath, overwrite: false);

        var startInfo = new ProcessStartInfo
        {
            FileName = helperPath,
            UseShellExecute = false,
            WorkingDirectory = helperDirectory
        };
        startInfo.ArgumentList.Add("--uninstall-helper");
        startInfo.ArgumentList.Add("--install-dir");
        startInfo.ArgumentList.Add(targetDirectory);
        startInfo.ArgumentList.Add("--parent-pid");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (removeLocalData)
        {
            startInfo.ArgumentList.Add("--remove-data");
        }

        if (quiet)
        {
            startInfo.ArgumentList.Add("--quiet");
        }

        var process = Process.Start(startInfo);
        if (process is null)
        {
            TryDeleteDirectory(helperDirectory);
            throw new InvalidOperationException("Ghost RDP uninstall could not be started.");
        }

        process.Dispose();
    }

    public static int RunUninstallHelper(string installDirectory, int parentProcessId, bool removeLocalData)
    {
        try
        {
            WaitForParentProcess(parentProcessId);
            EnsureProductProcessesAreClosed();

            var targetDirectory = NormalizeInstallDirectory(installDirectory);
            EnsureRegisteredUninstallTarget(targetDirectory);
            RemoveStartMenuShortcuts();
            DeleteDirectoryWithRetry(targetDirectory, ignoreMissing: true);
            RemoveUninstallRegistration();

            if (removeLocalData)
            {
                DeleteDirectoryWithRetry(LocalDataDirectory, ignoreMissing: true);
            }

            ScheduleHelperCleanup();
            return 0;
        }
        catch
        {
            ScheduleHelperCleanup();
            return 1;
        }
    }

    public static string ResolveInstalledDirectory() =>
        TryGetRegisteredInstallDirectory(out var registeredDirectory)
            ? registeredDirectory
            : DefaultInstallDirectory;

    public static void LaunchInstalledApplication(string installDirectory)
    {
        var targetDirectory = NormalizeInstallDirectory(installDirectory);
        var executable = Path.Combine(targetDirectory, "GhostRDP.exe");
        if (!File.Exists(executable))
        {
            throw new FileNotFoundException("Ghost RDP is not installed in the expected folder.", executable);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = targetDirectory,
            UseShellExecute = false
        };
        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Ghost RDP could not be started.");
        process.Dispose();
    }

    private static void EnsureInstallTargetCanBeReplaced(string targetDirectory)
    {
        var hasRegisteredInstallation = TryGetRegisteredInstallDirectory(out var registeredDirectory);
        if (hasRegisteredInstallation
            && !string.Equals(targetDirectory, registeredDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Ghost RDP is already registered in another installation folder. Uninstall it before choosing a different folder.");
        }

        if (File.Exists(targetDirectory))
        {
            throw new InvalidOperationException("The selected installation path points to an existing file.");
        }

        if (Directory.Exists(targetDirectory) && !hasRegisteredInstallation)
        {
            throw new InvalidOperationException(
                "Ghost RDP will not replace an existing folder unless Windows Installed Apps identifies it as the registered Ghost RDP installation.");
        }
    }

    private static void EnsureRegisteredUninstallTarget(string targetDirectory)
    {
        if (!TryGetRegisteredInstallDirectory(out var registeredDirectory)
            || !string.Equals(targetDirectory, registeredDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Ghost RDP will only uninstall the installation folder registered in Windows Installed Apps.");
        }

        if (File.Exists(targetDirectory))
        {
            throw new InvalidOperationException("The registered Ghost RDP installation path points to a file instead of a folder.");
        }
    }

    private static bool TryGetRegisteredInstallDirectory(out string installDirectory)
    {
        installDirectory = string.Empty;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(UninstallRegistryPath, writable: false);
            var registeredPath = key?.GetValue("InstallLocation") as string;
            if (string.IsNullOrWhiteSpace(registeredPath))
            {
                return false;
            }

            installDirectory = NormalizeInstallDirectory(registeredPath);
            return true;
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException
                                          or IOException
                                          or SecurityException
                                          or ArgumentException
                                          or InvalidOperationException
                                          or NotSupportedException)
        {
            installDirectory = string.Empty;
            return false;
        }
    }

    private static void ExtractPayload(string destinationDirectory)
    {
        using var stream = OpenPayloadStream();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var destinationRoot = Path.GetFullPath(destinationDirectory) + Path.DirectorySeparatorChar;

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, entry.FullName));
            if (!destinationPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The installer payload contains an invalid path.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            entry.ExtractToFile(destinationPath, overwrite: false);
        }
    }

    private static Stream OpenPayloadStream() =>
        Assembly.GetExecutingAssembly().GetManifestResourceStream(PayloadResourceName)
        ?? throw new InvalidDataException("The Ghost RDP installer payload is missing.");

    private static void CopySetupExecutable(string stagingDirectory)
    {
        var currentExecutable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExecutable) || !File.Exists(currentExecutable))
        {
            throw new InvalidOperationException("Ghost RDP Setup could not locate its executable.");
        }

        File.Copy(currentExecutable, Path.Combine(stagingDirectory, "GhostRDP-Setup.exe"), overwrite: false);
    }

    private static void ValidateInstalledPayload(string directory)
    {
        foreach (var name in RequiredPayloadFiles)
        {
            var path = Path.Combine(directory, name);
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                throw new InvalidDataException($"The installer payload is incomplete: {name}");
            }
        }

        if (!File.Exists(Path.Combine(directory, "GhostRDP-Setup.exe")))
        {
            throw new InvalidDataException("The installed Setup executable is missing.");
        }
    }

    private static void RegisterUninstall(string installDirectory)
    {
        using var key = Registry.CurrentUser.CreateSubKey(UninstallRegistryPath, writable: true)
            ?? throw new UnauthorizedAccessException("Windows could not register Ghost RDP for uninstall.");

        var setupPath = Path.Combine(installDirectory, "GhostRDP-Setup.exe");
        var appPath = Path.Combine(installDirectory, "GhostRDP.exe");
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.9.0";
        var estimatedSizeKb = Directory.EnumerateFiles(installDirectory, "*", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path).Length)
            .Sum() / 1024;

        key.SetValue("DisplayName", "Ghost RDP", RegistryValueKind.String);
        key.SetValue("DisplayVersion", version, RegistryValueKind.String);
        key.SetValue("Publisher", "Brendigo", RegistryValueKind.String);
        key.SetValue("InstallLocation", installDirectory, RegistryValueKind.String);
        key.SetValue("DisplayIcon", appPath, RegistryValueKind.String);
        key.SetValue("UninstallString", $"\"{setupPath}\" --uninstall", RegistryValueKind.String);
        key.SetValue("QuietUninstallString", $"\"{setupPath}\" --uninstall --quiet", RegistryValueKind.String);
        key.SetValue("URLInfoAbout", "https://github.com/bren-wp/Ghost-RDP", RegistryValueKind.String);
        key.SetValue("InstallDate", DateTime.UtcNow.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture), RegistryValueKind.String);
        key.SetValue("EstimatedSize", checked((int)Math.Min(estimatedSizeKb, int.MaxValue)), RegistryValueKind.DWord);
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    private static void RemoveUninstallRegistration()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(UninstallRegistryPath, throwOnMissingSubKey: false);
        }
        catch (UnauthorizedAccessException)
        {
            // Program files are already removed; Windows may clear the stale entry later.
        }
        catch (SecurityException)
        {
            // Program files are already removed; Windows may clear the stale entry later.
        }
    }

    private static void CreateStartMenuShortcuts(string installDirectory)
    {
        var programsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        if (string.IsNullOrWhiteSpace(programsDirectory))
        {
            return;
        }

        var shortcutDirectory = Path.Combine(programsDirectory, "Ghost RDP");
        Directory.CreateDirectory(shortcutDirectory);
        CreateShortcut(
            Path.Combine(shortcutDirectory, "Ghost RDP.lnk"),
            Path.Combine(installDirectory, "GhostRDP.exe"),
            installDirectory,
            "Ghost RDP");
        CreateShortcut(
            Path.Combine(shortcutDirectory, "Ghost RDP Host.lnk"),
            Path.Combine(installDirectory, "GhostRDP-Host.exe"),
            installDirectory,
            "Ghost RDP Host readiness diagnostics");
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string description)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell", throwOnError: false)
            ?? throw new PlatformNotSupportedException("Windows shortcut support is unavailable.");
        object? shellObject = null;
        object? shortcutObject = null;
        try
        {
            shellObject = Activator.CreateInstance(shellType)
                ?? throw new InvalidOperationException("Windows shortcut support could not be initialized.");
            dynamic shell = shellObject;
            shortcutObject = shell.CreateShortcut(shortcutPath);
            dynamic shortcut = shortcutObject;
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = workingDirectory;
            shortcut.Description = description;
            shortcut.IconLocation = targetPath;
            shortcut.Save();
        }
        finally
        {
            ReleaseComObject(shortcutObject);
            ReleaseComObject(shellObject);
        }
    }

    private static void RemoveStartMenuShortcuts()
    {
        try
        {
            var programsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            if (!string.IsNullOrWhiteSpace(programsDirectory))
            {
                TryDeleteDirectory(Path.Combine(programsDirectory, "Ghost RDP"));
            }
        }
        catch (IOException)
        {
            // Shortcut cleanup is best effort and must not block program removal.
        }
        catch (UnauthorizedAccessException)
        {
            // Shortcut cleanup is best effort and must not block program removal.
        }
    }

    private static void EnsureProductProcessesAreClosed()
    {
        foreach (var processName in new[] { "GhostRDP", "GhostRDP-Host", "GhostRdp.App", "GhostRdp.Host" })
        {
            var processes = Process.GetProcessesByName(processName);
            try
            {
                if (processes.Any(process => process.Id != Environment.ProcessId))
                {
                    throw new InvalidOperationException("Close Ghost RDP and Ghost RDP Host before continuing.");
                }
            }
            finally
            {
                foreach (var process in processes)
                {
                    process.Dispose();
                }
            }
        }
    }

    private static void WaitForParentProcess(int processId)
    {
        if (processId <= 0 || processId == Environment.ProcessId)
        {
            return;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            process.WaitForExit(15000);
        }
        catch (ArgumentException)
        {
            // The parent has already exited.
        }
        catch (InvalidOperationException)
        {
            // The parent has already exited.
        }
    }

    private static string NormalizeInstallDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("The installation folder is required.", nameof(path));
        }

        var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var root = Path.GetPathRoot(fullPath)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(root) || string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Ghost RDP cannot be installed into a drive root.");
        }

        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!string.IsNullOrWhiteSpace(windowsDirectory)
            && (string.Equals(fullPath, windowsDirectory, StringComparison.OrdinalIgnoreCase)
                || fullPath.StartsWith(windowsDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Ghost RDP cannot be installed into the Windows system directory.");
        }

        return fullPath;
    }

    private static void DeleteDirectoryWithRetry(string path, bool ignoreMissing = false)
    {
        if (!Directory.Exists(path))
        {
            if (ignoreMissing)
            {
                return;
            }

            throw new DirectoryNotFoundException(path);
        }

        Exception? lastException = null;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                lastException = exception;
                Thread.Sleep(250);
            }
        }

        throw new IOException("Windows could not remove the Ghost RDP installation folder.", lastException);
    }

    private static void TryDeleteDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup; a later install can use a new unique staging directory.
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort cleanup; a later install can use a new unique staging directory.
        }
    }

    private static void ScheduleHelperCleanup()
    {
        var helperPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(helperPath))
        {
            return;
        }

        var helperDirectory = Path.GetDirectoryName(helperPath);
        MoveFileEx(helperPath, null, MoveFileDelayUntilReboot);
        if (!string.IsNullOrWhiteSpace(helperDirectory))
        {
            MoveFileEx(helperDirectory, null, MoveFileDelayUntilReboot);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MoveFileEx(string existingFileName, string? newFileName, int flags);
}
