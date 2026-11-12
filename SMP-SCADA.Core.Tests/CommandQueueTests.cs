using SMP_SCADA.Core.Service.Contract;
using static SMP_SCADA.Core.Service.Contract.Command;

namespace SMP_SCADA.Core.Tests;

public class CommandQueueTests
{
    [Fact]
    public async Task A_command_is_answered_only_when_the_worker_handles_it()
    {
        var rig = new Rig();
        var queue = new CommandQueue();

        var answer = queue.Send(new SetBarDiameter(7, 350), "will", Role.Operator);
        Assert.False(answer.IsCompleted);                // nothing happens off the worker loop

        queue.HandleWaiting(rig.Monitor);

        var result = Assert.IsType<Result>(await answer);
        Assert.True(result.Ok);
        Assert.Equal(7, result.Id);
        Assert.Equal(350, rig.Store.SavedSettings!.BarDiameterMm);
    }
}