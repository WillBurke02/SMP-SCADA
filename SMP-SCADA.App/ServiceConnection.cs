using SMP_SCADA.Core.Service.Contract;
using SMP_SCADA.Core.Service.Pipe;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SMP_SCADA;

public sealed class ServiceConnection(SynchronizationContext ui)
{
    public static readonly TimeSpan RetryEvery = TimeSpan.FromSeconds(2);
    PipeClient? _client;
    public event Action<Snapshot>? SnapshotReceived;
    public event Action? StateChanged;
    public Hello? Hello { get; private set; }
    public string? Problem { get; private set; }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var client = await PipeClient.ConnectAsync(RetryEvery, ct: ct);
                client.SnapshotReceived += s => ui.Post(_ => SnapshotReceived?.Invoke(s), null);
                _client = client;
                SetState(client.Hello, null);
                await client.Completion.WaitAsync(ct);
                SetState(null, "Service Stopped");
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception)
            {
                SetState(null, "Service not running");
            }
            finally
            {
                _client = null;
            }
            try { await Task.Delay(RetryEvery, ct); }
            catch (OperationCanceledException){ return;  }
        }
    }

    public async Task<ServiceMessage> SendAsync(Func<int, Command> make)
    {
        if (_client is not { } client) return new Result(0, false, "Not connected to service");
        try
        {
            return await client.SendAsync(make);
        }
        catch(Exception e) when (e is not IOException or TimeoutException or ObjectDisposedException)
        {
            return new Result(0, false, e.Message);
        }
    }
    void SetState(Hello? hello, string? problem) => ui.Post(_ =>
    {
        Hello = hello;
        Problem = problem;
        StateChanged?.Invoke();
    }, null);
}
