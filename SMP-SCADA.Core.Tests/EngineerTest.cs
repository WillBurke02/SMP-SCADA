using SMP_SCADA.Core.Alarms;
using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;
using static SMP_SCADA.Core.Service.Contract.Command;

namespace SMP_SCADA.Core.Tests;

public class EngineerCommandsTests
{
    static Result AsEngineer(Rig rig, Command c) => Assert.IsType<Result>(rig.Send(c, Role.Engineer));

    static AlarmStatus? Alarm(Rig rig, string code) =>
        rig.Monitor.Latest.Alarms.Where(a => a.Info.Code == code).Cast<AlarmStatus?>().FirstOrDefault();

    [Fact]
    public void Restoring_enable_logic_writes_the_rule_value_and_clears_the_alarm()
    {
        var rig = new Rig();
        rig.Connect();
        rig.Run(0.1);
        Assert.Equal(AlarmState.ActiveUnacked, Alarm(rig, "ENABLE_LOGIC")?.State);

        Assert.True(AsEngineer(rig, new RestoreEnableLogic(1)).Ok);
        rig.Run(0.1);

        Assert.Equal(0, rig.Drive.Values[DriveObjects.EnableLogic.Key]);
        Assert.Equal(AlarmState.ClearedUnacked, Alarm(rig, "ENABLE_LOGIC")?.State);   // still shown until acknowledged
    }

    [Fact]
    public void A_failed_restore_is_refused_and_logged()
    {
        var rig = new Rig();
        rig.Connect();
        rig.Run(0.1);
        rig.Drive.DropReplies = true;

        Assert.False(AsEngineer(rig, new RestoreEnableLogic(1)).Ok);
        Assert.Contains(rig.Store.Events, e => e.Text.Contains("restore") && e.Text.Contains("failed"));
    }

    [Fact]
    public void Remeasure_proposes_a_calibration_and_only_saves_when_told()
    {
        var rig = new Rig();
        rig.Connect();
        rig.Run(0.1);
        Assert.True(AsEngineer(rig, new StartRemeasure(1)).Ok);

        rig.Drive.CountsPerSecond = 2_776_350 * 1.01;   // the real bar reads 1% more counts per turn
        rig.Run(10);                                    // ten turns
        Assert.True(AsEngineer(rig, new FinishRemeasure(2, Turns: 10, CalibrationDiameterMm: 200)).Ok);
        rig.Run(0.1);

        var proposed = Assert.IsType<Calibration>(rig.Monitor.Latest.Remeasure?.Proposed);   // fails if null
        Assert.InRange(proposed.CountsPerRev, 2_804_112, 2_804_115);
        Assert.Equal(Calibration.Default, rig.Store.SavedSettings!.Calibration);   // not saved yet

        Assert.True(AsEngineer(rig, new SaveRemeasure(3)).Ok);
        rig.Run(0.1);

        Assert.Equal(proposed, rig.Store.SavedSettings!.Calibration);
        Assert.Null(rig.Monitor.Latest.Remeasure);
        Assert.Contains(rig.Store.Events, e => e.Text.Contains("+1.00%"));
    }

    [Fact]
    public void Remeasure_steps_must_come_in_order()
    {
        var rig = new Rig();
        rig.Connect();
        rig.Run(0.1);

        Assert.False(AsEngineer(rig, new FinishRemeasure(1, 10, 200)).Ok);   // not started
        Assert.False(AsEngineer(rig, new SaveRemeasure(2)).Ok);              // nothing to save

        AsEngineer(rig, new StartRemeasure(3));
        Assert.True(AsEngineer(rig, new CancelRemeasure(4)).Ok);
        rig.Run(0.1);
        Assert.Null(rig.Monitor.Latest.Remeasure);
    }

    [Fact]
    public void Deadband_is_saved_and_the_adapter_only_changes_when_disconnected()
    {
        var rig = new Rig();
        Assert.True(AsEngineer(rig, new SetDeadband(1, 50)).Ok);
        Assert.Equal(50, rig.Store.SavedSettings!.DeadbandCount);

        Assert.True(AsEngineer(rig, new SetAdapter(2, "eth-a")).Ok);
        rig.Connect();
        Assert.False(AsEngineer(rig, new SetAdapter(3, "eth-b")).Ok);
        Assert.Equal("eth-a", rig.Store.SavedSettings!.Adapter);
    }

    [Fact]
    public void Alarm_rules_can_be_added_and_deleted_while_running()
    {
        var rig = new Rig();
        rig.Connect();
        var warm = new AlarmRule("STAGE_WARM", "Power stage warm", Priority.Medium, TimeSpan.Zero,
                                 DriveObjects.PowerStageTemp, RuleKind.HighLimit, 30);   // simulator reads 35 °C

   
    }
}