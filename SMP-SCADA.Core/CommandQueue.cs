using SMP_SCADA.Core.Service.Contract;
using System.Reflection.Metadata;
using System.Threading.Channels;

namespace SMP_SCADA.Core;

public sealed class CommandQueue
{
    readonly Channel<(Command Command, string User, Role Role, TaskCompletionSource<ServiceMessage> Reply)> _waiting =
        Channel.CreateUnbounded<(Command, string, Role, TaskCompletionSource<ServiceMessage>)>();

    public Task<ServiceMessage> Send(Command command, string user, Role role)
    {
        var reply = new TaskCompletionSource<ServiceMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiting.Writer.TryWrite((command, user, role, reply));
        return reply.Task;
    }
    public void HandleWaiting(ShaftMonitor monitor)
    {
        while (_waiting.Reader.TryRead(out var item)){
            try
            {
                item.Reply.SetResult(monitor.Handle(item.Command, item.User, item.Role));
            }
            catch(Exception e)
            {
                item.Reply.SetException(e);
                throw;
            }
        }
    }
}