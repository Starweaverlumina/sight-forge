using System.Collections.Concurrent;
using System.Speech.Synthesis;

namespace SightForge.Services;

public enum NarrationPriority
{
    Background = 0,
    Normal = 1,
    Important = 2,
    Critical = 3
}

public sealed record NarrationRequest(
    string Text,
    NarrationPriority Priority = NarrationPriority.Normal,
    bool InterruptLowerPriority = true,
    string? DeduplicationKey = null);

public sealed class AccessibleNarrationService : IDisposable
{
    private readonly SpeechSynthesizer _synthesizer = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _recent = new();
    private readonly object _gate = new();
    private NarrationPriority _activePriority = NarrationPriority.Background;
    private bool _disposed;

    public AccessibleNarrationService()
    {
        _synthesizer.SetOutputToDefaultAudioDevice();
        _synthesizer.Rate = 0;
        _synthesizer.Volume = 85;
        _synthesizer.SpeakCompleted += (_, _) =>
        {
            lock (_gate) _activePriority = NarrationPriority.Background;
        };
    }

    public int Rate
    {
        get => _synthesizer.Rate;
        set => _synthesizer.Rate = Math.Clamp(value, -10, 10);
    }

    public int Volume
    {
        get => _synthesizer.Volume;
        set => _synthesizer.Volume = Math.Clamp(value, 0, 100);
    }

    public IReadOnlyList<string> InstalledVoices =>
        _synthesizer.GetInstalledVoices()
            .Where(voice => voice.Enabled)
            .Select(voice => voice.VoiceInfo.Name)
            .ToArray();

    public void SelectVoice(string voiceName)
    {
        if (string.IsNullOrWhiteSpace(voiceName)) return;
        _synthesizer.SelectVoice(voiceName);
    }

    public void Speak(NarrationRequest request)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(request.Text)) return;

        var key = request.DeduplicationKey ?? request.Text;
        var now = DateTimeOffset.UtcNow;
        if (_recent.TryGetValue(key, out var previous) && now - previous < TimeSpan.FromSeconds(1.5)) return;
        _recent[key] = now;

        lock (_gate)
        {
            if (request.InterruptLowerPriority && request.Priority >= _activePriority)
                _synthesizer.SpeakAsyncCancelAll();
            else if (request.Priority < _activePriority)
                return;

            _activePriority = request.Priority;
            _synthesizer.SpeakAsync(request.Text);
        }

        foreach (var item in _recent.Where(pair => now - pair.Value > TimeSpan.FromMinutes(2)).ToArray())
            _recent.TryRemove(item.Key, out _);
    }

    public void Stop()
    {
        lock (_gate)
        {
            _synthesizer.SpeakAsyncCancelAll();
            _activePriority = NarrationPriority.Background;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _synthesizer.Dispose();
    }
}
