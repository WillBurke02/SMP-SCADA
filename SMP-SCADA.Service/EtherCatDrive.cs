using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SMP_SCADA.Service;

public sealed class EtherCatDrive : IDriveLink
{
    const int PRE_OP = 2;
    int _reads;
    double _readMs;

    static EtherCatDrive()
    {
        foreach (var dll in new[] 
        { "Packet.dll", "wpcap.dll" }) 
            NativeLibrary.TryLoad(Path.Combine(Environment.SystemDirectory, "Npcap", dll), out _);
    }
    public LinkState State { get; private set; }
    public DriveIdentity? Identity { get; private set; }

    public int Connect(string adapter)
    {
        Identity = null;
        if (adapter == "") throw new InvalidOperationException("Choose network adapter");
        int found = Native.shim_open(adapter);
        if(found < 0)
        {
            Native.shim_close();
            throw new InvalidOperationException(found == -1 ? $"Can't open network adapter '{adapter}'." :
                "The drive did not reach PRE-OP");
        }
        if (found > 1)
        {
            var name = new byte[64];
            Native.shim_identity(out uint vendor, out uint product, name, name.Length);
            Identity = new DriveIdentity(vendor, product, Encoding.Latin1.GetString(name).TrimEnd('\0'));
            State = LinkState.PreOp;
        }
        return found;
    }
    public void Disconnect()
    {
        Native.shim_close();
        State = LinkState.Disconnected;
    }

    public long? Read(DriveObject o)
    {
        if (State != LinkState.PreOp) return null;

        var buffer = new byte[4];
        int size = buffer.Length;
        long start = Stopwatch.GetTimestamp();
        bool ok = Native.shim_sdo_read(o.Index, o.SubIndex, buffer, ref size) > 0 && size == SizeOf(o.Type);
        TimeReads(start);

        if (ok) return Decode(o.Type, buffer);
        if (Native.shim_state() != PRE_OP) State = LinkState.Lost;
        return null;
    }
    // only for enable logic
    public bool Write(DriveObject o, long value)
    {
        if (State != LinkState.PreOp) return false;
        var buffer = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, (int)value); 
        return Native.shim_sdo_write(o.Index, o.SubIndex, buffer, SizeOf(o.Type)) > 0;
    }
    public IReadOnlyList<NetworkAdapter> Adapters()
    {
        var buffer = new byte[16 * 1024];
        int used = Native.shim_list_adapters(buffer, buffer.Length);
        if (used <= 0) return [];
        return [.. Encoding.Latin1.GetString(buffer, 0, used)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('|', 2))
            .Select(parts => new NetworkAdapter(parts[0], parts.Length > 1 ? parts[1] : ""))];
    }

    static int SizeOf(DataType type) => type switch
    {
        DataType.I8 or DataType.U8 => 1,
        DataType.I16 or DataType.U16 => 2,
        _ => 4,
    };
    static long Decode(DataType type, byte[] b) => type switch
    {
        DataType.I8 => (sbyte)b[0],
        DataType.U8 => b[0],
        DataType.I16 => BinaryPrimitives.ReadInt16LittleEndian(b),
        DataType.U16 => BinaryPrimitives.ReadUInt16LittleEndian(b),
        DataType.I32 => BinaryPrimitives.ReadInt32LittleEndian(b),
        _ => BinaryPrimitives.ReadUInt32LittleEndian(b),
    };
    void TimeReads(long start)
    {
        _readMs += Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        if (++_reads < 1000) return;
        Console.WriteLine($"SDO read: {_readMs / _reads:0.00} ms average over {_reads} reads");
        _reads = 0;
        _readMs = 0;
    }

    static class Native
    {
        const string Dll = "ecat_shim";
        [DllImport(Dll)] public static extern void shim_close();
        [DllImport(Dll)] public static extern int shim_list_adapters(byte[] buffer, int length);
        [DllImport(Dll, CharSet = CharSet.Ansi, BestFitMapping = false)] public static extern int shim_open(string ifname);
        [DllImport(Dll)] public static extern void shim_identity(out uint vendor, out uint product, byte[] name, int length);
        [DllImport(Dll)] public static extern int shim_state();
        [DllImport(Dll)] public static extern int shim_sdo_read(ushort index, byte sub, byte[] buffer, ref int size);
        [DllImport(Dll)] public static extern int shim_sdo_write(ushort index, byte sub, byte[] buffer, int size);
    }
}