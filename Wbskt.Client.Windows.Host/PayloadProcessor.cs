using System.Diagnostics;
using Wbskt.Common.Contracts;

namespace Wbskt.Client.Windows.Host;

internal static class PayloadProcessor
{
    public static void ProcessPayload(this ClientPayload payload)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"msedge https://jira.soti.net/browse/{payload.Data}",
            CreateNoWindow = false,
            UseShellExecute = false
        });
        process?.WaitForExit();
    }
}
