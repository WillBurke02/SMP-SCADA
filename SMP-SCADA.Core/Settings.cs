using SMP_SCADA.Core.Drive;

namespace SMP_SCADA.Core;

public sealed record Settings(double BarDiameterMm, Calibration Calibration, int DeadbandCount, string Adapter, DriveIdentity? PinnedDrive)
{
    public static readonly Settings Default = new(200.0, Calibration.Default, 0, "", null);
}
public sealed record SavedPosition(TrackerState Tracker, DateTimeOffset? ZeroSetAt, string? ZeroSetBy);