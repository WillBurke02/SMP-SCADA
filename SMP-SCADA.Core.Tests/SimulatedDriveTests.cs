using SMP_SCADA.Core.Drive;

namespace SMP_SCADA.Core.Tests;

public class SimulatedDriveTests
{
    static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

    [Fact]
    public void Position_moves_with_time()
    {
        var time = new ManualTime();
        var drive = new SimulatedDrive(time, startPosition: 100);
        drive.Connect("sim");
        drive.CountsPerSecond = 1000;

        time.Advance(2 * OneSecond);

        Assert.Equal(2100, drive.Read(DriveObjects.PositionActual));
    }

    [Fact]
    public void Changing_speed_does_not_jump_position()
    {
        var time = new ManualTime();
        var drive = new SimulatedDrive(time);
        drive.Connect("sim");

        drive.CountsPerSecond = 1000;
        time.Advance(OneSecond);           // forward to 1000
        drive.CountsPerSecond = -500;
        time.Advance(2 * OneSecond);       // back to 0

        Assert.Equal(0, drive.Read(DriveObjects.PositionActual));
    }

    [Fact]
    public void Counter_rollover_looks_like_a_small_step()
    {
        var time = new ManualTime();
        var drive = new SimulatedDrive(time, startPosition: int.MaxValue - 100);
        drive.Connect("sim");
        drive.CountsPerSecond = 1000;

        int older = (int)drive.Read(DriveObjects.PositionActual)!.Value;
        time.Advance(OneSecond);
        int newer = (int)drive.Read(DriveObjects.PositionActual)!.Value;

        Assert.True(newer < 0);                              // it really did roll over
        Assert.Equal(1000, ShaftMaths.Delta(newer, older));  // and the step is still 1000
    }

    [Fact]
    public void Reads_fail_when_not_connected_or_faulted()
    {
        var drive = new SimulatedDrive(new ManualTime());
        Assert.Null(drive.Read(DriveObjects.Statusword));    // not connected yet

        drive.Connect("sim");
        Assert.Equal(0x0027, drive.Read(DriveObjects.Statusword));

        drive.DropReplies = true;
        Assert.Null(drive.Read(DriveObjects.Statusword));
        Assert.False(drive.Write(DriveObjects.EnableLogic, 0));

        drive.DropReplies = false;
        drive.LoseLink();
        Assert.Equal(LinkState.Lost, drive.State);
        Assert.Null(drive.Read(DriveObjects.Statusword));
    }

    [Fact]
    public void Connect_leaves_enable_logic_on_EtherCAT_and_a_write_restores_it()
    {
        var drive = new SimulatedDrive(new ManualTime());
        drive.Connect("sim");
        Assert.Equal(0x18, drive.Read(DriveObjects.EnableLogic));

        Assert.True(drive.Write(DriveObjects.EnableLogic, 0));
        Assert.Equal(0, drive.Read(DriveObjects.EnableLogic));
    }
}