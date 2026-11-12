using SMP_SCADA.Core.Drive;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMP_SCADA.Core;

public enum Quality { Good, Stale, Bad }
public readonly record struct Reading(long Value, Quality Quality);
public readonly record struct Sample(long Value, DateTimeOffset Time, bool LastReadFailed = false)
{
    public const int StaleAfterPeriods = 3;

    public Quality QualityAt(DriveObject o, DateTimeOffset now, bool linkUp)
    {
        if (!linkUp || LastReadFailed) return Quality.Bad;
        if (o.Period == DriveObjects.OnceAtConnect) return Quality.Good;  
        return now - Time > o.Period * StaleAfterPeriods ? Quality.Stale : Quality.Good;
    }
}