using System;
using System.Collections.Generic;
using System.Text;

namespace SMP_SCADA.Core;
// How many drive position counts per revolution of a bar of DiameterMm
public readonly record struct Calibration(double DiameterMm, double CountsPerRev)
{
    public static readonly Calibration Default = new(200.0, 2_776_350.0);

    public double CountsPerMm => CountsPerRev / (Math.PI * DiameterMm);

    public void Check()
    {
        ShaftMaths.CheckDiameter(DiameterMm);
        if (!(CountsPerRev > 0 && double.IsFinite(CountsPerRev)))
            throw new ArgumentOutOfRangeException(nameof(CountsPerRev));
    }
}