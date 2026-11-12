using System;
using System.Collections.Generic;
using System.Text;

namespace SMP_SCADA.Core.Drive;

public enum DataType { I8, U8, I16, U16, I32, U32};

// One value in the drive and how often to read it
public sealed record DriveObject(string Key, ushort Index, byte SubIndex, DataType Type,
                                 string Name, string Unit, TimeSpan Period);

public static class DriveObjects
{
    public static readonly TimeSpan OnceAtConnect = Timeout.InfiniteTimeSpan;
    static TimeSpan Ms(int ms) => TimeSpan.FromMilliseconds(ms);

    // Status
    public static readonly DriveObject Statusword = new("statusword", 0x6041, 0, DataType.U16, "Statusword", "bits", Ms(1000));
    public static readonly DriveObject ManufacturerStatus = new("manufacturer_status", 0x2000, 1, DataType.U32, "Manufacturer statusword", "bits", Ms(1000));
    public static readonly DriveObject ManufacturerWarnings = new("manufacturer_warnings", 0x2001, 1, DataType.U32, "Manufacturer warnings", "bits", Ms(1000));
    public static readonly DriveObject ErrorRegister = new("error_register", 0x1001, 0, DataType.U8, "Error register", "bits", Ms(1000));
    public static readonly DriveObject LastWarning = new("last_warning", 0x200F, 0, DataType.U16, "Last warning code", "code", Ms(1000));
    public static readonly DriveObject ModeDisplay = new("mode_display", 0x6061, 0, DataType.I8, "Mode of operation", "mode", Ms(1000));

    // Error history
    public static readonly DriveObject ErrorCount = new("error_count", 0x1003, 0, DataType.U8, "Errors stored", "count", Ms(2000));
    public static readonly DriveObject Error1 = new("error_1", 0x1003, 1, DataType.U32, "Newest error", "code", Ms(2000));
    public static readonly DriveObject Error2 = new("error_2", 0x1003, 2, DataType.U32, "Error 2", "code", Ms(2000));
    public static readonly DriveObject Error3 = new("error_3", 0x1003, 3, DataType.U32, "Error 3", "code", Ms(2000));
    public static readonly DriveObject Error4 = new("error_4", 0x1003, 4, DataType.U32, "Error 4", "code", Ms(2000));

    // Position
    public static readonly DriveObject PositionActual = new("position_actual", 0x6064, 0, DataType.I32, "Position actual", "counts", Ms(100));

    // Motion
    public static readonly DriveObject PositionDemand = new("position_demand", 0x6062, 0, DataType.I32, "Position demand", "counts", Ms(250));
    public static readonly DriveObject FollowingError = new("following_error", 0x60F4, 0, DataType.I32, "Following error", "counts", Ms(250));
    public static readonly DriveObject ControlEffort = new("control_effort", 0x60FA, 0, DataType.I32, "Control effort", "raw", Ms(250));
    public static readonly DriveObject VelocityActual = new("velocity_actual", 0x606C, 0, DataType.I32, "Velocity actual", "raw", Ms(250));
    public static readonly DriveObject VelocitySensor = new("velocity_sensor", 0x6069, 0, DataType.I32, "Velocity sensor actual", "raw", Ms(250));
    public static readonly DriveObject VelocityFiltered = new("velocity_filtered", 0x2074, 0, DataType.I32, "Velocity actual, filtered", "raw", Ms(250));
    public static readonly DriveObject RotorPosition = new("rotor_position", 0x6410, 0x0F, DataType.U16, "Rotor position", "‰ rev", Ms(250));

    // Load
    public static readonly DriveObject TorqueActual = new("torque_actual", 0x6077, 0, DataType.I16, "Torque actual", "raw", Ms(500));
    public static readonly DriveObject CurrentActual = new("current_actual", 0x6078, 0, DataType.I16, "Current actual", "raw", Ms(500));
    public static readonly DriveObject I2tMotor = new("i2t_motor", 0x6410, 0x04, DataType.U16, "Motor I²t", "‰", Ms(500));
    public static readonly DriveObject I2tDrive = new("i2t_drive", 0x6510, 0x3D, DataType.U16, "Drive I²t", "‰", Ms(500));

