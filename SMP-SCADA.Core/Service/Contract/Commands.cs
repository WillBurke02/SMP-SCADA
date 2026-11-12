using System.Text.Json.Serialization;

namespace SMP_SCADA.Core.Service.Contract;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SetZero), nameof(SetZero))]
[JsonDerivedType(typeof(SetBarDiameter), nameof(SetBarDiameter))]
[JsonDerivedType(typeof(Acknowledge), nameof(Acknowledge))]
[JsonDerivedType(typeof(Connect), nameof(Connect))]
[JsonDerivedType(typeof(Disconnect), nameof(Disconnect))]
[JsonDerivedType(typeof(GetHistory), nameof(GetHistory))]
[JsonDerivedType(typeof(GetEvents), nameof(GetEvents))]
[JsonDerivedType(typeof(GetAdapters), nameof(GetAdapters))]
[JsonDerivedType(typeof(RestoreEnableLogic), nameof(RestoreEnableLogic))]
[JsonDerivedType(typeof(SetDeadband), nameof(SetDeadband))]
[JsonDerivedType(typeof(SetAdapter), nameof(SetAdapter))]
[JsonDerivedType(typeof(StartRemeasure), nameof(StartRemeasure))]
[JsonDerivedType(typeof(FinishRemeasure), nameof(FinishRemeasure))]
[JsonDerivedType(typeof(SaveRemeasure), nameof(SaveRemeasure))]
[JsonDerivedType(typeof(CancelRemeasure), nameof(CancelRemeasure))]
public abstract record Command(int Id)
{
    public virtual void Validate() { }
    [JsonIgnore]
    public Role RequiredRole => this switch
    {
        SetZero or SetBarDiameter or Acknowledge or Connect or Disconnect
            or GetHistory or GetEvents or GetAdapters
        => Role.Operator,
        _ => Role.Engineer
    };
    protected static void CheckRange(DateTimeOffset from, DateTimeOffset to, TimeSpan max)
    {
        if (to <= from) throw new ArgumentException("'To' must be after 'From'.");
        if (to - from > max) throw new ArgumentException($"At most {max.TotalDays:0} days at a time");
    }
    protected static void Required(string? text, string what)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException($"{what} is required.");
    }

    // Operator cmds
    public sealed record SetZero(int Id) : Command(Id);
    public sealed record SetBarDiameter(int Id, double Mm) : Command(Id)
    {
        public override void Validate() => ShaftMaths.CheckDiameter(Mm);
    }
    public sealed record Acknowledge(int Id) : Command(Id);

    public sealed record Connect(int Id, bool SafeToStop) : Command(Id)
    {
        public override void Validate()
        {
            if (!SafeToStop) throw new ArgumentException("Tank is still running");
        }
    }
    public sealed record Disconnect(int Id) : Command(Id);
    public sealed record GetHistory(int Id, string Key, DateTimeOffset From, DateTimeOffset To) : Command(Id)
    {
        public static readonly TimeSpan MaxSpan = TimeSpan.FromDays(31);
        public override void Validate()
        {
            if (!DerivedValues.HasHistory(Key)) throw new ArgumentException($"No History for '{Key}.");
            CheckRange(From, To, MaxSpan);
        }
    }
    public sealed record GetEvents(int Id, DateTimeOffset From, DateTimeOffset To) : Command(Id)
    {
        public override void Validate() => CheckRange(From, To, GetHistory.MaxSpan);
    }

    public sealed record GetAdapters(int Id) : Command(Id);

    // Engineer Cmds
    public sealed record RestoreEnableLogic(int Id) : Command(Id);
    public sealed record SetDeadband(int Id, int Counts) : Command(Id)
    {
        public override void Validate() => ArgumentOutOfRangeException.ThrowIfNegative(Counts);
    }
    public sealed record SetAdapter(int Id, string Name) : Command(Id)
    {
        public override void Validate() => Required(Name, "Adapter name");
    }

    public sealed record StartRemeasure(int Id) : Command(Id);

    public sealed record FinishRemeasure(int Id, int Turns, double CalibrationDiameterMm) : Command(Id)
    {
        public override void Validate()
        {
            if (Turns < 1) throw new ArgumentOutOfRangeException(nameof(Turns));
            ShaftMaths.CheckDiameter(CalibrationDiameterMm);
        }
    }

    public sealed record SaveRemeasure(int Id) : Command(Id);

    public sealed record CancelRemeasure(int Id) : Command(Id);
}
