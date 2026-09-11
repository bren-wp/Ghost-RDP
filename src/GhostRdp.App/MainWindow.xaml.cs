using System.Windows;
using System.Windows.Media;
using GhostRdp.Core.Runtime;

namespace GhostRdp.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AboutText.Text = AppMetadata.BuildAboutText();
        RefreshRuntimeStatus();
    }

    private void HomeNavButton_Click(object sender, RoutedEventArgs e) => ShowHome();

    private void AboutNavButton_Click(object sender, RoutedEventArgs e) => ShowAbout();

    private void RefreshStatusButton_Click(object sender, RoutedEventArgs e) => RefreshRuntimeStatus();

    private void ShowHome()
    {
        HomeView.Visibility = Visibility.Visible;
        AboutView.Visibility = Visibility.Collapsed;
    }

    private void ShowAbout()
    {
        HomeView.Visibility = Visibility.Collapsed;
        AboutView.Visibility = Visibility.Visible;
    }

    private void RefreshRuntimeStatus()
    {
        var status = RdpRuntimeDetector.Detect();
        RuntimeStatusText.Text = status.IsAvailable ? "Available" : "Unavailable";
        RuntimeStatusText.Foreground = (Brush)FindResource(status.IsAvailable ? "SuccessBrush" : "WarningBrush");
        RuntimeDetailText.Text = status.IsAvailable && status.ExecutablePath is not null
            ? $"{status.Message} Path: {status.ExecutablePath}"
            : status.Message;
    }
}
