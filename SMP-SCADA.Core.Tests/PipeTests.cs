using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;
using SMP_SCADA.Core.Service.Pipe;
using System.IO.Pipes;
using static SMP_SCADA.Core.Service.Contract.Command;

namespace SMP_SCADA.Core.Tests;

// End to end over a real named pipe: PipeSession on one side, PipeClient on the other.
public sealed class PipeTests : IDisposable
{
    static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
    readonly string _name = "smp-scada-test-" + Guid.NewGuid().ToString("N");
    readonly Rig _rig = new();
    readonly CommandQueue _queue = new();
    readonly CancellationTokenSource _stop = new();
    readonly List<Task> _running = [];

    public void Dispose()
    {
        _stop.Cancel();
        try { Task.WaitAll([.. _running], Wait); } catch (AggregateException) { /* cancelled, as intended */ }
    }

    // Stands in for the service's worker: answers queued commands and ticks the monitor.
    void StartWorker() => _running.Add(Task.Run(async () =>
    {
        while (!_stop.IsCancellationRequested)
        {
            _queue.HandleWaiting(_rig.Monitor);
            _rig.Run(0.1);
            await Task.Delay(10);
        }
    }));

    // Stands in for the service's pipe server, for one app.
    void StartServer(Func<NamedPipeServerStream, Task> serve) => _running.Add(Task.Run(async () =>
    {
        await using var pipe = new NamedPipeServerStream(_name, PipeDirection.InOut, 1,
                                                         PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        await pipe.WaitForConnectionAsync(_stop.Token);
        await serve(pipe);
    }));

    void StartSession(string user, Role role) => StartServer(pipe =>
        PipeSession.RunAsync(pipe, user, role, () => _rig.Monitor.Latest, _queue, TimeProvider.System, _stop.Token));

    [Fact]
    public async Task The_app_gets_hello_snapshots_and_answers()
    {
        _rig.Connect();
        StartWorker();
        StartSession("will", Role.Operator);

        await using var client = await PipeClient.ConnectAsync(Wait, _name);
        var snapshot = new TaskCompletionSource<Snapshot>();
        client.SnapshotReceived += s => snapshot.TrySetResult(s);

        Assert.Equal(new Hello(Protocol.Version, "will", Role.Operator), client.Hello);
        Assert.Equal(LinkState.PreOp, (await snapshot.Task.WaitAsync(Wait)).Link);

        var changed = Assert.IsType<Result>(await client.SendAsync(id => new SetBarDiameter(id, 350)));
        Assert.True(changed.Ok);
        var refused = Assert.IsType<Result>(await client.SendAsync(id => new SetDeadband(id, 5)));
        Assert.Equal("Needs the Engineer role.", refused.Err);
    }

    [Fact]
    public async Task An_unreadable_line_gets_a_reply_with_id_0()
    {
        StartSession("will", Role.Operator);

        await using var pipe = new NamedPipeClientStream(".", _name, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(Wait, CancellationToken.None);
        using var reader = new StreamReader(pipe);
        await using var writer = new StreamWriter(pipe) { AutoFlush = true };

        await writer.WriteLineAsync("this is not json");
        ServiceMessage message;
        do message = Protocol.ReadServiceMessage((await reader.ReadLineAsync().WaitAsync(Wait))!);
        while (message is not Result);                 // skip the Hello and any snapshot

        var result = (Result)message;
        Assert.Equal(0, result.Id);
        Assert.False(result.Ok);
    }

    [Fact]
    public async Task Losing_the_service_ends_the_client_and_fails_waiting_commands()
    {
        StartSession("will", Role.Operator);           // no worker, so commands are never answered
        await using var client = await PipeClient.ConnectAsync(Wait, _name);
        var waiting = client.SendAsync(id => new SetZero(id));

        _stop.Cancel();                                // the service stops

        await client.Completion.WaitAsync(Wait);
        await Assert.ThrowsAsync<IOException>(() => waiting);
    }
}