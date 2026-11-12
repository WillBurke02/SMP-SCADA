using SMP_SCADA.Core;

namespace SMP_SCADA.Core.Tests;

public class ShaftMathTests
{
    [Theory]
    [InlineData(0L, 200.0, 0.0, 0.0)]
    [InlineData(1_388_175L, 200.0, 314.16, 180.0)]
    [InlineData(2_776_350L, 200.0, 628.32, 0.0)]
    [InlineData(2_776_350L, 400.0, 628.32, 180.0)]
    [InlineData(-1_388_175L, 200.0, -314.16, 180.0)]
    public void Reference_vectors(long counts, double barDiameterMm, double expectedMm, double expectedDeg)
    {
        var cal = Calibration.Default;
        Assert.Equal(expectedMm, ShaftMaths.DistanceMm(counts, cal), 2);
        Assert.Equal(expectedDeg, ShaftMaths.AngleDeg(counts, barDiameterMm, cal), 2);
    }

    [Fact]
    public void New_calibration_changes_distance()
    {
        var cal = Calibration.Default with { CountsPerRev = 2_776_350.0 * 1.01 };
        Assert.Equal(Math.PI * 200.0 / 1.01, ShaftMaths.DistanceMm(2_776_350, cal), 6);
    }

    [Fact]
    public void Remeasure_gives_counts_per_rev_in_either_direction()
    {
        Assert.Equal(2_776_350.0, ShaftMaths.CountsPerRevFromTurns(1_000, 1_000 + 27_763_500, 10));
        Assert.Equal(2_776_350.0, ShaftMaths.CountsPerRevFromTurns(1_000 + 27_763_500, 1_000, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShaftMaths.CountsPerRevFromTurns(0, 1_000, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShaftMaths.CountsPerRevFromTurns(5, 5, 10));
    }

    [Fact]
    public void Delta_survives_int32_wrap()
    {
        Assert.Equal(10, ShaftMaths.Delta(int.MinValue + 5, int.MaxValue - 4));
        Assert.Equal(-10, ShaftMaths.Delta(int.MaxValue - 4, int.MinValue + 5));
    }

    [Fact]
    public void Out_of_range_inputs_are_rejected()
    {
        var cal = Calibration.Default; 
        Assert.Throws<ArgumentOutOfRangeException>(() => ShaftMaths.AngleDeg(0, ShaftMaths.MinDiameterMm - 0.1, cal));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShaftMaths.AngleDeg(0, ShaftMaths.MaxDiameterMm + 0.1, cal));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShaftMaths.CheckDiameter(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShaftMaths.DistanceMm(0, default));
        Assert.Throws<ArgumentOutOfRangeException>(() => ShaftMaths.DistanceMm(0, cal with { CountsPerRev = -1 }));
    }
}