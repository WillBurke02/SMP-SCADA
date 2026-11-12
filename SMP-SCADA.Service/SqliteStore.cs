using Microsoft.Data.Sqlite;
using SMP_SCADA.Core;
using SMP_SCADA.Core.Alarms;
using SMP_SCADA.Core.Service.Contract;
using System.Text.Json;

namespace SMP_SCADA.Service;

// The real IStore: one SQLite file. Only the worker loop uses it, so one connection is enough.
public sealed class SqliteStore : IStore, IDisposable
{
    public static readonly TimeSpan KeepHistory = TimeSpan.FromDays(90);   // ponytail: fixed until someone needs it changeable
    public const int MaxHistoryPoints = 2000;                              // per request; longer spans are averaged

    readonly SqliteConnection _db;
    DateTimeOffset _lastPrune = DateTimeOffset.MinValue;

    public SqliteStore(string path)
    {
        _db = new SqliteConnection($"Data Source={path}");
        _db.Open();
        Run("""
            PRAGMA journal_mode = WAL;
            CREATE TABLE IF NOT EXISTS kv (name TEXT PRIMARY KEY, json TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS alarm_rules (code TEXT PRIMARY KEY, json TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS events (time INTEGER NOT NULL, kind TEXT NOT NULL, text TEXT NOT NULL, user TEXT);
            CREATE INDEX IF NOT EXISTS events_time ON events (time);
            CREATE TABLE IF NOT EXISTS history (key TEXT NOT NULL, time INTEGER NOT NULL, value REAL NOT NULL,
                                                quality INTEGER NOT NULL, PRIMARY KEY (key, time)) WITHOUT ROWID;
            """);
    }

    public void Dispose() => _db.Dispose();

    // ---- Settings and position: one JSON row each ----

    public Settings? LoadSettings() => LoadJson<Settings>("settings");
    public void SaveSettings(Settings settings) => SaveJson("settings", settings);
    public SavedPosition? LoadPosition() => LoadJson<SavedPosition>("position");
    public void SavePosition(SavedPosition position) => SaveJson("position", position);

    // ---- Alarm rules ----

    public IReadOnlyList<AlarmRule> LoadAlarmRules()
    {
        var rules = new List<AlarmRule>();
        using var command = Sql("SELECT json FROM alarm_rules ORDER BY code");
        using var reader = command.ExecuteReader();
        while (reader.Read()) rules.Add(FromJson<AlarmRule>(reader.GetString(0)));
        return rules;
    }

    public void SaveAlarmRule(AlarmRule rule) =>
        Run("INSERT OR REPLACE INTO alarm_rules (code, json) VALUES ($code, $json)",
            ("$code", rule.Code), ("$json", ToJson(rule)));

    public void DeleteAlarmRule(string code) =>
        Run("DELETE FROM alarm_rules WHERE code = $code", ("$code", code));

    // ---- Event log ----

    public void AppendEvent(LoggedEvent e) =>
        Run("INSERT INTO events (time, kind, text, user) VALUES ($time, $kind, $text, $user)",
            ("$time", e.Time.ToUnixTimeMilliseconds()), ("$kind", e.Kind), ("$text", e.Text), ("$user", e.User));

    public IReadOnlyList<LoggedEvent> LoadEvents(DateTimeOffset from, DateTimeOffset to)
    {
        var events = new List<LoggedEvent>();
        using var command = Sql("SELECT time, kind, text, user FROM events WHERE time >= $from AND time < $to ORDER BY time",
            ("$from", from.ToUnixTimeMilliseconds()), ("$to", to.ToUnixTimeMilliseconds()));
        using var reader = command.ExecuteReader();
        while (reader.Read())
            events.Add(new LoggedEvent(Time(reader.GetInt64(0)), reader.GetString(1), reader.GetString(2),
                                       reader.IsDBNull(3) ? null : reader.GetString(3)));
        return events;
    }

    // ---- History ----

    public void AppendHistory(IReadOnlyList<(string Key, HistoryPoint Point)> points)
    {
        if (points.Count == 0) return;

        using var transaction = _db.BeginTransaction();
        using var command = Sql("INSERT OR REPLACE INTO history (key, time, value, quality) VALUES ($key, $time, $value, $quality)",
            ("$key", ""), ("$time", 0L), ("$value", 0.0), ("$quality", 0));
        command.Transaction = transaction;
        foreach (var (key, point) in points)
        {
            command.Parameters["$key"].Value = key;
            command.Parameters["$time"].Value = point.Time.ToUnixTimeMilliseconds();
            command.Parameters["$value"].Value = point.Value;
            command.Parameters["$quality"].Value = (int)point.Quality;
            command.ExecuteNonQuery();
        }
        transaction.Commit();

        PruneHourly(points[points.Count - 1].Point.Time);
    }

    // At most MaxHistoryPoints come back. Longer spans are averaged into equal time buckets, and each
    // bucket takes its worst quality (Good < Stale < Bad), so a gap is never hidden.
    // ponytail: averages hide short spikes; send min and max per bucket if the trends need them.
    public IReadOnlyList<HistoryPoint> LoadHistory(string key, DateTimeOffset from, DateTimeOffset to)
    {
        long fromMs = from.ToUnixTimeMilliseconds(), toMs = to.ToUnixTimeMilliseconds();
        long bucketMs = Math.Max(1000, (toMs - fromMs) / MaxHistoryPoints);

        var points = new List<HistoryPoint>();
        using var command = Sql("""
            SELECT MIN(time), AVG(value), MAX(quality) FROM history
            WHERE key = $key AND time >= $from AND time < $to
            GROUP BY (time - $from) / $bucket ORDER BY 1
            """, ("$key", key), ("$from", fromMs), ("$to", toMs), ("$bucket", bucketMs));
        using var reader = command.ExecuteReader();
        while (reader.Read())
            points.Add(new HistoryPoint(Time(reader.GetInt64(0)), reader.GetDouble(1), (Quality)reader.GetInt32(2)));
        return points;
    }

    void PruneHourly(DateTimeOffset now)
    {
        if (now - _lastPrune < TimeSpan.FromHours(1)) return;
        _lastPrune = now;
        Run("DELETE FROM history WHERE time < $cut", ("$cut", (now - KeepHistory).ToUnixTimeMilliseconds()));
    }

    // ---- Helpers ----

    T? LoadJson<T>(string name) where T : class
    {
        using var command = Sql("SELECT json FROM kv WHERE name = $name", ("$name", name));
        return command.ExecuteScalar() is string json ? FromJson<T>(json) : null;
    }

    void SaveJson<T>(string name, T value) =>
        Run("INSERT OR REPLACE INTO kv (name, json) VALUES ($name, $json)", ("$name", name), ("$json", ToJson(value)));

    // Same JSON settings as the pipe, so a missing field fails loudly here too.
    // So: a field added to Settings or SavedPosition later needs a default value, or old databases won't load.
    static string ToJson<T>(T value) => JsonSerializer.Serialize(value, Protocol.Options);
    static T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, Protocol.Options)
                                         ?? throw new JsonException($"Empty {typeof(T).Name} in the database.");

    static DateTimeOffset Time(long unixMs) => DateTimeOffset.FromUnixTimeMilliseconds(unixMs);

    // Parameters only, never values pasted into SQL text.
    SqliteCommand Sql(string sql, params (string Name, object? Value)[] parameters)
    {
        var command = _db.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return command;
    }

    void Run(string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = Sql(sql, parameters);
        command.ExecuteNonQuery();
    }
}