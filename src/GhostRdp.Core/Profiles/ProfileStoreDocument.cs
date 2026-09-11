namespace GhostRdp.Core.Profiles;

public sealed class ProfileStoreDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public List<ComputerProfile> Computers { get; set; } = [];
}
