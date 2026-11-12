using Microsoft.Data.Sqlite;
using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;
using SMP_SCADA.Service;

namespace SMP_SCADA.Core.Tests;

// Runs against a real SQLite file in the temp folder.
// Needs the test project to reference the Service project.
public sealed class SqliteStoreTests : IDisposable
{
    static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch.AddDays(20_000);   // whole milliseconds
    readonly string _path = Path.Combine(Path.GetTempPath(), $"smp-scada-test-{Guid.NewGuid():N}.db");
    readonly SqliteStore _store;

    public SqliteStoreTests() => _store = new SqliteStore(_path);

    public void Dispose()
    {
        _store.Dispose();
        SqliteConnection.ClearAllPools();   // pooled connections would keep the file locked
        foreach (var file in new[] { _path, _path + "-wal", _path + "-shm" }) File.Delete(file);
    }

    [Fact]
    public void An_empty_database_means_first_start()
    {
        Assert.Null(_store.LoadSettings());
        Assert.Null(_store.LoadPosition());
        Assert.Empty(_store.LoadAlarmRules());
    }

    [Fact]
    public void Settings_position_and_rules_survive_a_restart()
    {
        var settings = Settings.Default with { BarDiameterMm = 350, PinnedDrive = new DriveIdentity(0xE4, 0x8212, "BL 4320-C") };
        var position = new SavedPosition(new TrackerState(-5, 123_456_789_012, 1000, 77), T0, "will");
        _store.SaveSettings(settings);
        _store.SavePosition(position);
        foreach (var rule in Alarms.Alarms.All) _store.SaveAlarmRule(rule);
        _store.DeleteAlarmRule("DRIVE_WARN");

        using var reopened = new SqliteStore(_path);

        Assert.Equal(settings, reopened.LoadSettings());
        Assert.Equal(position, reopened.LoadPosition());
        Assert.Equal(Alarms.Alarms.All.Where(r => r.Code != "DRIVE_WARN").OrderBy(r => r.Code),
                     reopened.LoadAlarmRules());
    }

    [Fact]
    public void Events_come_back_in_order_with_missing_users_kept_as_null()
    {
        var started = new LoggedEvent(T0, "service", "Started", null);
        var zeroed = new LoggedEvent(T0.AddSeconds(5), "zero", "Zero set", "will");
        _store.AppendEvent(zeroed);
        _store.AppendEvent(started);

        Assert.Equal(new[] { started, zeroed }, _store.LoadEvents(T0, T0.AddMinutes(1)));
        Assert.Empty(_store.LoadEvents(T0.AddMinutes(1), T0.AddMinutes(2)));
    }

    [Fact]
    public void Long_history_spans_come_back_averaged_without_hiding_bad_data()
    {
        var points = Enumerable.Range(0, 10_000)
            .Select(i => (DerivedValues.Distance, new HistoryPoint(T0.AddSeconds(i), i, i == 5000 ? Quality.Bad : Quality.Good)))
            .ToList();
        _store.AppendHistory(points);

        var shortSpan = _store.LoadHistory(DerivedValues.Distance, T0, T0.AddSeconds(10));
        var longSpan = _store.LoadHistory(DerivedValues.Distance, T0, T0.AddSeconds(10_000));

        Assert.Equal(10, shortSpan.Count);                                  // raw seconds
        Assert.Equal(SqliteStore.MaxHistoryPoints, longSpan.Count);         // 5 s averages
        Assert.Contains(longSpan, p => p.Quality == Quality.Bad);           // the one bad second still shows
    }

    [Fact]
    public void History_older_than_the_keep_period_is_deleted()
    {
        _store.AppendHistory([(DerivedValues.Angle, new HistoryPoint(T0, 1, Quality.Good))]);
        var later = T0 + SqliteStore.KeepHistory + TimeSpan.FromDays(1);
        _store.AppendHistory([(DerivedValues.Angle, new HistoryPoint(later, 2, Quality.Good))]);

        var all = _store.LoadHistory(DerivedValues.Angle, T0, later.AddSeconds(1));

        Assert.Equal(2, Assert.Single(all).Value);
    }
}