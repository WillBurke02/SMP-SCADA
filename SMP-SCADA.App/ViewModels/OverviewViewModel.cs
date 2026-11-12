using SMP_SCADA.Core;
using SMP_SCADA.Core.Service.Contract;
using SMP_SCADA.ViewModels;
using System.Globalization;
using static SMP_SCADA.Core.Service.Contract.Command;

namespace SMP_SCADA.ViewModels;

// Overview (F1)
public sealed class OverviewViewModel : ViewModelBase
{
    readonly ServiceConnection _service;
    Snapshot? _s;
    bool _fresh;
    string _diameterInput = "";
    string _diameterFromService = "";
    string? _diameterError;
    string? _zeroError;
    bool _confirmingZero;

    public OverviewViewModel(ServiceConnection service)
    {
        _service = service;
        ApplyDiameter = new RelayCommand(async _ => await ApplyDiameterAsync());
        AskZero = new RelayCommand(_ => ConfirmingZero = true, _ => !IsStale && !ConfirmingZero);
        CancelZero = new RelayCommand(_ => ConfirmingZero = false);
        ConfirmZero = new RelayCommand(async _ => await ZeroAsync(), _ => !IsStale);
    }

    public RelayCommand ApplyDiameter { get; }
    public RelayCommand AskZero { get; }
    public RelayCommand CancelZero { get; }
    public RelayCommand ConfirmZero { get; }

    public void Update(Snapshot? snapshot, bool fresh)
    {
        _s = snapshot;
        _fresh = fresh;
        if (snapshot is not null)
        {
            string fromService = snapshot.BarDiameterMm.ToString("0.0");
            if (_diameterInput == _diameterFromService) _diameterInput = fromService;
            _diameterFromService = fromService;
        }
        OnPropertyChanged(string.Empty);
        AskZero.RaiseCanExecuteChanged();
        ConfirmZero.RaiseCanExecuteChanged();
    }

    public bool IsStale => !_fresh || _s?.PositionQuality != Quality.Good;

    //Surface speed

    double? Speed => _s?.SurfaceSpeedMmS;

    public string SpeedText => Speed is double v ? Math.Abs(v).ToString("0.0") : "—";

    public string Direction => IsStale || Speed is not double v ? ""
        : Math.Round(v, 1) switch { > 0 => "Forward", < 0 => "Reverse", _ => "Stopped" };

    // surface speed over circumference
    double? TurnsPerSecond => Speed is double v && _s is { } s ? Math.Abs(v) / (Math.PI * s.BarDiameterMm) : null;

    public string RpmText => TurnsPerSecond is double t ? (t * 60).ToString("0.00") : "—";
    public string HzText => TurnsPerSecond is double t ? t.ToString("0.000") : "—";

    // Angle, distance, travel

    public double AngleDeg => _s?.AngleDeg ?? 0;

    public string AngleText
    {
        get
        {
            string text = AngleDeg.ToString("0.0");
            return text == "360.0" ? "0.0" : text; 
        }
    }

    public string DistanceText => (_s?.DistanceMm ?? 0).ToString("+#,##0.00;-#,##0.00;0.00");

    public string TravelText => ((_s?.TravelMm ?? 0) / 1000).ToString("#,##0.000");   // metres

    public string ZeroText => _s?.ZeroSetAt is { } at ? $"Zero set {at.ToLocalTime():d MMM yyyy HH:mm:ss}" : "";

    //Bar d

    public string DiameterInput
    {
        get => _diameterInput;
        set { if (Set(ref _diameterInput, value)) OnPropertyChanged(nameof(CircumferenceText)); }
    }

    public string CircumferenceText => TryReadDiameter(out double mm) ? (Math.PI * mm).ToString("#,##0.00") : "—";

    public string? DiameterError
    {
        get => _diameterError;
        private set => Set(ref _diameterError, value);
    }

    bool TryReadDiameter(out double mm) =>
        double.TryParse(_diameterInput, NumberStyles.Float, CultureInfo.CurrentCulture, out mm)
        && mm >= ShaftMaths.MinDiameterMm && mm <= ShaftMaths.MaxDiameterMm;

    async Task ApplyDiameterAsync()
    {
        if (!TryReadDiameter(out double mm))
        {
            DiameterError = $"{ShaftMaths.MinDiameterMm:0.0} to {ShaftMaths.MaxDiameterMm:0.0} mm";
            return;
        }
        var reply = await _service.SendAsync(id => new SetBarDiameter(id, mm));
        if (reply is Result { Ok: false } refused)
        {
            DiameterError = refused.Err;
            return;
        }
        DiameterError = null;
        DiameterInput = mm.ToString("0.0");
    }

    public bool ConfirmingZero
    {
        get => _confirmingZero;
        private set
        {
            Set(ref _confirmingZero, value);
            AskZero.RaiseCanExecuteChanged();
        }
    }

    public string? ZeroError
    {
        get => _zeroError;
        private set => Set(ref _zeroError, value);
    }

    async Task ZeroAsync()
    {
        ConfirmingZero = false;
        var reply = await _service.SendAsync(id => new SetZero(id));
        ZeroError = reply is Result { Ok: false } refused ? refused.Err : null;
    }

    // Calibration 

    public string CalibrationText => _s is { } s
        ? $"{s.Calibration.CountsPerRev:#,##0} counts per rev at {s.Calibration.DiameterMm:0.0} mm"
        : "";
}