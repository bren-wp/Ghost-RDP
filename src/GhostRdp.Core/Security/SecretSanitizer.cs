using System.Text.RegularExpressions;

namespace GhostRdp.Core.Security;

public static partial class SecretSanitizer
{
    public static string Sanitize(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        return SecretAssignmentRegex().Replace(message, match => $"{match.Groups[1].Value}=[REDACTED]");
    }

    [GeneratedRegex(@"(?i)\b(password|passwd|token|secret|pairing[_ -]?code)\s*=\s*([^;\s]+)", RegexOptions.CultureInvariant)]
    private static partial Regex SecretAssignmentRegex();
}
