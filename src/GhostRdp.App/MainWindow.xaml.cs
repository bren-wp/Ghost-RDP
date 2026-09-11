using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GhostRdp.Core.Profiles;
using GhostRdp.Core.Runtime;

namespace GhostRdp.App;

public partial class MainWindow : Window
{
    private readonly ComputerProfileStore _profileStore;
    private List<ComputerProfile> _profiles = [];
    private bool _profileStoreWritable = true;

    public MainWindow()
    {
        InitializeComponent();
        _profileStore = new ComputerProfileStore(ComputerProfileStore.GetDefaultFilePath());
        MstscLauncher.CleanupStaleTemporaryFiles();
        AboutText.Text = AppMetadata.BuildAboutText();
        LoadProfiles();
        RefreshRuntimeStatus();
        RefreshProfileViews();
    }

    private void HomeNavButton_Click(object sender, RoutedEventArgs e) => ShowView(HomeView);

    private void ComputersNavButton_Click(object sender, RoutedEventArgs e) => ShowView(ComputersView);

    private void QuickConnectNavButton_Click(object sender, RoutedEventArgs e) => ShowView(QuickConnectView);

    private void AboutNavButton_Click(object sender, RoutedEventArgs e) => ShowView(AboutView);

    private void OpenQuickConnectButton_Click(object sender, RoutedEventArgs e) => ShowView(QuickConnectView);

    private void RefreshStatusButton_Click(object sender, RoutedEventArgs e) => RefreshRuntimeStatus();

    private void ShowView(FrameworkElement view)
    {
        HomeView.Visibility = Visibility.Collapsed;
        ComputersView.Visibility = Visibility.Collapsed;
        QuickConnectView.Visibility = Visibility.Collapsed;
        AboutView.Visibility = Visibility.Collapsed;
        view.Visibility = Visibility.Visible;
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
        SavedComputerCountText.Text = _profiles.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        FavoriteCountText.Text = _profiles.Count(profile => profile.Favorite).ToString(System.Globalization.CultureInfo.InvariantCulture);
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
        SetProfileStatus(result.Message, !result.Success);
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
        SetQuickConnectStatus(validation.IsValid ? "Connection details are valid. No profile was created." : validation.Error ?? "Connection details are invalid.", !validation.IsValid);
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
        SetQuickConnectStatus(result.Message, !result.Success);
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

        draft = new QuickConnectDraft
        {
            Host = QuickHostTextBox.Text.Trim(),
            Port = port,
            Username = QuickUsernameTextBox.Text.Trim(),
            Domain = QuickDomainTextBox.Text.Trim()
        };
        error = string.Empty;
        return true;
    }

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

    private Brush GetBrush(string key) => (Brush)FindResource(key);
}
