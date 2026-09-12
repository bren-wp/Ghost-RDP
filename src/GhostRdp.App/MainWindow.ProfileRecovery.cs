using System.Windows;

namespace GhostRdp.App;

public partial class MainWindow
{
    private bool _profileRecoveryPromptShown;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        OfferProfileStoreRecoveryIfAvailable();
    }

    private void OfferProfileStoreRecoveryIfAvailable()
    {
        if (_profileRecoveryPromptShown || _profileStoreWritable || !_profileStore.HasRecoverableBackup())
        {
            return;
        }

        _profileRecoveryPromptShown = true;
        var choice = MessageBox.Show(
            this,
            $"Saved computers could not be loaded safely, but Ghost RDP found a validated previous backup at:\n\n{_profileStore.BackupFilePath}\n\nRestore that backup now? The current unreadable file will be preserved separately before recovery.",
            "Recover saved computers",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (choice != MessageBoxResult.Yes)
        {
            SetProfileStatus(
                $"Saved computers remain read-only. A validated backup is available at {_profileStore.BackupFilePath}; restart Ghost RDP to be offered recovery again.",
                true);
            return;
        }

        try
        {
            var recovery = _profileStore.RestoreBackup();
            _profiles = recovery.Profiles.Select(profile => profile.Clone()).ToList();
            _favoriteProfileCount = _profiles.Count(profile => profile.Favorite);
            _profileStoreWritable = true;
            RefreshProfileViews();

            var preservationMessage = recovery.PreservedOriginalFilePath is null
                ? "No prior primary file required preservation."
                : $"The unreadable original was preserved at {recovery.PreservedOriginalFilePath}.";
            SetProfileStatus(
                $"Saved computers were restored from the validated backup. {preservationMessage}",
                false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            _profileStoreWritable = false;
            SetProfileStatus(
                $"Saved-computer recovery failed: {exception.Message} The existing files were not intentionally overwritten.",
                true);
        }
    }
}
