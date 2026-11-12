namespace SMP_SCADA.Core.Alarms;

public enum Priority { Low = 3, Medium = 2, High = 1}

// Latched alarm states until clear and ack
public enum AlarmState { Normal, ActiveUnacked, ActiveAcked, ClearedUnacked }

// What the engine needs to know about one alarm
// OnDelay: the condition must hold this long before the alarm is raised
public sealed record AlarmInfo(string Code, string Text, Priority Priority, TimeSpan OnDelay);

// State of each alarm
public readonly record struct AlarmStatus(AlarmInfo Info, AlarmState State, DateTimeOffset Since);

// State change
public readonly record struct AlarmEvent(DateTimeOffset Time, string Code, string Text, AlarmState State, string? User);

public sealed class AlarmEngine
{
    readonly Dictionary<string, AlarmStatus> _status = new();
    readonly Dictionary<string, DateTimeOffset> _trueSince = new();

    public AlarmState StateOf(string code) =>
        _status.TryGetValue(code, out var s) ? s.State : AlarmState.Normal;

    public bool IsActive(string code) => StateOf(code) is AlarmState.ActiveUnacked or AlarmState.ActiveAcked;

    // severest first, then newest
    public IEnumerable<AlarmStatus> Showing =>
        _status.Values.Where(s => s.State != AlarmState.Normal)
                      .OrderByDescending(s => s.Info.Priority)
                      .ThenByDescending(s => s.Since);

    // Checks whether the condition is correct
    public List<AlarmEvent> Evaluate(IEnumerable<(AlarmInfo Info, bool? Condition)> alarms, DateTimeOffset now)
    {
        var events = new List<AlarmEvent>();
        foreach (var (info, condition) in alarms)
        {
            if (condition is not bool met) continue;

            bool active = HeldLongEnough(info, met, now);
            var old = StateOf(info.Code);
            var next = (old, active) switch
            {
                (AlarmState.Normal, true) or (AlarmState.ClearedUnacked, true) => AlarmState.ActiveUnacked,
                (AlarmState.ActiveUnacked, false) => AlarmState.ClearedUnacked,
                (AlarmState.ActiveAcked, false) => AlarmState.Normal,
                _ => old,
            };
            if (next != old) Set(info, next, now, null, events);
        }
        return events;
    }

    // Ack All btn
    public List<AlarmEvent> AcknowledgeAll(string user, DateTimeOffset now)
    {
        var events = new List<AlarmEvent>();
        foreach (var s in _status.Values.ToList())
        {
            var next = s.State switch
            {
                AlarmState.ActiveUnacked => AlarmState.ActiveAcked,
                AlarmState.ClearedUnacked => AlarmState.Normal,
                var same => same,
            };
            if (next != s.State) Set(s.Info, next, now, user, events);
        }
        return events;
    }

    bool HeldLongEnough(AlarmInfo info, bool condition, DateTimeOffset now)
    {
        if (!condition)
        {
            _trueSince.Remove(info.Code);
            return false;
        }
        if (!_trueSince.TryGetValue(info.Code, out var since)) _trueSince[info.Code] = since = now;
        return now - since >= info.OnDelay;
    }

    void Set(AlarmInfo info, AlarmState next, DateTimeOffset now, string? user, List<AlarmEvent> events)
    {
        _status[info.Code] = new AlarmStatus(info, next, now);
        events.Add(new AlarmEvent(now, info.Code, info.Text, next, user));
    }
}