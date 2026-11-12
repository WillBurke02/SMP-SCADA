using SMP_SCADA.Core.Alarms;
using SMP_SCADA.Core.Service.Contract;

namespace SMP_SCADA.Core.Tests;

// IStore in memory, so monitor tests run without a database. Fields are public for asserts.
public sealed class MemoryStore : IStore
{
    public Settings? SavedSettings;
    public SavedPosition? SavedPosition;
    public readonly Dictionary<string, AlarmRule> Rules = new();
    public readonly List<LoggedEvent> Events = new();
    public readonly List<(string Key, HistoryPoint Point)> History = new();

    public Settings? LoadSettings() => SavedSettings;
    public void SaveSettings(Settings settings) => SavedSettings = settings;
    public SavedPosition? LoadPosition() => SavedPosition;
    public void SavePosition(SavedPosition position) => SavedPosition = position;

    public IReadOnlyList<AlarmRule> LoadAlarmRules() => Rules.Values.ToList();
    public void SaveAlarmRule(AlarmRule rule) => Rules[rule.Code] = rule;
    public void DeleteAlarmRule(string code) => Rules.Remove(code);

    public void AppendEvent(LoggedEvent e) => Events.Add(e);
    public IReadOnlyList<LoggedEvent> LoadEvents(DateTimeOffset from, DateTimeOffset to) =>
        Events.Where(e => e.Time >= from && e.Time < to).ToList();

    public void AppendHistory(IReadOnlyList<(string Key, HistoryPoint Point)> points) => History.AddRange(points);
    public IReadOnlyList<HistoryPoint> LoadHistory(string key, DateTimeOffset from, DateTimeOffset to) =>
        History.Where(h => h.Key == key && h.Point.Time >= from && h.Point.Time < to).Select(h => h.Point).ToList();
}