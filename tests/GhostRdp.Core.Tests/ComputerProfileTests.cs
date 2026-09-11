using GhostRdp.Core.Profiles;

namespace GhostRdp.Core.Tests;

[TestClass]
public sealed class ComputerProfileTests
{
    [TestMethod]
    public void CloneWithNewIdentity_PreservesSettingsButChangesId()
    {
        var source = CreateProfile();
        source.Favorite = true;
        source.Tags = ["office", "vpn"];

        var duplicate = source.CloneWithNewIdentity();

        Assert.AreNotEqual(source.Id, duplicate.Id);
        Assert.AreEqual("Office PC Copy", duplicate.DisplayName);
        Assert.AreEqual(source.Host, duplicate.Host);
        Assert.AreEqual(source.Favorite, duplicate.Favorite);
        CollectionAssert.AreEqual(source.Tags, duplicate.Tags);
    }

    [TestMethod]
    public void ValidateCollection_RejectsDuplicateIds()
    {
        var first = CreateProfile();
        var second = CreateProfile();
        second.Id = first.Id;

        var validation = ComputerProfileValidator.ValidateCollection([first, second]);

        Assert.IsFalse(validation.IsValid);
        StringAssert.Contains(validation.Error ?? string.Empty, "Duplicate profile ID");
    }

    [TestMethod]
    public void Validate_RejectsUnsupportedSchema()
    {
        var profile = CreateProfile();
        profile.SchemaVersion = 99;

        var validation = ComputerProfileValidator.Validate(profile);

        Assert.IsFalse(validation.IsValid);
        StringAssert.Contains(validation.Error ?? string.Empty, "schema version");
    }

    private static ComputerProfile CreateProfile() => new()
    {
        DisplayName = "Office PC",
        Host = "10.10.0.15",
        Port = 3389,
        Username = "marko"
    };
}
