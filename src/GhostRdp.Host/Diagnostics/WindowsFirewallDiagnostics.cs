using System.Collections;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder;
using GhostRdp.Core.Host;

namespace GhostRdp.Host.Diagnostics;

internal sealed record FirewallDiagnosticsSnapshot(
    bool? FirewallEnabled,
    bool? BlocksAllInbound,
    bool? RdpInboundRuleAvailable,
    string ActiveProfiles);

internal static class WindowsFirewallDiagnostics
{
    private const int DomainProfile = 1;
    private const int PrivateProfile = 2;
    private const int PublicProfile = 4;
    private const int KnownProfiles = DomainProfile | PrivateProfile | PublicProfile;
    private const int InboundDirection = 1;
    private const int AllowAction = 1;
    private const int TcpProtocol = 6;

    public static FirewallDiagnosticsSnapshot Capture(int? rdpPort, ICollection<string> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        if (!OperatingSystem.IsWindows())
        {
            return UnknownSnapshot();
        }

        object? policyObject = null;
        object? rulesObject = null;
        try
        {
            var policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2", throwOnError: false);
            if (policyType is null)
            {
                diagnostics.Add("Windows Firewall policy COM API is unavailable.");
                return UnknownSnapshot();
            }

            policyObject = Activator.CreateInstance(policyType);
            if (policyObject is null)
            {
                diagnostics.Add("Windows Firewall policy object could not be created.");
                return UnknownSnapshot();
            }

            dynamic policy = policyObject;
            var currentProfiles = (int)policy.CurrentProfileTypes & KnownProfiles;
            var activeProfiles = GetProfileBits(currentProfiles);
            if (activeProfiles.Count == 0)
            {
                diagnostics.Add("Windows Firewall did not report an active profile.");
                return UnknownSnapshot();
            }

            var firewallEnabled = activeProfiles.All(profile => (bool)policy.FirewallEnabled[profile]);
            var blockAllInbound = activeProfiles.Any(profile => (bool)policy.BlockAllInboundTraffic[profile]);
            bool? rdpRuleAvailable = null;

            if (rdpPort is not null)
            {
                rulesObject = policy.Rules;
                if (rulesObject is IEnumerable rules)
                {
                    var coveredProfiles = 0;
                    var unreadableRules = 0;
                    foreach (var ruleObject in rules)
                    {
                        if (ruleObject is null)
                        {
                            continue;
                        }

                        try
                        {
                            dynamic rule = ruleObject;
                            if (!(bool)rule.Enabled
                                || (int)rule.Direction != InboundDirection
                                || (int)rule.Action != AllowAction
                                || (int)rule.Protocol != TcpProtocol)
                            {
                                continue;
                            }

                            var localPorts = (string?)rule.LocalPorts;
                            if (!FirewallPortMatcher.CoversPort(localPorts, rdpPort.Value))
                            {
                                continue;
                            }

                            var ruleProfiles = (int)rule.Profiles;
                            coveredProfiles |= ruleProfiles == int.MaxValue
                                ? currentProfiles
                                : ruleProfiles & currentProfiles;
                        }
                        catch (Exception exception) when (exception is COMException or RuntimeBinderException or InvalidCastException or MissingMemberException)
                        {
                            unreadableRules++;
                        }
                        finally
                        {
                            ReleaseComObject(ruleObject);
                        }
                    }

                    if (unreadableRules > 0)
                    {
                        diagnostics.Add($"{unreadableRules} Windows Firewall rule(s) could not be inspected and were skipped.");
                    }

                    rdpRuleAvailable = (coveredProfiles & currentProfiles) == currentProfiles;
                }
            }

            return new FirewallDiagnosticsSnapshot(
                firewallEnabled,
                blockAllInbound,
                rdpRuleAvailable,
                FormatProfiles(currentProfiles));
        }
        catch (Exception exception) when (exception is COMException or RuntimeBinderException or InvalidCastException or MissingMemberException)
        {
            diagnostics.Add($"Windows Firewall diagnostics are unavailable: {exception.Message}");
            return UnknownSnapshot();
        }
        finally
        {
            ReleaseComObject(rulesObject);
            ReleaseComObject(policyObject);
        }
    }

    private static List<int> GetProfileBits(int profiles)
    {
        var result = new List<int>(3);
        if ((profiles & DomainProfile) != 0)
        {
            result.Add(DomainProfile);
        }

        if ((profiles & PrivateProfile) != 0)
        {
            result.Add(PrivateProfile);
        }

        if ((profiles & PublicProfile) != 0)
        {
            result.Add(PublicProfile);
        }

        return result;
    }

    private static string FormatProfiles(int profiles)
    {
        var names = new List<string>(3);
        if ((profiles & DomainProfile) != 0)
        {
            names.Add("Domain");
        }

        if ((profiles & PrivateProfile) != 0)
        {
            names.Add("Private");
        }

        if ((profiles & PublicProfile) != 0)
        {
            names.Add("Public");
        }

        return names.Count == 0 ? "Unknown" : string.Join(", ", names);
    }

    private static FirewallDiagnosticsSnapshot UnknownSnapshot() => new(null, null, null, "Unknown");

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }
}
