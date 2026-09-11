namespace GhostRdp.Core.Profiles;

public sealed class ComputerProfile
{
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string DisplayName { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 3389;

    public string Username { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public RemoteAccessMode RemoteAccessMode { get; set; } = RemoteAccessMode.Direct;

    public string GatewayHost { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public bool Favorite { get; set; }

    public List<string> Tags { get; set; } = [];

    public ComputerProfile Clone()
    {
        return Copy(Id, DisplayName);
    }

    public ComputerProfile CloneWithNewIdentity()
    {
        return Copy(Guid.NewGuid(), $"{DisplayName} Copy");
    }

    internal void MigrateToCurrentSchema()
    {
        if (SchemaVersion == 1)
        {
            RemoteAccessMode = RemoteAccessMode.Direct;
            GatewayHost = string.Empty;
            SchemaVersion = CurrentSchemaVersion;
        }
    }

    private ComputerProfile Copy(Guid id, string displayName)
    {
        return new ComputerProfile
        {
            SchemaVersion = SchemaVersion,
            Id = id,
            DisplayName = displayName,
            Host = Host,
            Port = Port,
            Username = Username,
            Domain = Domain,
            RemoteAccessMode = RemoteAccessMode,
            GatewayHost = GatewayHost,
            Notes = Notes,
            Favorite = Favorite,
            Tags = [.. Tags]
        };
    }
}
