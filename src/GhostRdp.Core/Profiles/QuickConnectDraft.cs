using GhostRdp.Core.Validation;

namespace GhostRdp.Core.Profiles;

public sealed class QuickConnectDraft
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 3389;

    public string Username { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public RemoteAccessMode RemoteAccessMode { get; set; } = RemoteAccessMode.Direct;

    public string GatewayHost { get; set; } = string.Empty;

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

        var domain = ConnectionInputValidator.ValidateDomain(Domain);
        if (!domain.IsValid)
        {
            return domain;
        }

        if (!Enum.IsDefined(RemoteAccessMode))
        {
            return ValidationResult.Failure("Remote access mode is invalid.");
        }

        if (RemoteAccessMode == RemoteAccessMode.RdGateway)
        {
            var gateway = ConnectionInputValidator.ValidateHost(GatewayHost);
            if (!gateway.IsValid)
            {
                return ValidationResult.Failure($"RD Gateway: {gateway.Error}");
            }
        }
        else if (!string.IsNullOrWhiteSpace(GatewayHost))
        {
            return ValidationResult.Failure("RD Gateway host must be empty unless RD Gateway mode is selected.");
        }

        return ValidationResult.Success();
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
            Domain = Domain.Trim(),
            RemoteAccessMode = RemoteAccessMode,
            GatewayHost = RemoteAccessMode == RemoteAccessMode.RdGateway ? GatewayHost.Trim() : string.Empty
        };
    }
}
