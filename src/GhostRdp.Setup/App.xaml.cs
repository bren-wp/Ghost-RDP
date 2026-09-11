using System.Windows;

namespace GhostRdp.Setup;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var arguments = e.Args;
        var quiet = HasFlag(arguments, "--quiet");
        var requestedPath = GetOption(arguments, "--path");

        if (HasFlag(arguments, "--self-test"))
        {
            Shutdown(SetupEngine.ValidateEmbeddedPayload() ? 0 : 2);
            return;
        }

        if (HasFlag(arguments, "--uninstall-helper"))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var installDirectory = GetRequiredOption(arguments, "--install-dir");
            var parentPidText = GetRequiredOption(arguments, "--parent-pid");
            var removeData = HasFlag(arguments, "--remove-data");
            var exitCode = int.TryParse(parentPidText, out var parentPid)
                ? SetupEngine.RunUninstallHelper(installDirectory, parentPid, removeData)
                : 2;

            if (exitCode != 0 && !quiet)
            {
                MessageBox.Show(
                    "Ghost RDP could not be completely removed. Close any running Ghost RDP windows and try again.",
                    "Ghost RDP Setup",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            Shutdown(exitCode);
            return;
        }

        var uninstall = HasFlag(arguments, "--uninstall");
        var installPath = uninstall
            ? requestedPath ?? SetupEngine.ResolveInstalledDirectory()
            : requestedPath ?? SetupEngine.DefaultInstallDirectory;

        if (quiet)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try
            {
                if (uninstall)
                {
                    SetupEngine.BeginUninstall(installPath, HasFlag(arguments, "--remove-data"), true);
                }
                else
                {
                    SetupEngine.Install(installPath);
                }

                Shutdown(0);
            }
            catch
            {
                Shutdown(1);
            }

            return;
        }

        var window = new MainWindow(uninstall ? SetupMode.Uninstall : SetupMode.Install, installPath);
        MainWindow = window;
        window.Show();
    }

    private static bool HasFlag(IEnumerable<string> arguments, string flag) =>
        arguments.Any(argument => string.Equals(argument, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOption(IReadOnlyList<string> arguments, string name)
    {
        for (var index = 0; index < arguments.Count - 1; index++)
        {
            if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return arguments[index + 1];
            }
        }

        return null;
    }

    private static string GetRequiredOption(IReadOnlyList<string> arguments, string name) =>
        GetOption(arguments, name) ?? string.Empty;
}
