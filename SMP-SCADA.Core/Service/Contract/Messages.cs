using SMP_SCADA.Core.Alarms;
using SMP_SCADA.Core.Drive;
using System.Text.Json.Serialization;

namespace SMP_SCADA.Core.Service.Contract;

public enum Role { Operator, Engineer }

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Hello), nameof(Hello))]
[JsonDerivedType(typeof(Snapshot), nameof(Snapshot))]
[JsonDerivedType(typeof(Result), nameof(Result))]
[JsonDerivedType(typeof(HistoryData), nameof(HistoryData))]
[JsonDerivedType(typeof(EventsData), nameof(EventsData))]
[JsonDerivedType(typeof(AlarmRulesData), nameof(AlarmRulesData))]
[JsonDerivedType(typeof(AdaptersData), nameof(AdaptersData))]
public abstract record ServiceMessage;
public sealed record Hello(int ProtocolVersion, string User, Role Role) : ServiceMessage;
public sealed record Snapshot(
    DateTimeOffset Time,
    bool Simulated,
    LinkState Link,
    bool ConnectRequested,
    DriveIdentity? Drive,
    // Overview
    Quality PositionQuality,
    double? SurfaceSpeedMmS,
    double AngleDeg,
    double DistanceMm,
    double TravelMm,
    // Settings
    double BarDiameterMm,
    Calibration Calibration,
    int DeadbandCounts,
    DateTimeOffset? ZeroSetAt,
    string? ZeroSetBy,
    // misc
    IReadOnlyDictionary<string, Reading> Values,
    IReadOnlyList<AlarmStatus> Alarms,
    RemeasureStatus? Remeasure) : ServiceMessage;
// whether a calibration remeasure is in progress
public sealed record RemeasureStatus(DateTimeOffset StartedAt, long CountsMoved, Calibration? Proposed);
public static class DerivedValues
{
    public const string SurfaceSpeed = "surface_speed";
    public const string Angle = "angle";
    public const string Distance = "distance";
    public const string Travel = "travel";
    public static readonly IReadOnlySet<string> All = new HashSet<string> { SurfaceSpeed, Angle, Distance, Travel };
    public static bool HasHistory(string key) => All.Contains(key) || DriveObjects.ByKey.ContainsKey(key);
}

public abstract record Reply(int Id) : ServiceMessage;
public sealed record Result(int Id, bool Ok, string? Err = null) : Reply(Id);

public readonly record struct HistoryPoint(DateTimeOffset Time, double Value, Quality Quality);
public sealed record HistoryData(int Id, string Key, IReadOnlyList<HistoryPoint> Points) : Reply(Id);
public readonly record struct LoggedEvent(DateTimeOffset Time, string Kind, string Text, string? User);
public sealed record EventsData(int Id, IReadOnlyList<LoggedEvent> Events) : Reply(Id);

public sealed record AlarmRulesData(int Id, IReadOnlyList<AlarmRule> Rules) : Reply(Id);

public readonly record struct NetworkAdapter(string Name, string Description);
public sealed record AdaptersData(int Id, IReadOnlyList<NetworkAdapter> Adapters) : Reply(Id);