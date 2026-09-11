using System.Text;

namespace GhostRdp.Core.Runtime;

public static class RdpFileSerializer
{
    public static string Serialize(RdpConnectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = request.Validate();
        if (!validation.IsValid)
        {
            throw new ArgumentException(validation.Error ?? "RDP connection request is invalid.", nameof(request));
        }

        var builder = new StringBuilder();
        AppendLine(builder, $"full address:s:{request.GetFullAddress()}");

        var username = request.GetQualifiedUsername();
        if (!string.IsNullOrWhiteSpace(username))
        {
            AppendLine(builder, $"username:s:{username}");
        }

        AppendLine(builder, "prompt for credentials:i:1");
        AppendLine(builder, "authentication level:i:1");
        AppendLine(builder, "enablecredsspsupport:i:1");
        return builder.ToString();
    }

    private static void AppendLine(StringBuilder builder, string value) => builder.Append(value).Append("\r\n");
}
