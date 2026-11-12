using SMP_SCADA.Core.Service.Contract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO.Pipes;
using System.Reflection;
using System.Text;

namespace SMP_SCADA.Core.Service.Pipe;

public sealed class PipeClient : IAsyncDisposable
{
    public static readonly TimeSpan ReplyTimeout = TimeSpan.FromSeconds(5);

    readonly NamedPipeClientStream _pipe;
    readonly StreamReader _reader;
    readonly StreamWriter _writer;
    readonly SemaphoreSlim _writeLock = new(1);
    readonly ConcurrentDictionary<int, TaskCompletionSource<ServiceMessage>> _waiting = new();
    int _lastId;

    PipeClient(NamedPipeClientStream pipe)
    {
        _pipe = pipe;
        _reader = new StreamReader(pipe);
        _writer = new StreamWriter(pipe) { AutoFlush = true };
    }

    public Hello Hello { get; private set; } = null;
    public Task Completion { get; private set; } = Task.CompletedTask;
    public event Action<Snapshot>? SnapshotReceived;

    public static async Task<PipeClient> ConnectAsync(TimeSpan timeout, string pipeName = Protocol.PipeName, CancellationToken ct = default)
    {
        var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous, System.Security.Principal.TokenImpersonationLevel.Identification);
        try
        {
            await pipe.ConnectAsync(timeout, ct);
            var client = new PipeClient(pipe);
            var first = await client.ReadAsync(ct) ?? throw new IOException("The service closed the connection");
            if (first is not Hello hello) throw new IOException($"Expected Hello, got {first.GetType().Name}.");
            if (hello.ProtocolVersion != Protocol.Version)
                throw new IOException($"The service speaks protocol {hello.ProtocolVersion}; this app speaks {Protocol.Version}.");

            client.Hello = hello;
            client.Completion = client.ReadLoopAsync();
            return client;
        }
        catch
        {
            await pipe.DisposeAsync();
            throw;
        }
    }
    public async Task<ServiceMessage> SendAsync(Func<int, Command> make)
    {
        int id = Interlocked.Increment(ref _lastId);
        var reply = new TaskCompletionSource<ServiceMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiting[id] = reply;
        try
        {
            if (Completion.IsCompleted) throw new IOException("Not connected to the service.");
            await _writeLock.WaitAsync();
            try { await _writer.WriteLineAsync(Protocol.Write(make(id))); }
            finally { _writeLock.Release(); }
            return await reply.Task.WaitAsync(ReplyTimeout);
        }
        finally
        {
            _waiting.TryRemove(id, out _);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _pipe.DisposeAsync();
        try { await Completion; } catch { /* the connection is going anyway */ }
    }

    async Task<ServiceMessage?> ReadAsync(CancellationToken ct) =>
        await _reader.ReadLineAsync(ct) is string line ? Protocol.ReadServiceMessage(line) : null;

    async Task ReadLoopAsync()
    {
        try
        {
            while (await ReadAsync(CancellationToken.None) is { } message)
            {
                if (message is Snapshot snapshot) SnapshotReceived?.Invoke(snapshot);
                else if (message is Reply r && _waiting.TryRemove(r.Id, out var reply)) reply.SetResult(message);
            }
        }
        catch (Exception e) when (e is IOException or ObjectDisposedException)
        {
            // pipe broken or closed
        }
        finally
        {
            foreach (var reply in _waiting.Values) reply.TrySetException(new IOException("Lost the connection to the service."));
        }
    }
}
