using System.Windows;
using GhostRdp.Core.Host;

namespace GhostRdp.Host;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var snapshot = HostEnvironmentSnapshot.Capture();
        ComputerNameText.Text = snapshot.ComputerName;
        OperatingSystemText.Text = snapshot.OperatingSystem;
        CurrentUserText.Text = snapshot.CurrentUser;
    }
}
