using System.Globalization;
using System.Net;

namespace GhostRdp.Core.Validation;

public static class ConnectionInputValidator
{
    public static ValidationResult ValidatePort(int port) =>
        port is >= 1 and <= 65535
            ? ValidationResult.Success()
            : ValidationResult.Failure("Port must be between 1 and 65535.");

    public static ValidationResult ValidateHost(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult.Failure("Host is required.");
        }

        var host = value.Trim();
        if (!string.Equals(host, value, StringComparison.Ordinal))
        {
            return ValidationResult.Failure("Host must not contain leading or trailing whitespace.");
        }

        var ipCandidate = host.Length >= 2 && host[0] == '[' && host[^1] == ']'
            ? host[1..^1]
            : host;

        if (IPAddress.TryParse(ipCandidate, out _))
        {
            return ValidationResult.Success();
        }

        if (host.Length > 253)
        {
            return ValidationResult.Failure("Hostname is too long.");
        }

        var normalized = host.EndsWith(".", StringComparison.Ordinal) ? host[..^1] : host;
        if (normalized.Length == 0)
        {
            return ValidationResult.Failure("Hostname is invalid.");
        }

        string ascii;
        try
        {
            ascii = new IdnMapping().GetAscii(normalized);
        }
        catch (ArgumentException)
        {
            return ValidationResult.Failure("Hostname contains invalid characters.");
        }

        foreach (var label in ascii.Split('.'))
        {
            if (label.Length is < 1 or > 63)
            {
                return ValidationResult.Failure("Hostname label length is invalid.");
            }

            if (!IsAlphaNumeric(label[0]) || !IsAlphaNumeric(label[^1]))
            {
                return ValidationResult.Failure("Hostname labels must start and end with a letter or digit.");
            }

            if (label.Any(character => !IsAlphaNumeric(character) && character != '-'))
            {
                return ValidationResult.Failure("Hostname contains invalid characters.");
            }
        }

        return ValidationResult.Success();
    }

    public static ValidationResult ValidateDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult.Failure("Display name is required.");
        }

        if (value.Length > 100)
        {
            return ValidationResult.Failure("Display name must be 100 characters or fewer.");
        }

        return ContainsControlCharacters(value)
            ? ValidationResult.Failure("Display name contains unsupported control characters.")
            : ValidationResult.Success();
    }

    public static ValidationResult ValidateUsername(string? value)
    {
        if (value is null)
        {
            return ValidationResult.Success();
        }

        if (value.Length > 256)
        {
            return ValidationResult.Failure("Username is too long.");
        }

        return ContainsControlCharacters(value)
            ? ValidationResult.Failure("Username contains unsupported control characters.")
            : ValidationResult.Success();
    }

    public static ValidationResult ValidateDomain(string? value)
    {
        if (value is null)
        {
            return ValidationResult.Success();
        }

        if (value.Length > 255)
        {
            return ValidationResult.Failure("Domain is too long.");
        }

        return ContainsControlCharacters(value)
            ? ValidationResult.Failure("Domain contains unsupported control characters.")
            : ValidationResult.Success();
    }

    private static bool IsAlphaNumeric(char value) =>
        value is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';

    private static bool ContainsControlCharacters(string value) => value.Any(char.IsControl);
}
