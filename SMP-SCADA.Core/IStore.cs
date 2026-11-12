using SMP_SCADA.Core.Alarms;
using SMP_SCADA.Core.Service.Contract;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMP_SCADA.Core;

public interface IStore
{
    Settings? LoadSettings();
    void SaveSettings(Settings settings);

    SavedPosition? LoadPosition();
    void SavePosition(SavedPosition position);

    IReadOnlyList<AlarmRule> LoadAlarmRules();
    void SaveAlarmRule(AlarmRule rule);
    void DeleteAlarmRule(string code);

    void AppendEvent(LoggedEvent e);
    IReadOnlyList<LoggedEvent> LoadEvents(DateTimeOffset from, DateTimeOffset to);

    void AppendHistory(IReadOnlyList<(string Key, HistoryPoint Point)> points);
    IReadOnlyList<HistoryPoint> LoadHistory(string key, DateTimeOffset from, DateTimeOffset to);
}