using SMP_SCADA.Core.Drive;

namespace SMP_SCADA.Core.Tests;

public class QualityTests
{
    static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    [Fact]
    public void Value_goes_stale_after_three_missed_periods()
    {
        var o = DriveObjects.PositionActual;            // read every 100 ms
        var sample = new Sample(123, T0);

        Assert.Equal(Quality.Good, sample.QualityAt(o, T0.AddMilliseconds(300), linkUp: true));
        Assert.Equal(Quality.Stale, sample.QualityAt(o, T0.AddMilliseconds(301), linkUp: true));
    }

    [Fact]
    public void Failed_read_or_lost_link_is_bad()
    {
        var o = DriveObjects.PositionActual;
        Assert.Equal(Quality.Bad, new Sample(123, T0, LastReadFailed: true).QualityAt(o, T0, linkUp: true));
        Assert.Equal(Quality.Bad, new Sample(123, T0).QualityAt(o, T0, linkUp: false));
    }

    [Fact]
    public void Read_once_objects_never_go_stale()
    {
        // OnceAtConnect is -1 ms underneath; multiplied by 3 it would make every value "stale".
        var sample = new Sample(80, T0);
        Assert.Equal(Quality.Good, sample.QualityAt(DriveObjects.MaxPowerStageTemp, T0.AddDays(1), linkUp: true));
    }
}