    // Supply and temperature
    public static readonly DriveObject DcLinkVoltage = new("dc_link_voltage", 0x6079, 0, DataType.U32, "DC link voltage", "raw", Ms(2000));
    public static readonly DriveObject DcLinkVoltageMv = new("dc_link_voltage_mv", 0x6510, 0x34, DataType.U32, "DC link voltage", "mV", Ms(2000));
    public static readonly DriveObject PowerStageTemp = new("power_stage_temp", 0x6510, 0x31, DataType.I16, "Power stage temperature", "°C", Ms(2000));
    public static readonly DriveObject MotorTemp = new("motor_temp", 0x6510, 0x2E, DataType.I16, "Motor temperature", "°C", Ms(2000));

    // Limits
    public static readonly DriveObject NominalCurrent = new("nominal_current", 0x6510, 0x40, DataType.U32, "Nominal current", "mA", OnceAtConnect);
    public static readonly DriveObject PeakCurrent = new("peak_current", 0x6510, 0x41, DataType.U32, "Peak current", "mA", OnceAtConnect);
    public static readonly DriveObject MaxPowerStageTemp = new("max_power_stage_temp", 0x6510, 0x32, DataType.I16, "Max power stage temperature", "°C", OnceAtConnect);

    // Scaling
    public static readonly DriveObject EncoderIncrements = new("encoder_increments", 0x608F, 1, DataType.U32, "Encoder increments", "count", OnceAtConnect);
    public static readonly DriveObject EncoderMotorRevs = new("encoder_motor_revs", 0x608F, 2, DataType.U32, "Encoder motor revolutions", "count", OnceAtConnect);
    public static readonly DriveObject PositionFactorNum = new("position_factor_num", 0x6093, 1, DataType.U32, "Position factor numerator", "", OnceAtConnect);
    public static readonly DriveObject PositionFactorDiv = new("position_factor_div", 0x6093, 2, DataType.U32, "Position factor divisor", "", OnceAtConnect);
    public static readonly DriveObject VelocityFactorNum = new("velocity_factor_num", 0x6094, 1, DataType.U32, "Velocity factor numerator", "", OnceAtConnect);
    public static readonly DriveObject VelocityFactorDiv = new("velocity_factor_div", 0x6094, 2, DataType.U32, "Velocity factor divisor", "", OnceAtConnect);
    public static readonly DriveObject PositionNotation = new("position_notation", 0x6089, 0, DataType.I8, "Position notation index", "", OnceAtConnect);
    public static readonly DriveObject PositionDimension = new("position_dimension", 0x608A, 0, DataType.U8, "Position dimension index", "", OnceAtConnect);
    public static readonly DriveObject VelocityNotation = new("velocity_notation", 0x608B, 0, DataType.I8, "Velocity notation index", "", OnceAtConnect);
    public static readonly DriveObject VelocityDimension = new("velocity_dimension", 0x608C, 0, DataType.U8, "Velocity dimension index", "", OnceAtConnect);

    // Digital I/O
    public static readonly DriveObject DigitalInputs = new("digital_inputs", 0x60FD, 0, DataType.U32, "Digital inputs", "bits", Ms(250));
    public static readonly DriveObject DigitalOutputs = new("digital_outputs", 0x60FE, 1, DataType.U32, "Digital outputs", "bits", Ms(250));

    // Configuration
    public static readonly DriveObject EnableLogic = new("enable_logic", 0x6510, 0x10, DataType.U16, "Enable logic", "code", Ms(1000));

    public static readonly IReadOnlyList<DriveObject> All =
    [
        Statusword, ManufacturerStatus, ManufacturerWarnings, ErrorRegister, LastWarning, ModeDisplay,
        ErrorCount, Error1, Error2, Error3, Error4,
        PositionActual,
        PositionDemand, FollowingError, ControlEffort, VelocityActual, VelocitySensor, VelocityFiltered, RotorPosition,
        TorqueActual, CurrentActual, I2tMotor, I2tDrive,
        DcLinkVoltage, DcLinkVoltageMv, PowerStageTemp, MotorTemp,
        NominalCurrent, PeakCurrent, MaxPowerStageTemp,
        EncoderIncrements, EncoderMotorRevs, PositionFactorNum, PositionFactorDiv,
        VelocityFactorNum, VelocityFactorDiv, PositionNotation, PositionDimension, VelocityNotation, VelocityDimension,
        DigitalInputs, DigitalOutputs,
        EnableLogic,
    ];
    public static readonly IReadOnlyDictionary<string, DriveObject> ByKey = All.ToDictionary(o => o.Key);
}