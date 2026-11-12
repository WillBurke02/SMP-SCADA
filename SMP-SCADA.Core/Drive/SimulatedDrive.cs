using SMP_SCADA.Core.Service.Contract;

namespace SMP_SCADA.Core.Drive;

// A fake drive for tests and for running the whole program without hardware (--simulate).
public sealed class SimulatedDrive : IDriveLink
{
    readonly TimeProvider _time;
    long _basePosition;
    DateTimeOffset _baseTime;
    double _countsPerSecond;

    public SimulatedDrive(TimeProvider time, int startPosition = 0)
    {
        _time = time;
        _basePosition = startPosition;
        _baseTime = time.GetUtcNow();

        foreach (var o in DriveObjects.All) Values[o.Key] = 0;
        Values[DriveObjects.Statusword.Key] = 0x0027;
        Values[DriveObjects.EnableLogic.Key] = 0x18;
        Values[DriveObjects.PowerStageTemp.Key] = 35;
        Values[DriveObjects.MaxPowerStageTemp.Key] = 80;
    }

    public Dictionary<string, long> Values { get; } = new();
    public bool DropReplies { get; set; }   // fault: drive stops answering, link stays up
    public LinkState State { get; private set; }
    public DriveIdentity? Identity { get; private set; }

    // Changing speed restarts the sum from the current position
    public double CountsPerSecond
    {
        get => _countsPerSecond;
        set
        {
            _basePosition = Unwrapped();
            _baseTime = _time.GetUtcNow();
            _countsPerSecond = value;
        }
    }

    public int Connect(string adapter)
    {
        State = LinkState.PreOp;
        Identity = new DriveIdentity(0, 0, "Simulated BL 4320-C");
        return 1;
    }

    public void Disconnect() => State = LinkState.Disconnected;
    public void LoseLink() => State = LinkState.Lost;   // fault: cable pulled or drive off

    public long? Read(DriveObject o)
    {
        if (State != LinkState.PreOp || DropReplies) return null;
        if (o == DriveObjects.PositionActual) return unchecked((int)Unwrapped());
        return Values.TryGetValue(o.Key, out long v) ? v : null;
    }

    public bool Write(DriveObject o, long value)
    {
        if (State != LinkState.PreOp || DropReplies) return false;
        Values[o.Key] = value;
        return true;
    }

    public IReadOnlyList<NetworkAdapter> Adapters() => [new("sim", "Simulated Adapter")];
    long Unwrapped() =>
        _basePosition + (long)(_countsPerSecond * (_time.GetUtcNow() - _baseTime).TotalSeconds);
}