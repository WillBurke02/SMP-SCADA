using SMP_SCADA.Core.Alarms;
using SMP_SCADA.Core.Service.Contract;
using SMP_SCADA.ViewModels;
using static SMP_SCADA.Core.Service.Contract.Command;

namespace SMP_SCADA.ViewModels;

public sealed record AlarmLogRow(string Time, Priority Priority, bool Active, string Alarm, string Change)
{
    public int Number => (int)Priority;
}

public sealed record InfoRow(string Time, string Kind, string Text);

// Alarms and events (F3) 
public sealed class EventsViewModel : ViewModelBase
{
    static readonly (string Name, TimeSpan Span)[] PeriodOptions =
    [
        ("Last hour", TimeSpan.FromHours(1)),
        ("Last 24 hours", TimeSpan.FromDays(1)),
        ("Last 7 days", TimeSpan.FromDays(7)),
        ("Last 31 days", TimeSpan.FromDays(31)), 
    ];

    readonly ServiceConnection _service;
    readonly TimeProvider _time;
    IReadOnlyList<LoggedEvent> _events = [];
    string _period = "Last 24 hours";
    string _priority = "All";
    string _kind = "All";

    public EventsViewModel(ServiceConnection service, TimeProvider time)
    {
        _service = service;
        _time = time;
        Refresh = new RelayCommand(async _ => await LoadAsync());
    }

    public RelayCommand Refresh { get; }
    public IReadOnlyList<string> Periods { get; } = [.. PeriodOptions.Select(p => p.Name)];
    public IReadOnlyList<string> Priorities { get; } = ["All", "High", "Medium", "Low"];
    public IReadOnlyList<string> Kinds { get; } = ["All", "Link", "Zero", "Setting", "Calibration", "Service"];
    public IReadOnlyList<AlarmLogRow> AlarmRows { get; private set; } = [];
    public IReadOnlyList<InfoRow> InfoRows { get; private set; } = [];
    public string? Error { get; private set; }

    public string Period
    {
        get => _period;
        set { _period = value; Refresh.Execute(null); }
    }

    public string PriorityFilter
    {
        get => _priority;
        set { _priority = value; Filter(); }
    }

    public string KindFilter
    {
        get => _kind;
        set { _kind = value; Filter(); }
    }

    async Task LoadAsync()
    {
        var now = _time.GetUtcNow();
        var span = PeriodOptions.First(p => p.Name == _period).Span;
        var reply = await _service.SendAsync(id => new GetEvents(id, now - span, now));
        if (reply is EventsData data)
        {
            _events = [.. data.Events.OrderByDescending(e => e.Time)];
            Error = null;
        }
        else
        {
            Error = (reply as Result)?.Err;
        }
        Filter();
    }

    void Filter()
    {
        AlarmRows = [.. _events.Where(e => e.Kind == "alarm").Select(ToAlarmRow)
                               .Where(r => _priority == "All" || r.Priority.ToString() == _priority)];
        InfoRows = [.. _events.Where(e => e.Kind != "alarm" && (_kind == "All" || e.Kind.Equals(_kind, StringComparison.OrdinalIgnoreCase)))
                              .Select(e => new InfoRow(Time(e), char.ToUpper(e.Kind[0]) + e.Kind[1..], e.Text))];
        OnPropertyChanged(string.Empty);
    }

    static AlarmLogRow ToAlarmRow(LoggedEvent e)
    {
        int space = e.Text.IndexOf(' '), colon = e.Text.IndexOf(": ");
        if (space < 0 || colon < space) return new(Time(e), Priority.Low, false, e.Text, "");

        string code = e.Text[..space];
        Enum.TryParse(e.Text[(space + 1)..colon], out AlarmState state);
        string change = state switch
        {
            AlarmState.ActiveUnacked => "Raised",
            AlarmState.ActiveAcked => "Acknowledged",
            AlarmState.ClearedUnacked => "Cleared",
            _ => e.User is null ? "Cleared" : "Acknowledged",
        };
        bool active = state is AlarmState.ActiveUnacked or AlarmState.ActiveAcked;
        return new(Time(e), PriorityOf(code), active, e.Text[(colon + 2)..], change);
    }

    static Priority PriorityOf(string code) =>
        code == CommsAlarm.Comms.Code ? CommsAlarm.Comms.Priority
        : Alarms.All.FirstOrDefault(r => r.Code == code)?.Priority ?? Priority.Low;

    static string Time(LoggedEvent e) => e.Time.ToLocalTime().ToString("d MMM HH:mm:ss");
}