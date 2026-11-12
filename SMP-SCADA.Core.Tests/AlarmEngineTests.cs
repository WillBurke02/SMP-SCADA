using SMP_SCADA.Core.Alarms;

namespace SMP_SCADA.Core.Tests;

public class AlarmEngineTests
{
    static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;
    static readonly AlarmInfo Test = new("TEST", "Test alarm", Priority.High, TimeSpan.Zero);

    static List<AlarmEvent> Run(AlarmEngine engine, bool condition, DateTimeOffset at, AlarmInfo? info = null) =>
        engine.Evaluate([(info ?? Test, condition)], at);

    [Fact]
    public void Alarm_stays_until_cleared_and_acknowledged()
    {
        var engine = new AlarmEngine();
        Run(engine, true, T0);
        Assert.Equal(AlarmState.ActiveUnacked, engine.StateOf("TEST"));

        Run(engine, false, T0);
        Assert.Equal(AlarmState.ClearedUnacked, engine.StateOf("TEST"));   // gone, but still showing

        engine.AcknowledgeAll("will", T0);
        Assert.Equal(AlarmState.Normal, engine.StateOf("TEST"));
        Assert.Empty(engine.Showing);
    }

    [Fact]
    public void Acknowledged_alarm_clears_by_itself()
    {
        var engine = new AlarmEngine();
        Run(engine, true, T0);
        engine.AcknowledgeAll("will", T0);
        Assert.Equal(AlarmState.ActiveAcked, engine.StateOf("TEST"));

        Run(engine, false, T0);
        Assert.Equal(AlarmState.Normal, engine.StateOf("TEST"));
    }

    [Fact]
    public void Returning_before_acknowledge_is_active_again()
    {
        var engine = new AlarmEngine();
        Run(engine, true, T0);
        Run(engine, false, T0);
        Run(engine, true, T0);
        Assert.Equal(AlarmState.ActiveUnacked, engine.StateOf("TEST"));
    }

    [Fact]
    public void On_delay_needs_the_condition_held_throughout()
    {
        var engine = new AlarmEngine();
        var slow = Test with { OnDelay = TimeSpan.FromSeconds(1) };

        Run(engine, true, T0, slow);
        Run(engine, true, T0.AddMilliseconds(900), slow);
        Assert.Equal(AlarmState.Normal, engine.StateOf("TEST"));

        Run(engine, false, T0.AddMilliseconds(950), slow);    // a blip resets the timer
        Run(engine, true, T0.AddMilliseconds(1000), slow);
        Run(engine, true, T0.AddMilliseconds(1900), slow);
        Assert.Equal(AlarmState.Normal, engine.StateOf("TEST"));

        Run(engine, true, T0.AddMilliseconds(2000), slow);
        Assert.Equal(AlarmState.ActiveUnacked, engine.StateOf("TEST"));
    }

    [Fact]
    public void Every_change_is_reported_once_with_the_user_on_acknowledge()
    {
        var engine = new AlarmEngine();
        var raised = Run(engine, true, T0);
        var again = Run(engine, true, T0.AddSeconds(1));        // still active: nothing new
        var acked = engine.AcknowledgeAll("will", T0.AddSeconds(2));

        Assert.Equal(AlarmState.ActiveUnacked, Assert.Single(raised).State);
        Assert.Empty(again);
        var ack = Assert.Single(acked);
        Assert.Equal(AlarmState.ActiveAcked, ack.State);
        Assert.Equal("will", ack.User);
    }

    [Fact]
    public void Deleted_rule_is_closed_in_the_log_and_forgotten()
    {
        var engine = new AlarmEngine();
        Run(engine, true, T0);

        var events = engine.Evaluate([], T0.AddSeconds(1));      // rule no longer in the list

        Assert.Equal(AlarmState.Normal, Assert.Single(events).State);
        Assert.Empty(engine.Showing);
    }

    [Fact]
    public void Showing_lists_most_severe_first_then_newest()
    {
        var engine = new AlarmEngine();
        var older = new AlarmInfo("OLD", "Older high", Priority.High, TimeSpan.Zero);
        var newer = new AlarmInfo("NEW", "Newer high", Priority.High, TimeSpan.Zero);
        var medium = new AlarmInfo("MED", "Medium", Priority.Medium, TimeSpan.Zero);

        engine.Evaluate([(older, true), (newer, false), (medium, true)], T0);
        engine.Evaluate([(older, true), (newer, true), (medium, true)], T0.AddSeconds(1));

        Assert.Equal(new[] { "NEW", "OLD", "MED" }, engine.Showing.Select(s => s.Info.Code));
    }

    [Fact]
    public void Duplicate_codes_are_refused()
    {
        var engine = new AlarmEngine();
        Assert.Throws<InvalidOperationException>(() => engine.Evaluate([(Test, true), (Test, false)], T0));
    }
}