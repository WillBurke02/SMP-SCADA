namespace SMP_SCADA.Core.Tests;

// Test clock: time only moves when the test says so.
public sealed class ManualTime : TimeProvider
{
    DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => _now;
    public void Advance(TimeSpan by) => _now += by;
}