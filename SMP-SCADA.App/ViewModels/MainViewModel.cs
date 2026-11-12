using SMP_SCADA.Core.Alarms;
using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;
using SMP_SCADA.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Controls;
using static SMP_SCADA.Core.Service.Contract.Command;

namespace SMP_SCADA.ViewModels;

public enum PageId { Overview, Drive, Alarms, Engineer }
public sealed record MenuEntry(PageId Page, string Name, string Key, bool EngineerOnly, bool IsCurrent);
public sealed record AlarmRow(Priority Priority, bool Active, bool SymbolShown, string Time, string Text, string State)
{
    public int Number => (int)Priority;
}
public sealed class MainViewModel : ViewModelBase
{
    public static readonly TimeSpan Snapshot = TimeSpan.FromSeconds(1);
    public static readonly TimeSpan FlashPeriod = TimeSpan.FromSeconds(2);

    static readonly (PageId Page, string Name, string Key, bool EngineerOnly)[] Pages =
        [
        (PageId.Overview, "Overview", "F1", false),
        (PageId.Drive, "Drive", "F2", false),
        (PageId.Alarms, "Alarms", "F3", false),
        (PageId.Engineer, "Engineer", "F4", true),
        ];

    readonly ServiceConnection _service;
    readonly TimeProvider _time;
    Snapshot? _snapshot;
    PageId _page = PageId.Overview;
    bool _flashOn = true;

    public MainViewModel(ServiceConnection service, TimeProvider time)
    {
        _service = service;
        _time = time;
        Overview = new OverviewViewModel(service);

        Drive = new DriveViewModel();
        Events = new EventsViewModel(service, time);
        Engineer = new EngineerViewModel(service);
        
        ShowPage = new RelayCommand(p => { if (p is PageId page) Show(page); });
        Acknowledge = new RelayCommand(async _ => await service.SendAsync(id => new Acknowledge(id)), _ => Unacked > 0);
        Restart = new RelayCommand(_ =>
        {
            Process.Start(Environment.ProcessPath!);
            Environment.Exit(0);
        });
        Menu = BuildMenu();

        service.SnapshotReceived += s =>
        {
            _snapshot = s;
            Refresh();
        };
        service.StateChanged += Refresh;
    }

    public OverviewViewModel Overview { get; }
    public DriveViewModel Drive { get; }
    public EventsViewModel Events { get; }
    public EngineerViewModel Engineer { get; }
    public RelayCommand ShowPage { get; }
    public RelayCommand Acknowledge { get; }
    public RelayCommand Restart { get; }
    public IReadOnlyList<MenuEntry> Menu { get; private set; }
    public object CurrentView => _page switch
    {
        PageId.Drive => Drive,
        PageId.Alarms => Events,
        PageId.Engineer => Engineer,
        _ => Overview,
    };
    public void OnTimer()
    {
        _flashOn = !_flashOn;
        Drive.Update(_snapshot, Fresh);
        Refresh();
    }
    // Header
    public string BarText => _snapshot is { } s ? $"Ø {s.BarDiameterMm:0.0} mm" : "";
    public string LinkText
    {
        get
        {
            if (_snapshot is not { } s || !Connected) return "Unknown";
            string link = s.Link switch
            {
                LinkState.PreOp => "Connected",
                LinkState.Lost => "Lost",
                _ => "Disconnected",
            };
            return (s.Simulated ? "Simulator · " : "EtherCAT · ") + link;
        }
    }
    // Alarm banner 

    IReadOnlyList<AlarmStatus> ShownAlarms => _snapshot?.Alarms ?? [];

    int Unacked => ShownAlarms.Count(a => a.State is AlarmState.ActiveUnacked or AlarmState.ClearedUnacked);

    public string AlarmSummary => ShownAlarms.Count == 0 ? "None"
        : Unacked > 0 ? $"{Unacked} unacknowledged"
        : "All acknowledged";

    public IReadOnlyList<AlarmRow> AlarmRows { get; private set; } = [];

    void UpdateAlarmRows()
    {
        IReadOnlyList<AlarmRow> rows = [.. ShownAlarms.Select(a => new AlarmRow(
            a.Info.Priority,
            Active: a.State != AlarmState.ClearedUnacked,
            SymbolShown: _flashOn || a.State == AlarmState.ActiveAcked,
            a.Since.ToLocalTime().ToString("HH:mm:ss"),
            a.Info.Text,
            a.State switch
            {
                AlarmState.ActiveUnacked => "Active, unacknowledged",
                AlarmState.ActiveAcked => "Active, acknowledged",
                _ => "Cleared, unacknowledged",
            }))];
        if (!rows.SequenceEqual(AlarmRows)) AlarmRows = rows;
    }
    public string DataText => Fresh && _snapshot is { } s
        ? $"{s.PositionQuality} · {Math.Max(0, (_time.GetUtcNow() - s.Time).TotalSeconds):0.0} s ago"
        : "STALE";

    public string AccessText => Engineer.IsLocked ? "Operator" : "Engineer";
    public string DateText => _time.GetLocalNow().ToString("d MMM yyyy");
    public string TimeText => _time.GetLocalNow().ToString("HH:mm:ss");
    public bool IsSimulated => _snapshot?.Simulated == true;
    public string ServiceText => Connected ? "Connected" : _service.Problem ?? "Connecting";

    bool Connected => _service.Hello is not null;
    bool Fresh => Connected && _snapshot is { } s && _time.GetUtcNow() - s.Time < Snapshot;

    void Show(PageId page)
    {
        _page = page;
        Menu = BuildMenu();
        if (page == PageId.Alarms) Events.Refresh.Execute(null);
        if (page == PageId.Engineer) Engineer.LoadAdapters.Execute(null);
        Refresh();
    }

    IReadOnlyList<MenuEntry> BuildMenu() =>
        [.. Pages.Select(p => new MenuEntry(p.Page, p.Name, p.Key, p.EngineerOnly, p.Page == _page))];

    void Refresh()
    {
        Overview.Update(_snapshot, Fresh);
        Engineer.Update(_snapshot);
        UpdateAlarmRows();
        Acknowledge.RaiseCanExecuteChanged();
        OnPropertyChanged(string.Empty);
    }
}