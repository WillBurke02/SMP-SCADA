using System;
using System.Collections.Generic;
using System.Text;

namespace SMP_SCADA.Core;

// Restart continuity
public readonly record struct TrackerState(int LastRaw, long Unwrapped, long Zero, long Travel);

public sealed class PositionTracker
{
    public static readonly TimeSpan SpeedWindow = TimeSpan.FromMilliseconds(500);

    int? _lastRaw;
    long _anchor; //where travel was last counted from
    DateTimeOffset _lastTime;
    readonly Queue<(DateTimeOffset Time, long Position)> _window = new(); // fifo buffer
    
    public PositionTracker(TrackerState? saved = null)
    {
        if (saved is not { } s) return;
        _lastRaw = s.LastRaw;
        Unwrapped = s.Unwrapped;
        Zero = s.Zero;
        TravelCounts = s.Travel;
        _anchor = s.Unwrapped;
    }
    public long Unwrapped { get; private set; } 
    public long Zero { get; private set; }
    public long TravelCounts { get; private set; }
    public long CountsFromZero => Unwrapped - Zero;

    public int DeadbandCounts
    {
        get;
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); field = value; }
    }

    public TrackerState? State => _lastRaw is int last ? new(last, Unwrapped, Zero, TravelCounts) : null;

    public void Update(int raw, DateTimeOffset time)
    {
        if (_lastRaw is int last)
            Unwrapped += ShaftMaths.Delta(raw, last);
        else // must be first reading
            Unwrapped = Zero = _anchor = raw;
        _lastRaw = raw;

        long moved = Math.Abs(Unwrapped - _anchor);
        if (moved >= DeadbandCounts)
        {
            TravelCounts += moved;
            _anchor = Unwrapped;
        }

        if (_window.Count > 0 && time <= _lastTime) _window.Clear();
        _window.Enqueue((time, Unwrapped));
        _lastTime = time;
        while (time - _window.Peek().Time > SpeedWindow) _window.Dequeue();
    }

    public double? SpeedCountsPerSecond
    {
        get
        {
            if (_window.Count < 2) return null;
            var (t0, p0) = _window.Peek();
            return (Unwrapped - p0) / (_lastTime - t0).TotalSeconds;
        }
    }

    public void SetZero()
    {
        Zero = _anchor = Unwrapped;
        TravelCounts = 0;
    }
}