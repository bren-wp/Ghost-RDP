using System.ComponentModel;
using System.Diagnostics;

namespace GhostRdp.Core.Runtime;

public static class MstscLauncher
{
    private static readonly TimeSpan StaleTemporaryFileAge = TimeSpan.FromHours(24);

    public static MstscLaunchResult Launch(RdpConnectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = request.Validate();
        if (!validation.IsValid)
        {
            return MstscLaunchResult.Failure(validation.Error ?? "Connection details are invalid.");
        }

        var runtime = RdpRuntimeDetector.Detect();
        if (!runtime.IsAvailable || string.IsNullOrWhiteSpace(runtime.ExecutablePath))
        {
            return MstscLaunchResult.Failure(runtime.Message);
        }

        CleanupStaleTemporaryFiles();

        TemporaryRdpFile? temporaryFile = null;
        Process? process = null;
        try
        {
            temporaryFile = TemporaryRdpFile.Create(request);
            var startInfo = MstscProcessStartInfoFactory.Create(runtime.ExecutablePath, temporaryFile.FilePath);
            process = Process.Start(startInfo);
            if (process is null)
            {
                temporaryFile.Dispose();
                return MstscLaunchResult.Failure("Microsoft Remote Desktop could not be started.");
            }

            var processId = process.Id;
            _ = CleanupAfterExitAsync(process, temporaryFile);
            return MstscLaunchResult.Started(processId);
        }
        catch (Exception exception) when (exception is Win32Exception or IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            process?.Dispose();
            temporaryFile?.Dispose();
            return MstscLaunchResult.Failure($"Microsoft Remote Desktop could not be started: {exception.Message}");
        }
    }

    public static int CleanupStaleTemporaryFiles() =>
        TemporaryRdpFile.CleanupStaleSessions(StaleTemporaryFileAge);

    private static async Task CleanupAfterExitAsync(Process process, TemporaryRdpFile temporaryFile)
    {
        try
        {
            await process.WaitForExitAsync().ConfigureAwait(false);
        }
        catch (SystemException)
        {
            // Stale-session cleanup will remove the file on a later application run if needed.
        }
        finally
        {
            temporaryFile.Dispose();
            process.Dispose();
        }
    }
}
