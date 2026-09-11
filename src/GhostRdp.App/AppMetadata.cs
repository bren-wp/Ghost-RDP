using System.Reflection;
using GhostRdp.Core;

namespace GhostRdp.App;

public static class AppMetadata
{
    public static string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";

    public static string BuildAboutText() =>
        $"{ProductInfo.ProductName} {Version}\n" +
        $"{ProductInfo.Tagline}\n\n" +
        $"Platform: {Environment.OSVersion.VersionString}\n" +
        "Runtime: Microsoft Remote Desktop (mstsc.exe) is launched only after an explicit user action.\n" +
        $"Privacy: {ProductInfo.PrivacySummary}";
}
