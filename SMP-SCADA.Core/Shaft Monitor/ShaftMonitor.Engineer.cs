using SMP_SCADA.Core.Alarms;
using SMP_SCADA.Core.Drive;
using SMP_SCADA.Core.Service.Contract;

namespace SMP_SCADA.Core;

public sealed partial class ShaftMonitor
{
    (DateTimeOffset StartedAt, long StartCounts)? _remeasureStart;
    Calibration? _proposed;

    // Puts enable logic back to what the rule expects (0 = DIN5 only) and reads it back.
    void RewriteEnableLogic(string user)
    {
        if (!LinkUp) throw new InvalidOperationException("Not connected.");
        if (ExpectedEnableLogic is not long expected)
            throw new InvalidOperationException("There is no ENABLE_LOGIC rule to say what to restore.");

        if (!_drive.Write(DriveObjects.EnableLogic, expected) || _drive.Read(DriveObjects.EnableLogic) != expected)
        {
            Log("link", $"Enable logic restore to {expected:X}h failed", user);
            throw new InvalidOperationException("The drive did not take the new enable logic.");
        }
        _samples[DriveObjects.EnableLogic] = new Sample(expected, _time.GetUtcNow());
        Log("link", $"Enable logic restored to {expected:X}h", user);
    }

    void ChangeDeadband(int counts, string user)
    {
        int old = _settings.DeadbandCount;
        SaveSettings(_settings with { DeadbandCount = counts });
        _tracker.DeadbandCounts = counts;
        Log("setting", $"Deadband {old} -> {counts} counts", user);
    }

    void ChangeAdapter(string name, string user)
    {
        if (LinkUp) throw new InvalidOperationException("Disconnect before changing the network adapter.");
        SaveSettings(_settings with { Adapter = name });
        Log("setting", $"Network adapter set to {name}", user);
    }

    void SaveRule(AlarmRule rule, string user)
    {
        int index = _rules.FindIndex(r => r.Code == rule.Code);
        if (index >= 0) _rules[index] = rule; else _rules.Add(rule);
        _store.SaveAlarmRule(rule);
        Log("setting", $"Alarm rule {rule.Code} {(index >= 0 ? "changed" : "added")}: {rule.Text}", user);
    }

    void DeleteRule(string code, string user)
    {
        if (_rules.RemoveAll(r => r.Code == code) == 0) throw new InvalidOperationException($"There is no alarm rule {code}.");
        _store.DeleteAlarmRule(code);
        Log("setting", $"Alarm rule {code} deleted", user);
    }

    // ---- Re-measure: mark the bar, Start, turn it a whole number of times, Finish, then Save. ----

    void BeginRemeasure(string user)
    {
        RequireGoodPosition();
        _remeasureStart = (_time.GetUtcNow(), _tracker.Unwrapped);
        _proposed = null;
        Log("calibration", "Re-measure started", user);
    }

    void CompleteRemeasure(int turns, double diameterMm, string user)
    {
        if (_remeasureStart is not { } start) throw new InvalidOperationException("Press Start first.");
        RequireGoodPosition();

        double perRev = ShaftMaths.CountsPerRevFromTurns(start.StartCounts, _tracker.Unwrapped, turns);
        var proposed = new Calibration(diameterMm, perRev);
        _proposed = proposed;
        Log("calibration", $"Re-measure: {perRev:0} counts/rev over {turns} turns on a {diameterMm:0.###} mm bar " +
                           $"({PercentChange(proposed):+0.00;-0.00}% from now)", user);
    }

    void AcceptRemeasure(string user)
    {
        if (_proposed is not { } proposed) throw new InvalidOperationException("Finish the re-measure first.");
        var old = _settings.Calibration;
        SaveSettings(_settings with { Calibration = proposed });
        Log("calibration", $"Calibration {old.DiameterMm:0.###} mm / {old.CountsPerRev:0} -> " +
                           $"{proposed.DiameterMm:0.###} mm / {proposed.CountsPerRev:0} counts/rev", user);
        _remeasureStart = null;
        _proposed = null;
    }

    void AbandonRemeasure(string user)
    {
        _remeasureStart = null;
        _proposed = null;
        Log("calibration", "Re-measure cancelled", user);
    }

    RemeasureStatus? RemeasureNow() =>
        _remeasureStart is { } start
            ? new RemeasureStatus(start.StartedAt, _tracker.Unwrapped - start.StartCounts, _proposed)
            : null;

    // Compared per mm of surface
    double PercentChange(Calibration proposed) => (proposed.CountsPerMm / _settings.Calibration.CountsPerMm - 1) * 100;

    void RequireGoodPosition()
    {
        if (Latest.PositionQuality != Quality.Good)
            throw new InvalidOperationException("Needs a good position reading.");
    }
}