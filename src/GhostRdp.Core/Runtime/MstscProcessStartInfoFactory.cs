using System.Diagnostics;

namespace GhostRdp.Core.Runtime;

public static class MstscProcessStartInfoFactory
{
    public static ProcessStartInfo Create(string executablePath, string rdpFilePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ArgumentException("Microsoft Remote Desktop executable path is required.", nameof(executablePath));
        }

        if (string.IsNullOrWhiteSpace(rdpFilePath))
        {
            throw new ArgumentException("Temporary RDP file path is required.", nameof(rdpFilePath));
        }

        if (!Path.IsPathFullyQualified(executablePath) || !Path.IsPathFullyQualified(rdpFilePath))
        {
            throw new ArgumentException("Process and RDP file paths must be fully qualified.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            CreateNoWindow = false
        };
        startInfo.ArgumentList.Add(rdpFilePath);
        return startInfo;
    }
}
