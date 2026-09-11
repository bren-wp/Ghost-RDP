using System.Net;
using System.Net.Sockets;
using GhostRdp.Core.Profiles;
using GhostRdp.Core.Validation;

namespace GhostRdp.Core.Runtime;

public sealed record RdpConnectionRequest(string Host, int Port, string Username, string Domain)
{
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

    public string GetFullAddress()
    {
        var normalizedHost = Host.Length >= 2 && Host[0] == '[' && Host[^1] == ']'
            ? Host[1..^1]
            : Host;

        if (IPAddress.TryParse(normalizedHost, out var address)
            && address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return $"[{normalizedHost}]:{Port}";
        }

        return $"{Host}:{Port}";
    }

    public string? GetQualifiedUsername()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(Domain)
            ? Username
            : $"{Domain}\\{Username}";
    }

    public static RdpConnectionRequest FromProfile(ComputerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new RdpConnectionRequest(profile.Host, profile.Port, profile.Username, profile.Domain);
    }

    public static RdpConnectionRequest FromQuickConnect(QuickConnectDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        return new RdpConnectionRequest(draft.Host, draft.Port, draft.Username, draft.Domain);
    }
}
