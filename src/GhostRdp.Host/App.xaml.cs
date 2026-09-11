using System.Windows;
using System.Windows.Threading;
using GhostRdp.Core.Host;
using GhostRdp.Host.Diagnostics;

namespace GhostRdp.Host;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        if (e.Args.Any(argument => string.Equals(argument, "--self-test", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var snapshot = WindowsHostReadinessProbe.Capture();
                _ = HostReadinessEvaluator.Evaluate(snapshot);
                Shutdown(0);
            }
            catch
            {
                Shutdown(1);
            }

            return;
        }

        base.OnStartup(e);
        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        MessageBox.Show(
            "Ghost RDP Host encountered an unexpected error and will close safely. Reopen the application and refresh diagnostics.",
            "Ghost RDP Host",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        Shutdown(1);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e) =>
        e.SetObserved();
}
