using SMP_SCADA.Core.Alarms;
using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;

namespace SMP_SCADA.Core;

public sealed partial class ShaftMonitor
{
    public static readonly TimeSpan HistoryEvery = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan SavePositionEvery = TimeSpan.FromSeconds(10);

    readonly IDriveLink _drive;
    readonly IStore _store;
    readonly TimeProvider _time;
    readonly bool _simulated;
    readonly AlarmEngine _alarms = new();
    readonly Dictionary<DriveObject, Sample> _samples = new();
    readonly Dictionary<DriveObject, DateTimeOffset> _nextRead = new();
    readonly Dictionary<string, HistoryPoint> _lastWritten = new();
    readonly List<AlarmRule> _rules;
    readonly PositionTracker _tracker;
    Settings _settings;
    DateTimeOffset? _zeroSetAt;
    string? _zeroSetBy;
    bool _connectRequested;
    DateTimeOffset _nextHistory, _nextSave;

    public ShaftMonitor(IDriveLink drive, IStore store, TimeProvider time, bool simulated)
    {
        _drive = drive;
        _store = store;
        _time = time;
        _simulated = simulated;

        var settings = store.LoadSettings();
        if (settings is null)   // first ever start
        {
            settings = Settings.Default;
            store.SaveSettings(settings);
            foreach (var rule in Alarms.Alarms.All) store.SaveAlarmRule(rule);
        }
        _settings = settings;
        var saved = store.LoadPosition();
        _tracker = new PositionTracker(saved?.Tracker) { DeadbandCounts = _settings.DeadbandCount };
        _zeroSetAt = saved?.ZeroSetAt;
        _zeroSetBy = saved?.ZeroSetBy;

        Log("service", simulated ? "Started (simulated)" : "Started", null);
        Latest = BuildSnapshot(time.GetUtcNow(), []);

    }
    public void Stop()
    {
        SavePosition();
        Log("service", "Stopped", null);
    }

    public Snapshot Latest { get; private set; }

    bool LinkUp => _drive.State == LinkState.PreOp;

    public void Tick()
    {
        var now = _time.GetUtcNow();
        if (LinkUp) ReadDueObjects();

        var readings = CurrentReadings(now);
        bool commsLost = CommsAlarm.IsActive(_connectRequested, _drive.State, QualityOf(readings, DriveObjects.PositionActual));
        var rules = Alarms.Alarms.All.Select(a => (a.Info, a.IsActive(readings, _alarms.IsActive(a.Code))));
        foreach (var e in _alarms.Evaluate([(CommsAlarm.Comms, commsLost), .. rules], now)) LogAlarm(e);

        Latest = BuildSnapshot(now, readings);

        if (now >= _nextHistory)
        {
            RecordHistory(Latest);
            _nextHistory = now + HistoryEvery;
        }
        if (now >= _nextSave)
        {
            SavePosition();
            _nextSave = now + SavePositionEvery;
        }
    }

    void ReadDueObjects()
    {
        foreach (var o in DriveObjects.All)
        {
            if (_nextRead.GetValueOrDefault(o) > _time.GetUtcNow()) continue;   // not due yet

            long? value = _drive.Read(o);
            var at = _time.GetUtcNow(); 
            _nextRead[o] = o.Period == DriveObjects.OnceAtConnect ? DateTimeOffset.MaxValue : at + o.Period;

            if (value is long v)
            {
                _samples[o] = new Sample(v, at);
                if (o == DriveObjects.PositionActual) _tracker.Update((int)v, at);
            }
            else if (_samples.TryGetValue(o, out var old))
            {
                _samples[o] = old with { LastReadFailed = true };
            }
        }
    }

    Dictionary<DriveObject, Reading> CurrentReadings(DateTimeOffset now) =>
        _samples.ToDictionary(kv => kv.Key, kv => new Reading(kv.Value.Value, kv.Value.QualityAt(kv.Key, now, LinkUp)));

    static Quality QualityOf(Dictionary<DriveObject, Reading> readings, DriveObject o) =>
        readings.TryGetValue(o, out var r) ? r.Quality : Quality.Bad;

    Snapshot BuildSnapshot(DateTimeOffset now, Dictionary<DriveObject, Reading> readings)
    {
        var cal = _settings.Calibration;
        long fromZero = _tracker.CountsFromZero;
        return new Snapshot(now, _simulated, _drive.State, _connectRequested, _drive.Identity,
            QualityOf(readings, DriveObjects.PositionActual),
            SurfaceSpeedMmS: _tracker.SpeedCountsPerSecond / cal.CountsPerMm,
            AngleDeg: ShaftMaths.AngleDeg(fromZero, _settings.BarDiameterMm, cal),
            DistanceMm: ShaftMaths.DistanceMm(fromZero, cal),
            TravelMm: ShaftMaths.DistanceMm(_tracker.TravelCounts, cal),
            _settings.BarDiameterMm, cal, _settings.DeadbandCount, _zeroSetAt, _zeroSetBy,
            readings.ToDictionary(kv => kv.Key.Key, kv => kv.Value),
            _alarms.Showing.ToList(),
            RemeasureNow()); 
    }

    void RecordHistory(Snapshot s)
    {
        var points = new List<(string Key, HistoryPoint Point)>();

        void Add(string key, double value, Quality quality)
        {
            if (_lastWritten.TryGetValue(key, out var last) && last.Value == value && last.Quality == quality) return;
            var point = new HistoryPoint(s.Time, value, quality);
            _lastWritten[key] = point;
            points.Add((key, point));
        }

        foreach (var (key, reading) in s.Values) Add(key, reading.Value, reading.Quality);
        if (s.SurfaceSpeedMmS is double speed) Add(DerivedValues.SurfaceSpeed, speed, s.PositionQuality);
        Add(DerivedValues.Angle, s.AngleDeg, s.PositionQuality);
        Add(DerivedValues.Distance, s.DistanceMm, s.PositionQuality);
        Add(DerivedValues.Travel, s.TravelMm, s.PositionQuality);

        if (points.Count > 0) _store.AppendHistory(points);
    }

    void SavePosition()
    {
        if (_tracker.State is { } state) _store.SavePosition(new SavedPosition(state, _zeroSetAt, _zeroSetBy));
    }

    void SaveSettings(Settings settings)
    {
        _settings = settings;
        _store.SaveSettings(settings);
    }

    void Log(string kind, string text, string? user) =>
        _store.AppendEvent(new LoggedEvent(_time.GetUtcNow(), kind, text, user));

    void LogAlarm(AlarmEvent e) =>
        _store.AppendEvent(new LoggedEvent(e.Time, "alarm", $"{e.Code} {e.State}: {e.Text}", e.User));
}