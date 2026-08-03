using SightForge.Models;

namespace SightForge.Services;

public sealed class ReplayBufferService
{
    private const long DefaultMaximumBytes = 384L * 1024L * 1024L;

    private readonly object _gate = new();
    private readonly LinkedList<RecordedFrame> _frames = new();
    private TimeSpan _retention = TimeSpan.FromSeconds(20);
    private long _storedBytes;

    public TimeSpan Retention
    {
        get => _retention;
        set => _retention = TimeSpan.FromSeconds(Math.Clamp(value.TotalSeconds, 5, 120));
    }

    public long MaximumBytes { get; set; } = DefaultMaximumBytes;

    public int Count
    {
        get { lock (_gate) return _frames.Count; }
    }

    public long StoredBytes
    {
        get { lock (_gate) return _storedBytes; }
    }

    public void Add(RecordedFrame frame)
    {
        lock (_gate)
        {
            _frames.AddLast(frame);
            _storedBytes += frame.Pixels.LongLength;
            var cutoff = frame.Timestamp - _retention;

            while (_frames.First is not null &&
                   (_frames.First.Value.Timestamp < cutoff || _storedBytes > MaximumBytes))
            {
                _storedBytes -= _frames.First.Value.Pixels.LongLength;
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
        lock (_gate)
        {
            _frames.Clear();
            _storedBytes = 0;
        }
    }
}
