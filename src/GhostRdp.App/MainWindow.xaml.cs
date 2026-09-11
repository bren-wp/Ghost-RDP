using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GhostRdp.App.Settings;
using GhostRdp.Core.Profiles;
using GhostRdp.Core.Runtime;

namespace GhostRdp.App;

public partial class MainWindow : Window
{
    private static readonly string[] PaletteKeys =
    [
        "WindowBrush", "PanelBrush", "PanelAltBrush", "BorderBrush", "TextBrush", "MutedBrush",
        "AccentBrush", "AccentStrongBrush", "SuccessBrush", "WarningBrush", "DangerBrush", "SelectionBrush"
    ];

    private readonly ComputerProfileStore _profileStore;
    private readonly AppSettingsStore _settingsStore;
    private readonly Dictionary<string, Color> _standardPalette = new(StringComparer.Ordinal);
    private List<ComputerProfile> _profiles = [];
    private AppSettings _settings = AppSettings.CreateDefault();
    private bool _profileStoreWritable = true;
    private bool _settingsAutoPersistEnabled = true;

    public MainWindow()
    {
        InitializeComponent();
        CaptureStandardPalette();
        SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;

        _settingsStore = new AppSettingsStore(AppSettingsStore.GetDefaultFilePath());
        _profileStore = new ComputerProfileStore(ComputerProfileStore.GetDefaultFilePath());

        LoadSettings();
        ApplySettingsToControls();
        ApplyAccessibilityPalette();

        MstscLauncher.CleanupStaleTemporaryFiles();
        LoadProfiles();
        RefreshRuntimeStatus();
        RefreshProfileViews();
        RefreshAboutView();
        ShowInitialView();
    }

    protected override void OnClosed(EventArgs e)
    {
        SystemParameters.StaticPropertyChanged -= SystemParameters_StaticPropertyChanged;
        base.OnClosed(e);
    }

    private void HomeNavButton_Click(object sender, RoutedEventArgs e) => ShowView(HomeView);

    private void ComputersNavButton_Click(object sender, RoutedEventArgs e) => ShowView(ComputersView);

    private void QuickConnectNavButton_Click(object sender, RoutedEventArgs e) => ShowView(QuickConnectView);

    private void SettingsNavButton_Click(object sender, RoutedEventArgs e) => ShowView(SettingsView);

    private void AboutNavButton_Click(object sender, RoutedEventArgs e) => ShowView(AboutView);

    private void OpenQuickConnectButton_Click(object sender, RoutedEventArgs e) => ShowView(QuickConnectView);

    private void RefreshStatusButton_Click(object sender, RoutedEventArgs e) => RefreshRuntimeStatus();

    private void ShowInitialView()
    {
        var initial = _settings.RememberLastView ? _settings.LastView : _settings.StartupView;
        ShowView(GetView(initial), persistLastView: false);
    }

    private FrameworkElement GetView(StartupView view) => view switch
    {
        StartupView.Computers => ComputersView,
        StartupView.QuickConnect => QuickConnectView,
        _ => HomeView
    };

    private void ShowView(FrameworkElement view, bool persistLastView = true)
    {
        HomeView.Visibility = Visibility.Collapsed;
        ComputersView.Visibility = Visibility.Collapsed;
        QuickConnectView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Collapsed;
        AboutView.Visibility = Visibility.Collapsed;
        view.Visibility = Visibility.Visible;

        if (persistLastView && IsLoaded && TryGetPersistableView(view, out var lastView))
        {
            PersistLastView(lastView);
        }
    }

    private bool TryGetPersistableView(FrameworkElement view, out StartupView lastView)
    {
        if (ReferenceEquals(view, ComputersView))
        {
            lastView = StartupView.Computers;
            return true;
        }

        if (ReferenceEquals(view, QuickConnectView))
        {
            lastView = StartupView.QuickConnect;
            return true;
        }

        if (ReferenceEquals(view, HomeView))
        {
            lastView = StartupView.Home;
            return true;
        }

        lastView = StartupView.Home;
        return false;
    }

    private void PersistLastView(StartupView lastView)
    {
        if (!_settings.RememberLastView || !_settingsAutoPersistEnabled || _settings.LastView == lastView)
        {
            return;
        }

        var candidate = _settings.Clone();
        candidate.LastView = lastView;
        try
        {
            _settingsStore.Save(candidate);
            _settings = candidate;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _settingsAutoPersistEnabled = false;
            SetSettingsStatus($"The last view could not be saved automatically: {exception.Message}", true);
        }
    }

