using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;

namespace SMP_SCADA.Core.Alarms;

public enum RuleKind { HighLimit, LowLimit, BitSet, NotEqual }

// One alarm as data is a row in table
public sealed record AlarmRule(string Code, string Text, Priority Priority, TimeSpan OnDelay,
                               DriveObject Source, RuleKind Kind, long Limit, long Deadband = 0)
{
    public AlarmInfo Info => new(Code, Text, Priority, OnDelay);

    public bool? IsActive(IReadOnlyDictionary<DriveObject, Reading> values, bool wasActive) 
    {
        if (!values.TryGetValue(Source, out var r) || r.Quality != Quality.Good) return null;
        long band = wasActive ? Deadband : 0;
        return Kind switch
        {
            RuleKind.HighLimit => r.Value >= Limit - band,
            RuleKind.LowLimit => r.Value <= Limit + band,
            RuleKind.BitSet => (r.Value & Limit) != 0,
            RuleKind.NotEqual => r.Value != Limit,
            _ => false,
        };
    }
}

// Link alarms
public static class CommsAlarm
{
    public static readonly AlarmInfo Comms = new("COMMS", "Drive Comms Lost", Priority.High, TimeSpan.FromSeconds(1));
    public static bool IsActive(bool connectRequested, LinkState link, Quality positionQuality) =>
         connectRequested && (link != LinkState.PreOp || positionQuality != Quality.Good);
}

public static class Alarms
{
    public static readonly AlarmRule DriveFault = new("DRIVE_FAULT", "Drive fault", Priority.High, TimeSpan.Zero,
            DriveObjects.Statusword, RuleKind.BitSet, 0x0008);

    public static readonly AlarmRule DriveWarning = new("DRIVE_WARN", "Drive warning", Priority.Medium, TimeSpan.Zero,
            DriveObjects.Statusword, RuleKind.BitSet, 0x0080);
    public static readonly AlarmRule EnableLogic = new("ENABLE_LOGIC", "Drive enable source changed", Priority.High, TimeSpan.Zero,
            DriveObjects.EnableLogic, RuleKind.NotEqual, 0);
    public static readonly AlarmRule TempStage = new("TEMP_STAGE", "Power stage temperature high", Priority.Medium, TimeSpan.FromSeconds(5),
            DriveObjects.PowerStageTemp, RuleKind.HighLimit, 70);
    public static readonly AlarmRule I2TMotor = new("I2T_MOTOR", "Motor I²t high", Priority.Medium, TimeSpan.FromSeconds(5),
            DriveObjects.I2tMotor, RuleKind.HighLimit, 900);
    public static readonly AlarmRule I2TDrive = new("I2T_DRIVE", "Drive I²t high", Priority.Medium, TimeSpan.FromSeconds(5),
            DriveObjects.I2tDrive, RuleKind.HighLimit, 900);
    public static readonly IReadOnlyList<AlarmRule> All = [DriveFault, DriveWarning, EnableLogic, TempStage, I2TMotor, I2TDrive];
}