using GhostRdp.Core.Validation;

namespace GhostRdp.Core.Profiles;

public static class ComputerProfileValidator
{
    public static ValidationResult Validate(ComputerProfile? profile)
    {
        if (profile is null)
        {
            return ValidationResult.Failure("Profile is required.");
        }

        if (profile.SchemaVersion != ComputerProfile.CurrentSchemaVersion)
        {
            return ValidationResult.Failure($"Unsupported profile schema version: {profile.SchemaVersion}.");
        }

        if (profile.Id == Guid.Empty)
        {
            return ValidationResult.Failure("Profile ID is invalid.");
        }

        var displayName = ConnectionInputValidator.ValidateDisplayName(profile.DisplayName);
        if (!displayName.IsValid)
        {
            return displayName;
        }

        var host = ConnectionInputValidator.ValidateHost(profile.Host);
        if (!host.IsValid)
        {
            return host;
        }

        var port = ConnectionInputValidator.ValidatePort(profile.Port);
        if (!port.IsValid)
        {
            return port;
        }

        var username = ConnectionInputValidator.ValidateUsername(profile.Username);
        if (!username.IsValid)
        {
            return username;
        }

        var domain = ConnectionInputValidator.ValidateDomain(profile.Domain);
        if (!domain.IsValid)
        {
            return domain;
        }

        if (!Enum.IsDefined(profile.RemoteAccessMode))
        {
            return ValidationResult.Failure("Remote access mode is invalid.");
        }

        if (profile.RemoteAccessMode == RemoteAccessMode.RdGateway)
        {
            var gateway = ConnectionInputValidator.ValidateHost(profile.GatewayHost);
            if (!gateway.IsValid)
            {
                return ValidationResult.Failure($"RD Gateway: {gateway.Error}");
            }
        }
        else if (!string.IsNullOrWhiteSpace(profile.GatewayHost))
        {
            return ValidationResult.Failure("RD Gateway host must be empty unless RD Gateway mode is selected.");
        }

        if (profile.Notes is null || profile.Notes.Length > 4000)
        {
            return ValidationResult.Failure("Notes must be 4000 characters or fewer.");
        }

        if (profile.Tags is null || profile.Tags.Count > 20 || profile.Tags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.Length > 40 || tag.Any(char.IsControl)))
        {
            return ValidationResult.Failure("Tags are invalid. Use up to 20 non-empty tags of 40 characters or fewer.");
        }

        return ValidationResult.Success();
    }

    public static ValidationResult ValidateCollection(IEnumerable<ComputerProfile> profiles)
    {
        var seen = new HashSet<Guid>();
        foreach (var profile in profiles)
        {
            var validation = Validate(profile);
            if (!validation.IsValid)
            {
                return validation;
            }

            if (!seen.Add(profile.Id))
            {
                return ValidationResult.Failure($"Duplicate profile ID detected: {profile.Id}.");
            }
        }

        return ValidationResult.Success();
    }
}
