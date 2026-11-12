using SMP_SCADA.Core.Service.Contract;
using System.Text.Json;

namespace SMP_SCADA.Core.Service.Pipe;

public static class PipeSession
{
    public static readonly TimeSpan SnapshotInterval = TimeSpan.FromMilliseconds(100);
    public static async Task RunAsync(Stream pipe, string user, Role role, Func<Snapshot> latest,
                                      CommandQueue commands, TimeProvider time, CancellationToken ct)
    {
        using var reader = new StreamReader(pipe, leaveOpen: true);
        await using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true, };
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var writeLock = new SemaphoreSlim(1);

        async Task Send(ServiceMessage message)
        {
            await writeLock.WaitAsync(stop.Token);
            try { await writer.WriteLineAsync(Protocol.Write(message).AsMemory(), stop.Token); }
            finally { writeLock.Release(); }
        }

        async Task SendSnapshots()
        {
            using var timer = new PeriodicTimer(SnapshotInterval, time);
            Snapshot? last = null;
            while (await timer.WaitForNextTickAsync(stop.Token))
            {
                var snapshot = latest();
                if (ReferenceEquals(snapshot, last)) continue;
                await Send(snapshot);
                last = snapshot;
            }
        }
        await Send(new Hello(Protocol.Version, user, role));
        var snapshots = SendSnapshots();
        try
        {
            while (await reader.ReadLineAsync(stop.Token) is string line)
            {
                Command command;
                try
                {
                    command = Protocol.ReadCommand(line);
                }
                catch (JsonException e)
                {
                    await Send(new Result(0, false, $"Unreadable message: {e.Message}"));
                    continue;
                }
                ServiceMessage reply;
                try
                {
                    reply = await commands.Send(command, user, role).WaitAsync(stop.Token);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    reply = new Result(command.Id, false, "Service fault");
                }
                await Send(reply);
            }
        }
        finally
        {
            stop.Cancel();
            try { await snapshots; }
            catch (Exception e) when (e is not OperationCanceledException or IOException) { }
        }
    }
}