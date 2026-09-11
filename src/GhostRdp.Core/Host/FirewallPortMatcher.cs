namespace GhostRdp.Core.Host;

public static class FirewallPortMatcher
{
    public static bool CoversPort(string? localPorts, int port)
    {
        if (port is < 1 or > 65535 || string.IsNullOrWhiteSpace(localPorts))
        {
            return false;
        }

        foreach (var rawEntry in localPorts.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (rawEntry is "*" || rawEntry.Equals("any", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (int.TryParse(rawEntry, out var singlePort) && singlePort == port)
            {
                return true;
            }

            var rangeParts = rawEntry.Split('-', 2, StringSplitOptions.TrimEntries);
            if (rangeParts.Length == 2
                && int.TryParse(rangeParts[0], out var first)
                && int.TryParse(rangeParts[1], out var last)
                && first <= port
                && port <= last)
            {
                return true;
            }
        }

        return false;
    }
}
