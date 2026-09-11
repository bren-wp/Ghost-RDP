using GhostRdp.App.Profiles;
using GhostRdp.App.Settings;
using GhostRdp.Core.Profiles;

namespace GhostRdp.App.Tests;

[TestClass]
public sealed class ComputerProfileViewQueryTests
{
    [TestMethod]
    public void Apply_SearchesAcrossProfileFieldsAndTags()
    {
        var profiles = new[]
        {
            CreateProfile("Alpha", "10.0.0.10", username: "alice", tags: ["office"]),
            CreateProfile("Beta", "server.example.test", gatewayHost: "gateway.example.test", notes: "Finance workstation"),
            CreateProfile("Gamma", "10.0.0.30", domain: "LAB", tags: ["maintenance"])
        };

        var gateway = ComputerProfileViewQuery.Apply(profiles, "gateway.example", false, ComputerSortPreference.Name);
        var notes = ComputerProfileViewQuery.Apply(profiles, "finance", false, ComputerSortPreference.Name);
        var tags = ComputerProfileViewQuery.Apply(profiles, "maintenance", false, ComputerSortPreference.Name);
        var domain = ComputerProfileViewQuery.Apply(profiles, "lab", false, ComputerSortPreference.Name);

        Assert.AreEqual("Beta", Assert.Single(gateway).DisplayName);
        Assert.AreEqual("Beta", Assert.Single(notes).DisplayName);
        Assert.AreEqual("Gamma", Assert.Single(tags).DisplayName);
        Assert.AreEqual("Gamma", Assert.Single(domain).DisplayName);
    }

    [TestMethod]
    public void Apply_FavoritesOnlyFiltersBeforeSorting()
    {
        var profiles = new[]
        {
            CreateProfile("Zulu", "z.example.test", favorite: true),
            CreateProfile("Alpha", "a.example.test"),
            CreateProfile("Bravo", "b.example.test", favorite: true)
        };

        var result = ComputerProfileViewQuery.Apply(profiles, null, true, ComputerSortPreference.Name);

        CollectionAssert.AreEqual(new[] { "Bravo", "Zulu" }, result.Select(profile => profile.DisplayName).ToArray());
    }

    [TestMethod]
    public void Apply_SortsByHostThenDisplayNameCaseInsensitively()
    {
        var profiles = new[]
        {
            CreateProfile("Zulu", "same.example.test"),
            CreateProfile("alpha", "SAME.example.test"),
            CreateProfile("Bravo", "a.example.test")
        };

        var result = ComputerProfileViewQuery.Apply(profiles, string.Empty, false, ComputerSortPreference.Host);

        CollectionAssert.AreEqual(new[] { "Bravo", "alpha", "Zulu" }, result.Select(profile => profile.DisplayName).ToArray());
    }

    [TestMethod]
    public void Apply_FavoritesFirstUsesNameAsStableSecondarySort()
    {
        var profiles = new[]
        {
            CreateProfile("Zulu", "z.example.test"),
            CreateProfile("Charlie", "c.example.test", favorite: true),
            CreateProfile("Alpha", "a.example.test", favorite: true),
            CreateProfile("Bravo", "b.example.test")
        };

        var result = ComputerProfileViewQuery.Apply(profiles, null, false, ComputerSortPreference.FavoritesFirst);

        CollectionAssert.AreEqual(new[] { "Alpha", "Charlie", "Bravo", "Zulu" }, result.Select(profile => profile.DisplayName).ToArray());
    }

    private static ComputerProfile CreateProfile(
        string displayName,
        string host,
        string username = "",
        string domain = "",
        string gatewayHost = "",
        string notes = "",
        bool favorite = false,
        List<string>? tags = null) => new()
        {
            DisplayName = displayName,
            Host = host,
            Username = username,
            Domain = domain,
            GatewayHost = gatewayHost,
            Notes = notes,
            Favorite = favorite,
            Tags = tags ?? []
        };
}
