using System.Threading.Channels;

namespace SightForge.Platform;

public sealed class AssistanceScheduler : IAsyncDisposable
{
    private const int MaximumQueuedMessages = 64;
    private readonly object _gate = new();
    private readonly List<AssistanceMessage> _pending = new();
    private readonly SemaphoreSlim _available = new(0);
    private readonly HashSet<string> _deduplicationKeys = new(StringComparer.Ordinal);
    private bool _disposed;

    public ValueTask PublishAsync(
        AssistanceMessage message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_gate)
        {
            RemoveExpired(DateTimeOffset.UtcNow);

            if (!string.IsNullOrWhiteSpace(message.DeduplicationKey) &&
                !_deduplicationKeys.Add(message.DeduplicationKey))
            {
                return ValueTask.CompletedTask;
            }

            if (_pending.Count >= MaximumQueuedMessages)
            {
                var removableIndex = FindLowestPriorityIndex();
                if (removableIndex >= 0 &&
                    _pending[removableIndex].Priority <= message.Priority)
                {
                    RemoveAt(removableIndex);
                }
                else
                {
                    return ValueTask.CompletedTask;
                }
            }

            _pending.Add(message);
            _available.Release();
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask<AssistanceMessage> ReadNextAsync(
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            await _available.WaitAsync(cancellationToken);
            lock (_gate)
            {
                RemoveExpired(DateTimeOffset.UtcNow);
                if (_pending.Count == 0) continue;

                var selectedIndex = 0;
                for (var index = 1; index < _pending.Count; index++)
                {
                    var candidate = _pending[index];
                    var selected = _pending[selectedIndex];
                    if (candidate.Priority > selected.Priority ||
                        candidate.Priority == selected.Priority && candidate.CreatedAt < selected.CreatedAt)
                    {
                        selectedIndex = index;
                    }
                }

                var message = _pending[selectedIndex];
                RemoveAt(selectedIndex);
                return message;
            }
        }
    }

    public int Count
    {
        get
        {
            lock (_gate) return _pending.Count;
        }
    }

    public void ClearBelow(AssistancePriority minimumPriority)
    {
        lock (_gate)
        {
            for (var index = _pending.Count - 1; index >= 0; index--)
            {
                if (_pending[index].Priority < minimumPriority)
                {
                    RemoveAt(index);
                }
            }
        }
    }

    private void RemoveExpired(DateTimeOffset now)
    {
        for (var index = _pending.Count - 1; index >= 0; index--)
        {
            var message = _pending[index];
            if (message.ExpiresAfter is not null && now - message.CreatedAt > message.ExpiresAfter)
            {
                RemoveAt(index);
            }
        }
    }

    private int FindLowestPriorityIndex()
    {
        if (_pending.Count == 0) return -1;
        var index = 0;
        for (var candidate = 1; candidate < _pending.Count; candidate++)
        {
            if (_pending[candidate].Priority < _pending[index].Priority ||
                _pending[candidate].Priority == _pending[index].Priority &&
                _pending[candidate].CreatedAt < _pending[index].CreatedAt)
            {
                index = candidate;
            }
        }
        return index;
    }

    private void RemoveAt(int index)
    {
        var key = _pending[index].DeduplicationKey;
        _pending.RemoveAt(index);
        if (!string.IsNullOrWhiteSpace(key)) _deduplicationKeys.Remove(key);
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        lock (_gate)
        {
            _pending.Clear();
            _deduplicationKeys.Clear();
        }
        _available.Dispose();
        return ValueTask.CompletedTask;
    }
}
