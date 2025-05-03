using System.Diagnostics;
using Wbskt.Client.Contracts;

namespace Wbskt.Client.Windows.Service;

internal sealed class PayloadHandler(ILogger<PayloadHandler> logger) : IPayloadHandler
{
    public void ProcessPayload(ClientPayload payload)
    {
        ExecCmdCommand(payload.Data);
    }

    public void ExecCmdCommand(string rawCommand)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c execute.bat \"{rawCommand}\"",
            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
            // CreateNoWindow = true,
            // UseShellExecute = true
        });

        process?.WaitForExit();
        logger.LogInformation("execute.bat completed with an exit code: {code}", process?.ExitCode);
    }
}
