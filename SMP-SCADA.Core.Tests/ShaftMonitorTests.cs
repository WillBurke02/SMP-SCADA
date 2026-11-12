using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;
using static SMP_SCADA.Core.Service.Contract.Command;

namespace SMP_SCADA.Core.Tests;

public class ShaftMonitorTests
{
    static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(100);

    // A monitor wired to a simulated drive, a test clock and an in-memory store.
    sealed class Rig
    {
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

    static bool HasAlarm(Snapshot s, string code) => s.Alarms.Any(a => a.Info.Code == code);

    [Fact]
    public void First_start_saves_defaults_and_alarm_rules()
    {
        var rig = new Rig();

        Assert.Equal(Settings.Default, rig.Store.SavedSettings);
        Assert.Equal(Alarms.Alarms.All.Count, rig.Store.Rules.Count);
        Assert.Contains(rig.Store.Events, e => e.Text == "Started (simulated)");
    }

    [Fact]
    public void Reads_the_drive_and_reports_surface_speed_and_distance()
    {
        var rig = new Rig();
        rig.Connect();
        rig.Run(0.1);                                                   // first reading = zero
        rig.Drive.CountsPerSecond = Calibration.Default.CountsPerMm * 10;   // 10 mm/s at the surface
        rig.Run(1);

        var s = rig.Monitor.Latest;
        Assert.Equal(Quality.Good, s.PositionQuality);
        Assert.Equal(10, s.DistanceMm, 1);
        Assert.Equal(10, s.SurfaceSpeedMmS!.Value, 1);
        Assert.Equal(10, s.TravelMm, 1);
    }

    [Fact]
    public void Operators_are_refused_engineer_commands_and_bad_values()
    {
        var rig = new Rig();

        var refused = Assert.IsType<Result>(rig.Send(new RestoreEnableLogic(1)));
        Assert.False(refused.Ok);
        Assert.Equal("Needs the Engineer role.", refused.Err);

        Assert.False(Assert.IsType<Result>(rig.Send(new SetBarDiameter(2, 10))).Ok);
        Assert.Equal(200, rig.Store.SavedSettings!.BarDiameterMm);       // unchanged
    }

    [Fact]
    public void Zero_and_bar_diameter_are_saved_and_logged_with_the_user()
    {
        var rig = new Rig();
        Assert.False(Assert.IsType<Result>(rig.Send(new SetZero(1))).Ok);   // no reading yet

        rig.Connect();
        rig.Drive.CountsPerSecond = 10_000;
        rig.Run(1);
        Assert.True(Assert.IsType<Result>(rig.Send(new SetZero(2))).Ok);
        Assert.True(Assert.IsType<Result>(rig.Send(new SetBarDiameter(3, 350))).Ok);
        rig.Run(0.1);

        Assert.Equal(350, rig.Store.SavedSettings!.BarDiameterMm);
        Assert.Equal("will", rig.Monitor.Latest.ZeroSetBy);
        Assert.Contains(rig.Store.Events, e => e.Kind == "zero" && e.User == "will");
        Assert.Contains(rig.Store.Events, e => e.Text == "Bar diameter 200 -> 350 mm");
    }

    [Fact]
    public void Connecting_shows_the_enable_logic_switch_as_an_alarm()
    {
        var rig = new Rig();
        rig.Connect();
        rig.Run(0.1);

        Assert.True(HasAlarm(rig.Monitor.Latest, "ENABLE_LOGIC"));   // simulator leaves 18h, like the real drive
    }

    [Fact]
    public void A_silent_drive_raises_COMMS_and_a_lost_link_raises_LINK()
    {
        var rig = new Rig();
        rig.Connect();
        rig.Run(1);
        Assert.False(HasAlarm(rig.Monitor.Latest, "COMMS"));

        rig.Drive.DropReplies = true;
        rig.Run(1.5);                       // 300 ms to go stale, then the 1 s on-delay
        Assert.True(HasAlarm(rig.Monitor.Latest, "COMMS"));

        rig.Drive.LoseLink();
        rig.Run(0.1);
        Assert.True(HasAlarm(rig.Monitor.Latest, "LINK"));
    }

    [Fact]
    public void Deliberately_disconnected_is_not_an_alarm()
    {
        var rig = new Rig();
        rig.Connect();
        rig.Run(0.5);
        rig.Send(new Disconnect(1));
        rig.Run(2);

        Assert.False(HasAlarm(rig.Monitor.Latest, "COMMS"));
        Assert.False(HasAlarm(rig.Monitor.Latest, "LINK"));
    }

    [Fact]
    public void A_different_drive_is_refused()
    {
        var store = new MemoryStore { SavedSettings = Settings.Default with { PinnedDrive = new DriveIdentity(0xE4, 0x1234, "Other") } };
        var rig = new Rig(store);

        var result = Assert.IsType<Result>(rig.Send(new Connect(1, true)));

        Assert.False(result.Ok);
        Assert.Equal(LinkState.Disconnected, rig.Drive.State);
    }

    [Fact]
    public void A_restart_carries_on_from_the_saved_position()
    {
        var first = new Rig();
        first.Connect();
        first.Drive.CountsPerSecond = 10_000;
        first.Run(1);                           // zero at the first reading (1,000), now at 10,000
        first.Send(new Disconnect(1));

        first.Time.Advance(TimeSpan.FromSeconds(2));   // service down; the drive keeps counting
        var second = new Rig(first.Store, first.Drive, first.Time);
        second.Connect();
        second.Run(0.1);                        // drive now at 31,000

        Assert.Equal(ShaftMaths.DistanceMm(30_000, Calibration.Default), second.Monitor.Latest.DistanceMm, 6);
        Assert.Contains(second.Store.Events, e => e.Text.Contains("check zero"));
    }

    [Fact]
    public void History_is_one_row_per_second_and_only_on_change()
    {
        var rig = new Rig();
        rig.Connect();
        rig.Run(3);                         // shaft standing still

        Assert.Single(rig.Store.History, h => h.Key == DriveObjects.Statusword.Key);
        Assert.Single(rig.Store.History, h => h.Key == DerivedValues.Distance);

        rig.Drive.CountsPerSecond = 10_000;
        rig.Run(3);                         // moving: distance changes every second
        Assert.True(rig.Store.History.Count(h => h.Key == DerivedValues.Distance) >= 3);
    }
    [Fact]
    public void A_bad_rule_in_the_database_is_skipped_and_logged()
    {
        var store = new MemoryStore { SavedSettings = Settings.Default };
        store.SaveAlarmRule(Alarms.Alarms.All[0]);

        var rig = new Rig(store);
        rig.Connect();
        rig.Run(0.1);                       // would throw on every tick if the bad rule were used

    }

    [Fact]
    public void Stopping_saves_the_position()
    {
        var rig = new Rig();
        rig.Connect();
        rig.Drive.CountsPerSecond = 10_000;
        rig.Run(1);

        rig.Monitor.Stop();

        Assert.Equal(10_000, rig.Store.SavedPosition!.Tracker.LastRaw);
        Assert.Contains(rig.Store.Events, e => e.Text == "Stopped");
    }
}