using GhostRdp.Core.Validation;

namespace GhostRdp.Core.Profiles;

public sealed class QuickConnectDraft
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 3389;

    public string Username { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public ValidationResult Validate()
    {
        var host = ConnectionInputValidator.ValidateHost(Host);
        if (!host.IsValid)
        {
            return host;
        }

        var port = ConnectionInputValidator.ValidatePort(Port);
        if (!port.IsValid)
        {
            return port;
        }

        var username = ConnectionInputValidator.ValidateUsername(Username);
        if (!username.IsValid)
        {
            return username;
        }

        return ConnectionInputValidator.ValidateDomain(Domain);
    }

    public ComputerProfile CreateProfile(string displayName)
    {
        var draftValidation = Validate();
        if (!draftValidation.IsValid)
        {
            throw new InvalidOperationException(draftValidation.Error);
        }

        var displayNameValidation = ConnectionInputValidator.ValidateDisplayName(displayName);
        if (!displayNameValidation.IsValid)
        {
            throw new InvalidOperationException(displayNameValidation.Error);
        }

        return new ComputerProfile
        {
            DisplayName = displayName.Trim(),
            Host = Host.Trim(),
            Port = Port,
            Username = Username.Trim(),
            Domain = Domain.Trim()
        };
    }
}
