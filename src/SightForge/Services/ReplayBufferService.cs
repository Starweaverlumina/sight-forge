using SightForge.Models;

namespace SightForge.Services;

public sealed class ReplayBufferService
{
    private readonly object _gate = new();
    private readonly LinkedList<RecordedFrame> _frames = new();
    private TimeSpan _retention = TimeSpan.FromSeconds(20);

    public TimeSpan Retention
    {
        get => _retention;
        set => _retention = TimeSpan.FromSeconds(Math.Clamp(value.TotalSeconds, 5, 120));
    }

    public int Count
    {
        get { lock (_gate) return _frames.Count; }
    }

    public void Add(RecordedFrame frame)
    {
        lock (_gate)
        {
            _frames.AddLast(frame);
            var cutoff = frame.Timestamp - _retention;
            while (_frames.First is not null && _frames.First.Value.Timestamp < cutoff)
            {
                _frames.RemoveFirst();
            }
        }
    }

    public IReadOnlyList<RecordedFrame> Snapshot()
    {
        lock (_gate)
        {
            return _frames.ToArray();
        }
    }

    public void Clear()
    {
        lock (_gate) _frames.Clear();
    }
}
