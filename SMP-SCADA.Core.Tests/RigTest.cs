using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;
using static SMP_SCADA.Core.Service.Contract.Command;

namespace SMP_SCADA.Core.Tests;

// A monitor wired to a simulated drive, a test clock and an in-memory store.
public sealed class Rig
{
    static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(100);

    public readonly ManualTime Time;
    public readonly MemoryStore Store;
    public readonly SimulatedDrive Drive;
    public readonly ShaftMonitor Monitor;

    // Pass in the parts of an earlier rig to simulate a service restart.
    public Rig(MemoryStore? store = null, SimulatedDrive? drive = null, ManualTime? time = null)
    {
        Time = time ?? new ManualTime();
        Store = store ?? new MemoryStore();
        Drive = drive ?? new SimulatedDrive(Time);
        Monitor = new ShaftMonitor(Drive, Store, Time, simulated: true);
    }

    public ServiceMessage Send(Command c, Role role = Role.Operator) => Monitor.Handle(c, "will", role);

    public void Run(double seconds)
    {
        for (int i = 0; i < seconds * 10; i++) { Time.Advance(Tick); Monitor.Tick(); }
    }

    public void Connect() => Assert.True(Assert.IsType<Result>(Send(new Connect(1, true))).Ok);
}