namespace SMP_SCADA.Core.Tests;

public class PositionTrackerTests
{
    static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;
    static DateTimeOffset Ms(int ms) => T0.AddMilliseconds(ms);

    [Fact]
    public void First_reading_is_zero()
    {
        var tracker = new PositionTracker();
        tracker.Update(5000, T0);

        Assert.Equal(0, tracker.CountsFromZero);
        Assert.Equal(0, tracker.TravelCounts);
        Assert.Null(tracker.SpeedCountsPerSecond);   // one reading can't give a speed
    }

    [Fact]
    public void Zero_resets_distance_and_travel()
    {
        var tracker = new PositionTracker();
        tracker.Update(1000, Ms(0));
        tracker.Update(3000, Ms(100));
        Assert.Equal(2000, tracker.CountsFromZero);

        tracker.SetZero();
        Assert.Equal(0, tracker.CountsFromZero);
        Assert.Equal(0, tracker.TravelCounts);

        tracker.Update(2500, Ms(200));
        Assert.Equal(-500, tracker.CountsFromZero);   // signed: behind zero
        Assert.Equal(500, tracker.TravelCounts);      // travel is always positive
    }

    [Fact]
    public void Distance_survives_int32_rollover()
    {
        var tracker = new PositionTracker();
        tracker.Update(int.MaxValue - 50, Ms(0));
        tracker.Update(int.MinValue + 49, Ms(100));   // moved +100 across the rollover

        Assert.Equal(100, tracker.CountsFromZero);
    }

    [Fact]
    public void Deadband_ignores_jitter_but_counts_slow_motion()
    {
        var tracker = new PositionTracker { DeadbandCounts = 100 };
        tracker.Update(0, Ms(0));
        tracker.Update(80, Ms(100));    // jitter out...
        tracker.Update(0, Ms(200));     // ...and back
        Assert.Equal(0, tracker.TravelCounts);

        tracker.Update(150, Ms(300));   // a real move
        Assert.Equal(150, tracker.TravelCounts);

        tracker.Update(210, Ms(400));   // slow: 60, under the deadband
        tracker.Update(270, Ms(500));   // 120 since the last count, so it adds up
        Assert.Equal(270, tracker.TravelCounts);
    }

    [Fact]
    public void Speed_is_signed_and_averaged_over_the_window()
    {
        var tracker = new PositionTracker();
        int position = 0, ms = 0;
        for (int i = 0; i <= 10; i++) { tracker.Update(position, Ms(ms)); position += 1000; ms += 100; }
        Assert.Equal(10_000, tracker.SpeedCountsPerSecond!.Value, 6);    // 1000 counts per 100 ms

        for (int i = 0; i <= 6; i++) { position -= 2000; tracker.Update(position, Ms(ms)); ms += 100; }
        Assert.Equal(-20_000, tracker.SpeedCountsPerSecond!.Value, 6);   // reversed, window refilled
    }

    [Fact]
    public void Clock_stepping_back_restarts_the_speed_window()
    {
        var tracker = new PositionTracker();
        tracker.Update(0, Ms(1000));
        tracker.Update(1000, Ms(1100));
        tracker.Update(2000, Ms(500));                // clock stepped back

        Assert.Null(tracker.SpeedCountsPerSecond);
    }

    [Fact]
    public void Saved_state_carries_on_after_a_restart()
    {
        var before = new PositionTracker();
        before.Update(1000, Ms(0));
        before.Update(4000, Ms(100));

        var after = new PositionTracker(before.State);
        after.Update(6000, Ms(5000));                 // drive kept counting while we were down

        Assert.Equal(5000, after.CountsFromZero);
        Assert.Equal(5000, after.TravelCounts);
    }

    [Fact]
    public void Negative_deadband_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PositionTracker { DeadbandCounts = -1 });
    }
}