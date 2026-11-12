using System;
using System.Collections.Generic;
using System.Text;

namespace SMP_SCADA.Core;
public static class ShaftMaths
{
    /*
     * All the maths for the program
     * 
     * The original had a calibrated measurement where on a 200mm bar, 
     * one revolution read 2,776,350 counts, which fixes the surface travel 
     * per count for each bar diam. 
     */


    // Limits
    public const double MinDiameterMm = 25.0, MaxDiameterMm = 1500.0;
    // Keep an accurate running total instead of rolling over 
    public static int Delta(int newer, int older) => unchecked(newer - older);
    public static double DistanceMm(long counts, Calibration cal)
    {
        cal.Check();
        return counts / cal.CountsPerMm;
    }

    // Bar Angle 0..<360
    public static double AngleDeg(long counts, double barDiameterMm, Calibration cal)
    {
        cal.Check();
        CheckDiameter(barDiameterMm);
        double turns = counts * cal.DiameterMm / (cal.CountsPerRev * barDiameterMm);
        return (turns - Math.Floor(turns)) * 360.0;
    }

    public static double CountsPerRevFromTurns(long startCounts, long endCounts, int turns)
    {
        if (turns < 1) throw new ArgumentOutOfRangeException(nameof(turns));
        double perRev = Math.Abs((double)(endCounts - startCounts)) / turns;
        if (perRev <= 0) throw new ArgumentOutOfRangeException(nameof(endCounts), "The bar didn't move");
        return perRev;
    }

    public static void CheckDiameter(double diameterMm)
    {
        if (!(diameterMm >= MinDiameterMm && diameterMm <= MaxDiameterMm))
            throw new ArgumentOutOfRangeException(nameof(diameterMm));
    }
}