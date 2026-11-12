using SMP_SCADA.Core.Alarms;
using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;
using System.Text.Json;
using System.Text.Json.Serialization;
using static SMP_SCADA.Core.Service.Contract.Command;

namespace SMP_SCADA.Core.Tests;

public class ProtocolTests
{
    static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    // One of every command. A test below fails if a new command is added without one here.
    static readonly Command[] Samples =
    [
        new SetZero(1), new SetBarDiameter(2, 350), new Acknowledge(3), new Connect(4, true),
        new Disconnect(5), new GetHistory(6, DerivedValues.SurfaceSpeed, T0, T0.AddHours(1)),
        new GetEvents(7, T0, T0.AddDays(1)), new GetAdapters(9),
        new RestoreEnableLogic(10), new SetDeadband(11, 20), new SetAdapter(12, @"\Device\NPF_{X}"),
        new StartRemeasure(15), new FinishRemeasure(16, 10, 200), new SaveRemeasure(17), new CancelRemeasure(18),
    ];

    [Fact]
    public void Every_command_survives_the_trip_and_is_one_line()
    {
        foreach (var command in Samples)
        {
            string line = Protocol.Write(command);
            Assert.DoesNotContain("\n", line);
            Assert.Equal(command, Protocol.ReadCommand(line));
        }
    }

    [Fact]
    public void Every_command_type_is_registered_and_has_a_sample()
    {
        var types = typeof(Command).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Command)) && !t.IsAbstract).ToList();
        var registered = typeof(Command).GetCustomAttributes(typeof(JsonDerivedTypeAttribute), false)
            .Cast<JsonDerivedTypeAttribute>().Select(a => a.DerivedType).ToList();

        Assert.Equal(types.Count, registered.Count);
        Assert.All(types, t => Assert.Contains(t, registered));
        Assert.All(types, t => Assert.Contains(Samples, s => s.GetType() == t));
    }

    [Fact]
    public void Snapshot_survives_the_trip()
    {
        var snapshot = new Snapshot(T0, Simulated: true, LinkState.PreOp, ConnectRequested: true,
            new DriveIdentity(0xE4, 0x8212, "BL 4320-C"), Quality.Good,
            SurfaceSpeedMmS: 185.6, AngleDeg: 20.95, DistanceMm: 2263.11, TravelMm: 5000,
            BarDiameterMm: 350, Calibration.Default, DeadbandCounts: 20, ZeroSetAt: T0, ZeroSetBy: "will",
            new Dictionary<string, Reading> { [DriveObjects.Statusword.Key] = new(0x0027, Quality.Good) },
            [new AlarmStatus(CommsAlarm.Comms, AlarmState.ActiveUnacked, T0)],
            Remeasure: null);

        var back = Assert.IsType<Snapshot>(Protocol.ReadServiceMessage(Protocol.Write(snapshot)));

        Assert.Equal(185.6, back.SurfaceSpeedMmS);
        Assert.Equal(Calibration.Default, back.Calibration);
        Assert.Equal(new Reading(0x0027, Quality.Good), back.Values[DriveObjects.Statusword.Key]);
        Assert.Equal(snapshot.Alarms[0], back.Alarms[0]);
        Assert.Equal("will", back.ZeroSetBy);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"type\":\"NoSuchCommand\",\"Id\":1}")]
    [InlineData("{\"Id\":1}")]                                        // no type
    [InlineData("{\"type\":\"SetBarDiameter\",\"Id\":1}")]            // missing Mm
    [InlineData("{\"type\":\"SetAdapter\",\"Id\":1,\"Name\":null}")]  // null where not allowed
    public void Malformed_lines_throw_JsonException(string line)
    {
        Assert.Throws<JsonException>(() => Protocol.ReadCommand(line));
    }

    [Fact]
    public void Operators_get_the_day_to_day_commands_and_everything_else_needs_an_engineer()
    {
        Assert.Equal(Role.Operator, new SetZero(1).RequiredRole);
        Assert.Equal(Role.Operator, new SetBarDiameter(1, 350).RequiredRole);
        Assert.Equal(Role.Operator, new Acknowledge(1).RequiredRole);
        Assert.Equal(Role.Engineer, new RestoreEnableLogic(1).RequiredRole);
        Assert.Equal(Role.Engineer, new FinishRemeasure(1, 10, 200).RequiredRole);
    }

    [Fact]
    public void Commands_check_their_own_values()
    {
        foreach (var command in Samples) command.Validate();   // all the samples are valid

        Assert.ThrowsAny<ArgumentException>(() => new SetBarDiameter(1, 10).Validate());
        Assert.ThrowsAny<ArgumentException>(() => new Connect(1, SafeToStop: false).Validate());
        Assert.ThrowsAny<ArgumentException>(() => new SetDeadband(1, -1).Validate());
        Assert.ThrowsAny<ArgumentException>(() => new SetAdapter(1, " ").Validate());
        Assert.ThrowsAny<ArgumentException>(() => new GetHistory(1, "no_such_value", T0, T0.AddHours(1)).Validate());
        Assert.ThrowsAny<ArgumentException>(() => new GetHistory(1, DerivedValues.Angle, T0, T0).Validate());
        Assert.ThrowsAny<ArgumentException>(() => new GetEvents(1, T0, T0.AddDays(40)).Validate());
        Assert.ThrowsAny<ArgumentException>(() => new FinishRemeasure(1, 0, 200).Validate());
    }
}