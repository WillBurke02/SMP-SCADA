using SMP_SCADA.Core;
using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;
using static SMP_SCADA.Core.Service.Contract.Command;

namespace SMP_SCADA.Service;

// Monitor worker at 100ms
public sealed class MonitorWorker(ShaftMonitor monitor, CommandQueue commands, IDriveLink drive,
                                  TimeProvider time, ILogger<MonitorWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool simulated = drive is SimulatedDrive;
        if (simulated)
            log.LogInformation("Simulated connect: {Result}", monitor.Handle(new Connect(0, true), "simulator", Role.Engineer));

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100), time);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    if (drive is SimulatedDrive sim) sim.CountsPerSecond = SimulatedSpeed();
                    commands.HandleWaiting(monitor);
                    monitor.Tick();
                }
                catch (Exception e)
                {
                    log.LogError(e, "Monitor tick failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // service stopping
        }
        monitor.Stop();
    }
    double SimulatedSpeed()
    {
        double seconds = time.GetUtcNow().ToUnixTimeMilliseconds() / 1000.0;
        double mmPerSecond = 50 * Math.Sin(2 * Math.PI * seconds / 60);
        return mmPerSecond * Calibration.Default.CountsPerMm;
    }
}