    private void LoadSettings()
    {
        try
        {
            _settings = _settingsStore.Load();
            _settingsAutoPersistEnabled = true;
            SetSettingsStatus($"Settings are stored locally at {_settingsStore.FilePath}", false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _settings = AppSettings.CreateDefault();
            _settingsAutoPersistEnabled = false;
            SetSettingsStatus($"Settings could not be loaded: {exception.Message} The existing file was not modified. Save or reset settings explicitly to replace it.", true);
        }
    }

    private void ApplySettingsToControls()
    {
        StartupViewComboBox.SelectedIndex = _settings.StartupView switch
        {
            StartupView.Computers => 1,
            StartupView.QuickConnect => 2,
            _ => 0
        };
        DefaultSortComboBox.SelectedIndex = _settings.DefaultComputerSort switch
        {
            ComputerSortPreference.Host => 1,
            ComputerSortPreference.FavoritesFirst => 2,
            _ => 0
        };
        RememberLastViewCheckBox.IsChecked = _settings.RememberLastView;
        ComputerSortComboBox.SelectedIndex = DefaultSortComboBox.SelectedIndex;
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var candidate = new AppSettings
        {
            StartupView = StartupViewComboBox.SelectedIndex switch
            {
                1 => StartupView.Computers,
                2 => StartupView.QuickConnect,
                _ => StartupView.Home
            },
            DefaultComputerSort = DefaultSortComboBox.SelectedIndex switch
            {
                1 => ComputerSortPreference.Host,
                2 => ComputerSortPreference.FavoritesFirst,
                _ => ComputerSortPreference.Name
            },
            RememberLastView = RememberLastViewCheckBox.IsChecked == true,
            LastView = _settings.LastView
        };

        try
        {
            _settingsStore.Save(candidate);
            _settings = candidate;
            _settingsAutoPersistEnabled = true;
            ComputerSortComboBox.SelectedIndex = DefaultSortComboBox.SelectedIndex;
            RefreshProfileViews();
            SetSettingsStatus("Settings saved. Only local UI preferences were written.", false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            SetSettingsStatus($"Settings were not saved: {exception.Message}", true);
        }
    }

    private void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var defaults = AppSettings.CreateDefault();
        try
        {
            _settingsStore.Save(defaults);
            _settings = defaults;
            _settingsAutoPersistEnabled = true;
            ApplySettingsToControls();
            RefreshProfileViews();
            SetSettingsStatus("Settings reset to safe defaults.", false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            SetSettingsStatus($"Settings could not be reset: {exception.Message}", true);
        }
    }

    private void SetSettingsStatus(string message, bool isError)
    {
        SettingsStatusText.Text = message;
        SettingsStatusText.Foreground = GetBrush(isError ? "DangerBrush" : "MutedBrush");
    }

    private void LoadProfiles()
    {
        try
        {
            _profiles = _profileStore.Load().Select(profile => profile.Clone()).ToList();
            _profileStoreWritable = true;
            SetProfileStatus($"Profiles are stored locally at {_profileStore.FilePath}", false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _profiles = [];
            _profileStoreWritable = false;
            SetProfileStatus($"Saved computers could not be loaded: {exception.Message} Existing profile data will not be overwritten.", true);
        }
    }

    private void RefreshRuntimeStatus()
    {
        var status = RdpRuntimeDetector.Detect();
        RuntimeStatusText.Text = status.IsAvailable ? "Available" : "Unavailable";
        RuntimeStatusText.Foreground = GetBrush(status.IsAvailable ? "SuccessBrush" : "WarningBrush");
        RuntimeDetailText.Text = status.IsAvailable && status.ExecutablePath is not null
            ? $"{status.Message} Path: {status.ExecutablePath}"
            : status.Message;
        ConnectSelectedComputerButton.IsEnabled = status.IsAvailable;
        QuickConnectConnectButton.IsEnabled = status.IsAvailable;
        AboutRuntimeText.Text = status.IsAvailable && status.ExecutablePath is not null
            ? $"Microsoft RDP runtime available: {status.ExecutablePath}"
            : status.Message;
    }

    private void RefreshAboutView()
    {
        AboutText.Text = AppMetadata.BuildAboutText();
        AboutProfilePathText.Text = _profileStore.FilePath;
        AboutSettingsPathText.Text = _settingsStore.FilePath;
    }

    private void RefreshProfileViews()
    {
        var search = ComputerSearchTextBox.Text.Trim();
        IEnumerable<ComputerProfile> filtered = _profiles;
        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(profile =>
                profile.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || profile.Host.Contains(search, StringComparison.OrdinalIgnoreCase)
                || profile.GatewayHost.Contains(search, StringComparison.OrdinalIgnoreCase)
                || profile.RemoteAccessMode.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
                || profile.Username.Contains(search, StringComparison.OrdinalIgnoreCase)
                || profile.Domain.Contains(search, StringComparison.OrdinalIgnoreCase)
                || profile.Notes.Contains(search, StringComparison.OrdinalIgnoreCase)
                || profile.Tags.Any(tag => tag.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        if (FavoritesOnlyCheckBox.IsChecked == true)
        {
            filtered = filtered.Where(profile => profile.Favorite);
        }

        var sortTag = (ComputerSortComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        filtered = sortTag switch
        {
            "host" => filtered.OrderBy(profile => profile.Host, StringComparer.OrdinalIgnoreCase).ThenBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase),
            "favorite" => filtered.OrderByDescending(profile => profile.Favorite).ThenBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase),
            _ => filtered.OrderBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase)
        };

        ProfileListBox.ItemsSource = filtered.ToList();
        ComputerSummaryText.Text = $"{_profiles.Count} saved computer{(_profiles.Count == 1 ? string.Empty : "s")}";
        SavedComputerCountText.Text = _profiles.Count.ToString(CultureInfo.InvariantCulture);
        FavoriteCountText.Text = _profiles.Count(profile => profile.Favorite).ToString(CultureInfo.InvariantCulture);
    }

    private void ComputerSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded)
        {
            RefreshProfileViews();
        }
    }

    private void ComputerSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
        {
            RefreshProfileViews();
        }
    }

