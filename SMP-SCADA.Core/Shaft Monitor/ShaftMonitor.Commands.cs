using SMP_SCADA.Core.Alarms;
using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;
using static SMP_SCADA.Core.Service.Contract.Command;

namespace SMP_SCADA.Core;

public sealed partial class ShaftMonitor
{
    public ServiceMessage Handle(Command command, string user, Role role)
    {
        try
        {
            if (role < command.RequiredRole)
                return new Result(command.Id, false, $"Needs the {command.RequiredRole} role.");
            command.Validate();

            switch (command)
            {
                case SetZero: ZeroPosition(user); break;
                case SetBarDiameter c: ChangeBarDiameter(c.Mm, user); break;
                case Acknowledge: AcknowledgeAlarms(user); break;
                case Connect: ConnectDrive(user); break;
                case Disconnect: DisconnectDrive(user); break;
                case GetHistory c: return new HistoryData(c.Id, c.Key, _store.LoadHistory(c.Key, c.From, c.To));
                case GetEvents c: return new EventsData(c.Id, _store.LoadEvents(c.From, c.To));
                case GetAdapters c: return new AdaptersData(c.Id, _drive.Adapters());
                case RestoreEnableLogic: RewriteEnableLogic(user); break;
                case SetDeadband c: ChangeDeadband(c.Counts, user); break;
                case SetAdapter c: ChangeAdapter(c.Name, user); break;
                case StartRemeasure: BeginRemeasure(user); break;
                case FinishRemeasure c: CompleteRemeasure(c.Turns, c.CalibrationDiameterMm, user); break;
                case SaveRemeasure: AcceptRemeasure(user); break;
                case CancelRemeasure: AbandonRemeasure(user); break;
                default: throw new InvalidOperationException($"Unknown command {command.GetType().Name}.");
            }
            return new Result(command.Id, true);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            return new Result(command.Id, false, e.Message);
        }
    }

    void ZeroPosition(string user)
    {
        if (Latest.PositionQuality != Quality.Good)
            throw new InvalidOperationException("Can't zero without Good position reading.");
        _tracker.SetZero();
        _zeroSetAt = _time.GetUtcNow();
        _zeroSetBy = user;
        SavePosition();
        Log("zero", "Zero set", user);
    }

    void ChangeBarDiameter(double mm, string user)
    {
        double old = _settings.BarDiameterMm;
        SaveSettings(_settings with { BarDiameterMm = mm });
        Log("setting", $"Bar diameter {old:0.###} -> {mm:0.###} mm", user);
    }

    void AcknowledgeAlarms(string user)
    {
        foreach (var e in _alarms.AcknowledgeAll(user, _time.GetUtcNow())) LogAlarm(e);
    }

    void ConnectDrive(string user)
    {
        if (LinkUp) throw new InvalidOperationException("Already connected.");

        int slaves = _drive.Connect(_settings.Adapter);
        if (slaves != 1 || _drive.Identity is not { } found)
        {
            _drive.Disconnect();
            throw new InvalidOperationException($"Expected one drive, found {slaves}.");
        }

        if (_settings.PinnedDrive is not { } pinned)
        {
            SaveSettings(_settings with { PinnedDrive = found });
            Log("link", $"Drive recorded: {found.Name}, vendor {found.VendorId:X}h, product {found.ProductCode:X}h", user);
        }
        else if (pinned.VendorId != found.VendorId || pinned.ProductCode != found.ProductCode)
        {
            _drive.Disconnect();
            throw new InvalidOperationException(
                $"Unexpected drive {found.Name} (vendor {found.VendorId:X}h, product {found.ProductCode:X}h).");
        }

        _connectRequested = true; 
        _samples.Clear();
        _nextRead.Clear();
        Log("link", _tracker.State is null ? "Connected" : "Connected: check zero", user);
    }

    void DisconnectDrive(string user)
    {
        _connectRequested = false;
        _drive.Disconnect();
        SavePosition();
        Log("link", "Disconnected", user);

        if (_samples.TryGetValue(DriveObjects.EnableLogic, out var s) && ExpectedEnableLogic is long expected && s.Value != expected)
            Log("link", $"Warning: enable logic was {s.Value:X}h, not {expected:X}h. DIN5 won't enable the drive until it is restored.", user);
    }

    long? ExpectedEnableLogic =>
        _rules.FirstOrDefault(r => r.Source == DriveObjects.EnableLogic && r.Kind == RuleKind.NotEqual)?.Limit;
}