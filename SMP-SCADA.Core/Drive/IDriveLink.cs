using SMP_SCADA.Core.Service.Contract;

namespace SMP_SCADA.Core.Drive;

public enum LinkState { Disconnected, PreOp, Lost }

public readonly record struct DriveIdentity(uint VendorId, uint ProductCode, string Name);

public interface IDriveLink
{
    LinkState State { get; }
    DriveIdentity? Identity { get; }

    int Connect(string adapter);           // returns slaves foundS
    void Disconnect();
    long? Read(DriveObject o);             // null = failed read
    bool Write(DriveObject o, long value); // only ever used to restore enable
    IReadOnlyList<NetworkAdapter> Adapters();
}