    private void FavoritesOnlyCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            RefreshProfileViews();
        }
    }

    private void AddComputerButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProfileStoreWritable())
        {
            return;
        }

        var editor = new ComputerProfileEditorWindow(new ComputerProfile(), true) { Owner = this };
        if (editor.ShowDialog() == true && editor.SavedProfile is not null)
        {
            var candidate = _profiles.Select(profile => profile.Clone()).ToList();
            candidate.Add(editor.SavedProfile);
            CommitProfiles(candidate, "Computer saved.");
        }
    }

    private void ConnectSelectedComputerButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedProfile(out var selected))
        {
            return;
        }

        var result = MstscLauncher.Launch(RdpConnectionRequest.FromProfile(selected));
        SetProfileStatus($"{DescribeRemoteAccessMode(selected.RemoteAccessMode)} {result.Message}", !result.Success);
        if (!result.Success)
        {
            RefreshRuntimeStatus();
        }
    }

    private void EditComputerButton_Click(object sender, RoutedEventArgs e) => EditSelectedComputer();

    private void ProfileListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) => EditSelectedComputer();

    private void EditSelectedComputer()
    {
        if (!EnsureProfileStoreWritable() || ProfileListBox.SelectedItem is not ComputerProfile selected)
        {
            if (ProfileListBox.SelectedItem is null)
            {
                SetProfileStatus("Select a computer to edit.", true);
            }

            return;
        }

        var editor = new ComputerProfileEditorWindow(selected, false) { Owner = this };
        if (editor.ShowDialog() != true || editor.SavedProfile is null)
        {
            return;
        }

        var candidate = _profiles.Select(profile => profile.Clone()).ToList();
        var index = candidate.FindIndex(profile => profile.Id == selected.Id);
        if (index < 0)
        {
            SetProfileStatus("The selected computer no longer exists.", true);
            return;
        }

        candidate[index] = editor.SavedProfile;
        CommitProfiles(candidate, "Computer updated.");
    }

    private void DuplicateComputerButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProfileStoreWritable() || !TryGetSelectedProfile(out var selected))
        {
            return;
        }

        var candidate = _profiles.Select(profile => profile.Clone()).ToList();
        candidate.Add(selected.CloneWithNewIdentity());
        CommitProfiles(candidate, "Computer duplicated with a new profile ID.");
    }

    private void ToggleFavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProfileStoreWritable() || !TryGetSelectedProfile(out var selected))
        {
            return;
        }

        var candidate = _profiles.Select(profile => profile.Clone()).ToList();
        var profile = candidate.Single(item => item.Id == selected.Id);
        profile.Favorite = !profile.Favorite;
        CommitProfiles(candidate, profile.Favorite ? "Computer added to favorites." : "Computer removed from favorites.");
    }

    private void DeleteComputerButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProfileStoreWritable() || !TryGetSelectedProfile(out var selected))
        {
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Delete '{selected.DisplayName}'? This removes only the local Ghost RDP profile.",
            "Delete computer",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var candidate = _profiles.Where(profile => profile.Id != selected.Id).Select(profile => profile.Clone()).ToList();
        CommitProfiles(candidate, "Computer deleted.");
    }

    private bool TryGetSelectedProfile(out ComputerProfile selected)
    {
        if (ProfileListBox.SelectedItem is ComputerProfile profile)
        {
            selected = profile;
            return true;
        }

        selected = null!;
        SetProfileStatus("Select a computer first.", true);
        return false;
    }

    private bool EnsureProfileStoreWritable()
    {
        if (_profileStoreWritable)
        {
            return true;
        }

        SetProfileStatus("Profile changes are disabled because the existing profile store could not be safely loaded.", true);
        return false;
    }

    private bool CommitProfiles(List<ComputerProfile> candidate, string successMessage)
    {
        try
        {
            _profileStore.Save(candidate);
            _profiles = candidate;
            SetProfileStatus(successMessage, false);
            RefreshProfileViews();
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            SetProfileStatus($"Profile change was not saved: {exception.Message}", true);
            return false;
        }
    }

    private void ValidateQuickConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildQuickConnectDraft(out var draft, out var error))
        {
            SetQuickConnectStatus(error, true);
            return;
        }

        var validation = draft.Validate();
        var message = validation.IsValid
            ? $"Connection details are valid. {DescribeRemoteAccessMode(draft.RemoteAccessMode)} No profile was created."
            : validation.Error ?? "Connection details are invalid.";
        SetQuickConnectStatus(message, !validation.IsValid);
    }

    private void QuickConnectConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildQuickConnectDraft(out var draft, out var error))
        {
            SetQuickConnectStatus(error, true);
            return;
        }

        var validation = draft.Validate();
        if (!validation.IsValid)
        {
            SetQuickConnectStatus(validation.Error ?? "Connection details are invalid.", true);
            return;
        }

        var result = MstscLauncher.Launch(RdpConnectionRequest.FromQuickConnect(draft));
        SetQuickConnectStatus($"{DescribeRemoteAccessMode(draft.RemoteAccessMode)} {result.Message}", !result.Success);
        if (!result.Success)
        {
            RefreshRuntimeStatus();
        }
    }

    private void SaveQuickConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureProfileStoreWritable())
        {
            SetQuickConnectStatus("Saved computers are currently read-only because the profile store could not be loaded safely.", true);
            return;
        }

        if (!TryBuildQuickConnectDraft(out var draft, out var error))
        {
            SetQuickConnectStatus(error, true);
            return;
        }

        ComputerProfile profile;
        try
        {
            profile = draft.CreateProfile(QuickSaveNameTextBox.Text);
        }
        catch (InvalidOperationException exception)
        {
            SetQuickConnectStatus(exception.Message, true);
            return;
        }

        var candidate = _profiles.Select(item => item.Clone()).ToList();
        candidate.Add(profile);
        if (CommitProfiles(candidate, "Computer saved from Quick Connect."))
        {
            QuickSaveNameTextBox.Clear();
            SetQuickConnectStatus("Computer saved explicitly. The Quick Connect draft itself was not persisted separately.", false);
        }
    }

    private bool TryBuildQuickConnectDraft(out QuickConnectDraft draft, out string error)
    {
        if (!int.TryParse(QuickPortTextBox.Text, out var port))
        {
            draft = new QuickConnectDraft();
            error = "Port must be a number between 1 and 65535.";
            return false;
        }

        var remoteAccessMode = GetRemoteAccessMode(QuickRemoteAccessModeComboBox.SelectedIndex);
        draft = new QuickConnectDraft
        {
            Host = QuickHostTextBox.Text.Trim(),
            Port = port,
            Username = QuickUsernameTextBox.Text.Trim(),
            Domain = QuickDomainTextBox.Text.Trim(),
            RemoteAccessMode = remoteAccessMode,
            GatewayHost = remoteAccessMode == RemoteAccessMode.RdGateway
                ? QuickGatewayHostTextBox.Text.Trim()
                : string.Empty
        };
        error = string.Empty;
        return true;
    }

    private static RemoteAccessMode GetRemoteAccessMode(int selectedIndex) => selectedIndex switch
    {
        1 => RemoteAccessMode.PrivateNetwork,
        2 => RemoteAccessMode.RdGateway,
        _ => RemoteAccessMode.Direct
    };

    private static string DescribeRemoteAccessMode(RemoteAccessMode mode) => mode switch
    {
        RemoteAccessMode.PrivateNetwork => "Private-network route selected; Ghost RDP assumes your VPN/overlay route is already active.",
        RemoteAccessMode.RdGateway => "RD Gateway route selected; Microsoft Remote Desktop owns gateway and target credential prompts.",
        _ => "Direct route selected."
    };

    private void SetProfileStatus(string message, bool isError)
    {
        ProfileStoreStatusText.Text = message;
        ProfileStoreStatusText.Foreground = GetBrush(isError ? "DangerBrush" : "MutedBrush");
        ProfileStoreHomeStatusText.Text = message;
        ProfileStoreHomeStatusText.Foreground = GetBrush(isError ? "DangerBrush" : "MutedBrush");
    }

    private void SetQuickConnectStatus(string message, bool isError)
    {
        QuickConnectStatusText.Text = message;
        QuickConnectStatusText.Foreground = GetBrush(isError ? "DangerBrush" : "SuccessBrush");
    }

    private void CaptureStandardPalette()
    {
        foreach (var key in PaletteKeys)
        {
            if (Application.Current.Resources[key] is SolidColorBrush brush)
            {
                _standardPalette[key] = brush.Color;
            }
        }
    }

    private void ApplyAccessibilityPalette()
    {
        if (SystemParameters.HighContrast)
        {
            SetBrushResource("WindowBrush", SystemColors.WindowColor);
            SetBrushResource("PanelBrush", SystemColors.WindowColor);
            SetBrushResource("PanelAltBrush", SystemColors.ControlColor);
            SetBrushResource("BorderBrush", SystemColors.WindowTextColor);
            SetBrushResource("TextBrush", SystemColors.WindowTextColor);
            SetBrushResource("MutedBrush", SystemColors.GrayTextColor);
            SetBrushResource("AccentBrush", SystemColors.HighlightColor);
            SetBrushResource("AccentStrongBrush", SystemColors.HighlightColor);
            SetBrushResource("SuccessBrush", SystemColors.WindowTextColor);
            SetBrushResource("WarningBrush", SystemColors.WindowTextColor);
            SetBrushResource("DangerBrush", SystemColors.WindowTextColor);
            SetBrushResource("SelectionBrush", SystemColors.ControlColor);
            AccessibilityStatusText.Text = "Windows High Contrast is active. Ghost RDP is using system colors while preserving text labels and visible keyboard focus.";
            return;
        }

        foreach (var pair in _standardPalette)
        {
            SetBrushResource(pair.Key, pair.Value);
        }

        AccessibilityStatusText.Text = "Standard Ghost RDP palette is active. Windows High Contrast is detected automatically when enabled.";
    }

    private static void SetBrushResource(string key, Color color) =>
        Application.Current.Resources[key] = new SolidColorBrush(color);

    private void SystemParameters_StaticPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.Equals(e.PropertyName, nameof(SystemParameters.HighContrast), StringComparison.Ordinal))
        {
            return;
        }

        if (Dispatcher.CheckAccess())
        {
            ApplyAccessibilityPalette();
        }
        else
        {
            Dispatcher.Invoke(ApplyAccessibilityPalette);
        }
    }

    private Brush GetBrush(string key) => (Brush)FindResource(key);
}
