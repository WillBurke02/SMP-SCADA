using SMP_SCADA.Core;
using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;
using SMP_SCADA.ViewModels;
using System.Globalization;
using static SMP_SCADA.Core.Service.Contract.Command;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SMP_SCADA.ViewModels;

public sealed record Link(string Name, string Target);
// Engineer (F4)
public sealed class EngineerViewModel : ViewModelBase
{
    readonly ServiceConnection _service;
    readonly List<RelayCommand> _commands = [];
    Snapshot? _s;
    bool _safeToStop;
    string? _error;

    public EngineerViewModel(ServiceConnection service)
    {
        UseAdapter = Send(id => new SetAdapter(id, AdapterInput ?? ""), () => AdapterInput is not null);
        LoadAdapters = new RelayCommand(async _ =>
        {
            if (await _service.SendAsync(id => new GetAdapters(id)) is AdaptersData data) NetworkAdapters = data.Adapters;
            OnPropertyChanged(nameof(NetworkAdapters));
        });
        _service = service;
        ConnectDrive = Send(id => new Connect(id, SafeToStop), () => SafeToStop);
        DisconnectDrive = Send(id => new Disconnect(id));
        RestoreEnable = Send(id => new RestoreEnableLogic(id), () => SafeToStop);
        ApplyDeadband = Send(id => new SetDeadband(id, ReadInt(DeadbandInput)));
        Start = Send(id => new StartRemeasure(id), () => Remeasure is null);
        Finish = Send(id => new FinishRemeasure(id, ReadInt(TurnsInput), ReadDouble(DiameterInput)), () => Remeasure is not null);
        Save = Send(id => new SaveRemeasure(id), () => Remeasure?.Proposed is not null);
        Cancel = Send(id => new CancelRemeasure(id), () => Remeasure is not null);
        Links = ReadLinks(LinksFile);
        OpenLink = new RelayCommand(target => Open(target as string));
    }
    public RelayCommand LoadAdapters { get; }
    public RelayCommand UseAdapter { get; }
    public RelayCommand ConnectDrive { get; }
    public RelayCommand DisconnectDrive { get; }
    public RelayCommand RestoreEnable { get; }
    public RelayCommand ApplyDeadband { get; }
    public RelayCommand Start { get; }
    public RelayCommand Finish { get; }
    public RelayCommand Save { get; }
    public RelayCommand Cancel { get; }
    const string PinSha256 = "03AC674216F3E15C761EE1A5E255F067953623C8B388B4459E13F978D7C846F4";

    public bool IsLocked { get; private set; } = true;
    public bool IsUnlocked => !IsLocked;
    public string? PinError { get; private set; }

    public void Unlock(string pin)
    {
        IsLocked = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pin))) != PinSha256;
        PinError = IsLocked ? "Wrong PIN" : null;
        OnPropertyChanged(string.Empty);
    }

    public void Lock()
    {
        IsLocked = true;
        PinError = null;
        SafeToStop = false;
        OnPropertyChanged(string.Empty);
    }
    public void Update(Snapshot? snapshot)
    {
        _s = snapshot;
        OnPropertyChanged(string.Empty);
        foreach (var command in _commands) command.RaiseCanExecuteChanged();
    }

    public string? Error
    {
        get => _error;
        private set => Set(ref _error, value);
    }

    //   Drive link  
    public IReadOnlyList<NetworkAdapter> NetworkAdapters { get; private set; } = [];
    public string? AdapterInput { get; set; }
    public bool SafeToStop
    {
        get => _safeToStop;
        set
        {
            Set(ref _safeToStop, value);
            foreach (var command in _commands) command.RaiseCanExecuteChanged();
        }
    }

    public string EnableLogicText => _s is { } s && s.Values.TryGetValue(DriveObjects.EnableLogic.Key, out var r)
        ? DriveText.EnableSource(r.Value) : "—";

    // Deadband  

    public string DeadbandInput { get; set; } = "";
    public string DeadbandText => _s is { } s ? $"{s.DeadbandCounts} counts" : "—";

    // Re-measure  

    RemeasureStatus? Remeasure => _s?.Remeasure;

    public string TurnsInput { get; set; } = "";
    public string DiameterInput { get; set; } = "";

    public string CalibrationText => _s is { } s ? Describe(s.Calibration) : "—";
    public string CountsMovedText => Remeasure is { } r ? r.CountsMoved.ToString("#,##0") : "—";
    public string ProposedText => Remeasure?.Proposed is { } p ? Describe(p) : "—";

    static string Describe(Calibration c) => $"{c.CountsPerRev:#,##0} counts per rev at {c.DiameterMm:0.0} mm";

    static readonly string LinksFile = Path.Combine(AppContext.BaseDirectory, "links.txt");

    public IReadOnlyList<Link> Links { get; }
    public RelayCommand OpenLink { get; }

    static IReadOnlyList<Link> ReadLinks(string file) => !File.Exists(file) ? [] :
        [.. File.ReadAllLines(file).Select(line => line.Split('|', 2)).Where(parts => parts.Length == 2)
                                    .Select(parts => new Link(parts[0].Trim(), 
                                        Path.Combine(AppContext.BaseDirectory, parts[1].Trim())))];

    void Open(string? target)
    {
        if (target is null) return;
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            Error = null;
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException)
        {
            Error = $"{target}: {e.Message}";
        }
    }
    // SEnding

    RelayCommand Send(Func<int, Command> make, Func<bool>? canRun = null)
    {
        var command = new RelayCommand(async _ =>
        {
            var reply = await _service.SendAsync(make);
            Error = reply is Result { Ok: false } refused ? refused.Err : null;
            SafeToStop = false;
        }, _ => canRun?.Invoke() ?? true);
        _commands.Add(command);
        return command;
    }

     static int ReadInt(string text) => int.TryParse(text, out int n) ? n : -1;

    static double ReadDouble(string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double d) && double.IsFinite(d) ? d : 0;
}