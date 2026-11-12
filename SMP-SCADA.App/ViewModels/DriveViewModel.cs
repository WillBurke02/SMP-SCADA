using SMP_SCADA.Core;
using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;
using SMP_SCADA.ViewModels;
using System.IO;

namespace SMP_SCADA.ViewModels;

public sealed class DriveRow(DriveObject o) : ViewModelBase
{
    public string Key => o.Key;
    public string Name => o.Name;
    public string Index => DriveText.Index(o);
    public string Unit => o.Unit;
    public string Value { get; private set; } = "—";
    public Quality Quality { get; private set; } = Quality.Bad;

    public void Update(Reading? reading, bool fresh)
    {
        if (reading is { } r)
        {
            Value = o.Unit is "bits" or "code" ? $"{r.Value:X4}h" : r.Value.ToString("#,##0");
            Quality = fresh ? r.Quality : Quality.Stale;
        }
        OnPropertyChanged(string.Empty);
    }
}

// Drive (F2) 
public sealed class DriveViewModel : ViewModelBase
{
    static readonly string WatchFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMP SCADA", "watched.txt");

    static readonly string[] DefaultWatched =
        [DriveObjects.PowerStageTemp.Key, DriveObjects.I2tMotor.Key, DriveObjects.I2tDrive.Key, DriveObjects.FollowingError.Key];

    static readonly HashSet<DriveObject> KeyObjects =
        [DriveObjects.Statusword, DriveObjects.EnableLogic, DriveObjects.ModeDisplay,
         DriveObjects.DigitalInputs, DriveObjects.DigitalOutputs, DriveObjects.ErrorRegister];

    readonly List<DriveRow> _rows = [.. DriveObjects.All.Where(o => !KeyObjects.Contains(o)).Select(o => new DriveRow(o))];
    readonly List<string> _watched;

    public DriveViewModel()
    {
        _watched = File.Exists(WatchFile) ? [.. File.ReadAllLines(WatchFile)] : [.. DefaultWatched];
        Watch = new RelayCommand(key => Change(key, add: true));
        Unwatch = new RelayCommand(key => Change(key, add: false));
        Split();
    }

    public RelayCommand Watch { get; }
    public RelayCommand Unwatch { get; }
    public IReadOnlyList<KeyRow> KeyRows { get; private set; } = [];
    public IReadOnlyList<DriveRow> Watched { get; private set; } = [];
    public IReadOnlyList<DriveRow> Others { get; private set; } = [];
    public bool IsStale { get; private set; } = true;

    // 1Hz
    public void Update(Snapshot? s, bool fresh)
    {
        IReadOnlyDictionary<string, Reading> values = s?.Values ?? new Dictionary<string, Reading>();
        foreach (var row in _rows) row.Update(values.TryGetValue(row.Key, out var r) ? r : null, fresh);
        KeyRows = DriveText.KeyRows(o => values.TryGetValue(o.Key, out var r) ? r.Value : null);
        IsStale = !fresh;
        OnPropertyChanged(nameof(KeyRows));
        OnPropertyChanged(nameof(IsStale));
    }

    void Change(object? key, bool add)
    {
        if (key is not string k) return;
        _watched.Remove(k);
        if (add) _watched.Add(k);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(WatchFile)!);
            File.WriteAllLines(WatchFile, _watched);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
        Split();
    }

    void Split()
    {
        Watched = [.. _watched.Select(k => _rows.Find(r => r.Key == k)).OfType<DriveRow>()];
        Others = [.. _rows.Where(r => !_watched.Contains(r.Key))];
        OnPropertyChanged(nameof(Watched));
        OnPropertyChanged(nameof(Others));
    }
}