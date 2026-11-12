using SMP_SCADA.Core.Drive;

namespace SMP_SCADA.ViewModels;

// On/off indicator
public sealed record Lamp(string Name, bool On, bool Fault)
{
    public string State => !On ? "Off" : Fault ? "Fault" : "On";
}

 public sealed record KeyRow(string Name, string Index, string Text, IReadOnlyList<Lamp> Lamps);

// What the drive's coded values mean
// from -- Metronix "EtherCAT and CANopen Manual, smartServo BL 4000-C" v1.0.
public static class DriveText
{
    // 6510h_10h enable_logic
    // section 3.4.2.1
    public static string EnableSource(long v) => v switch
    {
        0x00 => "DIN5",
        0x01 => "DIN5 + parameterisation interface",
        0x02 => "DIN5 + CAN",
        0x03 => "DIN5 + PROFIBUS/PROFINET",
        0x08 => "DIN5 + EtherCAT",
        0x11 => "Parameterisation interface only",
        0x12 => "CAN only",
        0x13 => "PROFIBUS/PROFINET only",
        0x18 => "EtherCAT only",
        _ => "Unknown",
    } + $" ({v:X2}h)";

    // 6041h statusword
    // section 4.4
    public static string DriveState(long sw) => (sw & 0x4F) switch
    {
        0x00 => "Not ready to switch on",
        0x40 => "Switch on disabled",
        0x0F or 0x08 => "Fault",
        _ => (sw & 0x6F) switch
        {
            0x21 => "Ready to switch on",
            0x23 => "Switched on",
            0x27 => "Operation enabled",
            0x07 => "Quick stop active",
            _ => "Unknown",
        },
    };

    // 6061h modes_of_operation_display
    // section 5.1.2.2
    public static string Mode(long v) => v switch
    {
        -1 => "Unknown or changing",
        -11 => "User position",
        -13 => "User velocity",
        -14 => "User torque",
        1 => "Profile position",
        3 => "Profile velocity",
        4 => "Torque profile",
        6 => "Homing",
        7 => "Interpolated position",
        8 => "Cyclic synchronous position",
        _ => $"Mode {v}",
    };

    // Lamps
    static readonly (string, int, bool)[] StatusBits =   // 6041h section 4.5.1
        [("Warning", 7, true), 
        ("Moving", 8, false), 
        ("Target reached", 10, false), 
        ("Internal limit", 11, true), 
        ("setpoint acknowledge/speed 0/homing attained/ip mode active", 12, false),
        ("Following error", 13, true)];

    // 60FDh Digital_inputs
    // section 3.13.2.1
    static readonly (string, int, bool)[] InputBits =
        [("DIN0", 24, false), ("DIN1", 25, false), ("DIN2", 26, false), ("DIN3", 27, false), 
        ("DIN4", 29, false), ("DIN8", 28, false),
        ("Negative limit", 0, true), ("Positive limit", 1, true), ("Reference", 2, false), ("Enable or STO missing", 3, true)];

    // 60FEh Digital_outputs
    // section 3.13.2.2
    static readonly (string, int, bool)[] OutputBits =   
        [("Brake", 0, false), ("DOUT1", 25, false), ("DOUT2", 26, false), ("DOUT3", 27, false)];

    // 1001h error_register
    static readonly (string, int, bool)[] ErrorBits =    
        [("Generic", 0, true), ("Current", 1, true), ("Voltage", 2, true), ("Temperature", 3, true),
         ("Communication", 4, true), ("Profile", 5, true), ("Manufacturer", 7, true)];

    public static IReadOnlyList<KeyRow> KeyRows(Func<DriveObject, long?> read)
    {
        KeyRow Row(string name, DriveObject o, Func<long, string> text, (string, int, bool)[] bits)
        {
            long? v = read(o);
            return new(name, Index(o), v is long x ? text(x) : "—",
                       [.. bits.Select(b => new Lamp(b.Item1, v is long x && (x >> b.Item2 & 1) == 1, b.Item3))]);
        }

        return
        [
            Row("Drive state", DriveObjects.Statusword, DriveState, StatusBits),
            Row("Enable source", DriveObjects.EnableLogic, EnableSource, []),
            Row("Mode of operation", DriveObjects.ModeDisplay, Mode, []),
            Row("Digital inputs", DriveObjects.DigitalInputs, _ => "", InputBits),
            Row("Digital outputs", DriveObjects.DigitalOutputs, _ => "", OutputBits),
            Row("Error register", DriveObjects.ErrorRegister, v => v == 0 ? "None" : "", ErrorBits),
        ];
    }

    public static string Index(DriveObject o) => $"{o.Index:X4}h:{o.SubIndex:X2}";
}