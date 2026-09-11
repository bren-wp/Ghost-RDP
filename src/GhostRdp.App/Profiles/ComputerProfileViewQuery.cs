using GhostRdp.App.Settings;
using GhostRdp.Core.Profiles;

namespace GhostRdp.App.Profiles;

public static class ComputerProfileViewQuery
{
    public static IReadOnlyList<ComputerProfile> Apply(
        IEnumerable<ComputerProfile> profiles,
        string? search,
        bool favoritesOnly,
        ComputerSortPreference sortPreference)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        var normalizedSearch = search?.Trim() ?? string.Empty;
        IEnumerable<ComputerProfile> query = profiles;

        if (normalizedSearch.Length > 0)
        {
            query = query.Where(profile => Matches(profile, normalizedSearch));
        }

        if (favoritesOnly)
        {
            query = query.Where(profile => profile.Favorite);
        }

        query = sortPreference switch
        {
            ComputerSortPreference.Host => query
                .OrderBy(profile => profile.Host, StringComparer.OrdinalIgnoreCase)
                .ThenBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase),
            ComputerSortPreference.FavoritesFirst => query
                .OrderByDescending(profile => profile.Favorite)
                .ThenBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase),
            _ => query.OrderBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase)
        };

        return query.ToList();
    }

    private static bool Matches(ComputerProfile profile, string search) =>
        profile.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
        || profile.Host.Contains(search, StringComparison.OrdinalIgnoreCase)
        || profile.GatewayHost.Contains(search, StringComparison.OrdinalIgnoreCase)
        || profile.RemoteAccessMode.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
        || profile.Username.Contains(search, StringComparison.OrdinalIgnoreCase)
        || profile.Domain.Contains(search, StringComparison.OrdinalIgnoreCase)
        || profile.Notes.Contains(search, StringComparison.OrdinalIgnoreCase)
        || profile.Tags.Any(tag => tag.Contains(search, StringComparison.OrdinalIgnoreCase));
}
