using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace GhostRdp.Setup;

public enum SetupMode
{
    Install,
    Uninstall
}

public partial class MainWindow : Window
{
    private readonly SetupMode _mode;
    private readonly string _installDirectory;

    public MainWindow(SetupMode mode, string installDirectory)
    {
        InitializeComponent();
        _mode = mode;
        _installDirectory = installDirectory;

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.9.0";
        VersionText.Text = $"Version {version}";
        ArchitectureText.Text = RuntimeInformation.ProcessArchitecture.ToString();
        InstallPathText.Text = installDirectory;

        if (mode == SetupMode.Uninstall)
        {
            HeadingText.Text = "Uninstall Ghost RDP";
            DescriptionText.Text = "Remove the installed application and shortcuts from this Windows account.";
            DataNoticeText.Text = "Saved computers and local settings are kept by default. Select the option below only if you also want to remove that local data.";
            RemoveDataCheckBox.Visibility = Visibility.Visible;
            PrimaryButton.Content = "Uninstall Ghost RDP";
        }
        else
        {
            HeadingText.Text = "Install Ghost RDP";
            DescriptionText.Text = "Install the native Windows build for this package into your local application folder.";
            DataNoticeText.Text = "Setup does not enable Remote Desktop, change firewall rules, create a service, or store credentials.";
            PrimaryButton.Content = "Install Ghost RDP";
        }
    }

    private async void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        try
        {
            if (_mode == SetupMode.Uninstall)
            {
                SetupEngine.BeginUninstall(_installDirectory, RemoveDataCheckBox.IsChecked == true, false);
                StatusText.Text = "Windows is removing Ghost RDP.";
                Close();
                return;
            }

            await Task.Run(() => SetupEngine.Install(_installDirectory));
            StatusText.Foreground = (Brush)FindResource("SuccessBrush");
            StatusText.Text = "Ghost RDP was installed successfully.";
            PrimaryButton.Visibility = Visibility.Collapsed;
            LaunchButton.Visibility = Visibility.Visible;
        }
        catch
        {
            StatusText.Foreground = (Brush)FindResource("DangerBrush");
            StatusText.Text = _mode == SetupMode.Uninstall
                ? "Ghost RDP could not be removed. Close any running Ghost RDP windows and try again."
                : "Ghost RDP could not be installed. Close any running Ghost RDP windows and verify access to the installation folder.";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetupEngine.LaunchInstalledApplication(_installDirectory);
            Close();
        }
        catch
        {
            StatusText.Foreground = (Brush)FindResource("DangerBrush");
            StatusText.Text = "Ghost RDP is installed, but it could not be started from Setup.";
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void SetBusy(bool busy)
    {
        ProgressBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        PrimaryButton.IsEnabled = !busy;
        LaunchButton.IsEnabled = !busy;
    }
}
