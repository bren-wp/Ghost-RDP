using System.ComponentModel;
using System.Runtime.InteropServices;
using GhostRdp.Core.Host;

namespace GhostRdp.Host.Diagnostics;

internal static partial class WindowsServiceDiagnostics
{
    private const uint ScManagerConnect = 0x0001;
    private const uint ServiceQueryStatus = 0x0004;
    private const int ScStatusProcessInfo = 0;

    public static HostServiceState ReadRemoteDesktopServiceState(ICollection<string> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        if (!OperatingSystem.IsWindows())
        {
            return HostServiceState.Unknown;
        }

        nint serviceControlManager = 0;
        nint service = 0;
        nint buffer = 0;

        try
        {
            serviceControlManager = OpenSCManager(null, null, ScManagerConnect);
            if (serviceControlManager == 0)
            {
                AddWin32Diagnostic(diagnostics, "Service Control Manager could not be opened");
                return HostServiceState.Unknown;
            }

            service = OpenService(serviceControlManager, "TermService", ServiceQueryStatus);
            if (service == 0)
            {
                AddWin32Diagnostic(diagnostics, "Remote Desktop Services status could not be opened");
                return HostServiceState.Unknown;
            }

            var size = Marshal.SizeOf<ServiceStatusProcess>();
            buffer = Marshal.AllocHGlobal(size);
            if (!QueryServiceStatusEx(service, ScStatusProcessInfo, buffer, (uint)size, out _))
            {
                AddWin32Diagnostic(diagnostics, "Remote Desktop Services status could not be queried");
                return HostServiceState.Unknown;
            }

            var status = Marshal.PtrToStructure<ServiceStatusProcess>(buffer);
            return status.CurrentState switch
            {
                1 => HostServiceState.Stopped,
                2 => HostServiceState.StartPending,
                3 => HostServiceState.StopPending,
                4 => HostServiceState.Running,
                5 => HostServiceState.ContinuePending,
                6 => HostServiceState.PausePending,
                7 => HostServiceState.Paused,
                _ => HostServiceState.Unknown
            };
        }
        finally
        {
            if (buffer != 0)
            {
                Marshal.FreeHGlobal(buffer);
            }

            if (service != 0)
            {
                CloseServiceHandle(service);
            }

            if (serviceControlManager != 0)
            {
                CloseServiceHandle(serviceControlManager);
            }
        }
    }

    private static void AddWin32Diagnostic(ICollection<string> diagnostics, string prefix)
    {
        var error = Marshal.GetLastWin32Error();
        diagnostics.Add($"{prefix}: {new Win32Exception(error).Message}");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ServiceStatusProcess
    {
        public uint ServiceType;
        public uint CurrentState;
        public uint ControlsAccepted;
        public uint Win32ExitCode;
        public uint ServiceSpecificExitCode;
        public uint CheckPoint;
        public uint WaitHint;
        public uint ProcessId;
        public uint ServiceFlags;
    }

    [LibraryImport("advapi32.dll", EntryPoint = "OpenSCManagerW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint OpenSCManager(string? machineName, string? databaseName, uint desiredAccess);

    [LibraryImport("advapi32.dll", EntryPoint = "OpenServiceW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint OpenService(nint serviceControlManager, string serviceName, uint desiredAccess);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool QueryServiceStatusEx(
        nint service,
        int infoLevel,
        nint buffer,
        uint bufferSize,
        out uint bytesNeeded);

    [LibraryImport("advapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseServiceHandle(nint handle);
}
