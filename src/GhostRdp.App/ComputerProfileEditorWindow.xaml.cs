using System.Windows;
using GhostRdp.Core.Profiles;

namespace GhostRdp.App;

public partial class ComputerProfileEditorWindow : Window
{
    private readonly ComputerProfile _workingProfile;

    public ComputerProfileEditorWindow(ComputerProfile profile, bool isNew)
    {
        ArgumentNullException.ThrowIfNull(profile);
        InitializeComponent();
        _workingProfile = profile.Clone();
        HeadingText.Text = isNew ? "Add computer" : "Edit computer";
        PopulateFields();
    }

    public ComputerProfile? SavedProfile { get; private set; }

    private void PopulateFields()
    {
        DisplayNameTextBox.Text = _workingProfile.DisplayName;
        HostTextBox.Text = _workingProfile.Host;
        PortTextBox.Text = _workingProfile.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        UsernameTextBox.Text = _workingProfile.Username;
        DomainTextBox.Text = _workingProfile.Domain;
        TagsTextBox.Text = string.Join(", ", _workingProfile.Tags);
        NotesTextBox.Text = _workingProfile.Notes;
        FavoriteCheckBox.IsChecked = _workingProfile.Favorite;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(PortTextBox.Text, out var port))
        {
            ValidationText.Text = "Port must be a number between 1 and 65535.";
            return;
        }

        _workingProfile.DisplayName = DisplayNameTextBox.Text.Trim();
        _workingProfile.Host = HostTextBox.Text.Trim();
        _workingProfile.Port = port;
        _workingProfile.Username = UsernameTextBox.Text.Trim();
        _workingProfile.Domain = DomainTextBox.Text.Trim();
        _workingProfile.Tags = TagsTextBox.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        _workingProfile.Notes = NotesTextBox.Text;
        _workingProfile.Favorite = FavoriteCheckBox.IsChecked == true;

        var validation = ComputerProfileValidator.Validate(_workingProfile);
        if (!validation.IsValid)
        {
            ValidationText.Text = validation.Error;
            return;
        }

        SavedProfile = _workingProfile.Clone();
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
