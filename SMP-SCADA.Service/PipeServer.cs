using SMP_SCADA.Core;
using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;
using SMP_SCADA.Core.Service.Pipe;
using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace SMP_SCADA.Service;

[SupportedOSPlatform("windows")]
public sealed class PipeServer(ShaftMonitor monitor, CommandQueue commands, IDriveLink drive,
                               TimeProvider time, ILogger<PipeServer> log) : BackgroundService
{
    public const int MaxApps = 4;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            NamedPipeServerStream pipe;
            try
            {
                pipe = CreatePipe();
            }
            catch (IOException)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), time, stoppingToken);
                continue;
            }

            try
            {
                await pipe.WaitForConnectionAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                await pipe.DisposeAsync();
                break;
            }
            _ = ServeAsync(pipe, stoppingToken);
        }
    }

    async Task ServeAsync(NamedPipeServerStream pipe, CancellationToken ct)
    {
        await using (pipe)
        {
            try
            {
                log.LogInformation("App connected");
                await PipeSession.RunAsync(pipe, "HMI", Role.Engineer, () => monitor.Latest, commands, time, ct);
                log.LogInformation("App disconnected");
            }
            catch (Exception e) when (e is IOException or OperationCanceledException)
            {
                // the app went away, or the service is stopping
            }
            catch (Exception e)
            {
                log.LogError(e, "App connection failed");
            }
        }
    }
    // Signed-in users on this PC may connect
    static NamedPipeServerStream CreatePipe()
    {
        var security = new PipeSecurity();
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                                                  PipeAccessRights.ReadWrite, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User!,
                                                  PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.NetworkSid, null),
                                                  PipeAccessRights.FullControl, AccessControlType.Deny));

        return NamedPipeServerStreamAcl.Create(Protocol.PipeName, PipeDirection.InOut, MaxApps,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0, security);
    }
}