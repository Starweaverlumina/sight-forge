using NAudio.Wave;
using SightForge.Models;

namespace SightForge.Services;

public sealed class WasapiLoopbackDirectionService : IDisposable
{
    private readonly object _gate = new();
    private WasapiLoopbackCapture? _capture;
    private SoundForgeSettings _settings = new();
    private DateTimeOffset _lastPublished = DateTimeOffset.MinValue;
    private bool _disposed;

    public event EventHandler<AudioDirectionSnapshot>? DirectionChanged;
    public event EventHandler<Exception>? CaptureFailed;

    public bool IsRunning { get; private set; }

    public void Start(SoundForgeSettings settings)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(settings);

        lock (_gate)
        {
            if (IsRunning) return;
            _settings = settings;
            _capture = new WasapiLoopbackCapture();
            _capture.DataAvailable += Capture_DataAvailable;
            _capture.RecordingStopped += Capture_RecordingStopped;
            _capture.StartRecording();
            IsRunning = true;
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (!IsRunning) return;
            IsRunning = false;
            _capture?.StopRecording();
        }
    }

    private void Capture_DataAvailable(object? sender, WaveInEventArgs e)
    {
        try
        {
            var capture = _capture;
            if (capture is null || e.BytesRecorded <= 0) return;

            var snapshot = AnalyzeBuffer(e.Buffer, e.BytesRecorded, capture.WaveFormat, _settings);
            if (snapshot.CombinedLevel < _settings.MinimumAudibleLevel) return;

            var now = DateTimeOffset.UtcNow;
            if ((now - _lastPublished).TotalMilliseconds <
                Math.Clamp(_settings.MinimumEventIntervalMilliseconds, 50, 2000)) return;

            _lastPublished = now;
            DirectionChanged?.Invoke(this, snapshot);
        }
        catch (Exception exception)
        {
            CaptureFailed?.Invoke(this, exception);
        }
    }

    private void Capture_RecordingStopped(object? sender, StoppedEventArgs e)
    {
        lock (_gate)
        {
            IsRunning = false;
            if (_capture is not null)
            {
                _capture.DataAvailable -= Capture_DataAvailable;
                _capture.RecordingStopped -= Capture_RecordingStopped;
                _capture.Dispose();
                _capture = null;
            }
        }

        if (e.Exception is not null) CaptureFailed?.Invoke(this, e.Exception);
    }

    public static AudioDirectionSnapshot AnalyzeBuffer(
        byte[] buffer,
        int bytesRecorded,
        WaveFormat format,
        SoundForgeSettings settings)
    {
        if (format.Channels < 1) return AudioDirectionSnapshot.Silence;

        double leftSquares = 0;
        double rightSquares = 0;
        var leftSamples = 0;
        var rightSamples = 0;

        if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
        {
            var sampleCount = bytesRecorded / 4;
            for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
            {
                var value = BitConverter.ToSingle(buffer, sampleIndex * 4);
                Accumulate(value, sampleIndex % format.Channels, format.Channels,
                    ref leftSquares, ref rightSquares, ref leftSamples, ref rightSamples);
            }
        }
        else if (format.BitsPerSample == 16)
        {
            var sampleCount = bytesRecorded / 2;
            for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
            {
                var value = BitConverter.ToInt16(buffer, sampleIndex * 2) / 32768.0;
                Accumulate(value, sampleIndex % format.Channels, format.Channels,
                    ref leftSquares, ref rightSquares, ref leftSamples, ref rightSamples);
            }
        }
        else
        {
            return AudioDirectionSnapshot.Silence;
        }

        var left = leftSamples == 0 ? 0 : Math.Sqrt(leftSquares / leftSamples);
        var right = rightSamples == 0 ? left : Math.Sqrt(rightSquares / rightSamples);
        var combined = Math.Sqrt(((left * left) + (right * right)) / 2.0);
        var denominator = Math.Max(left + right, 0.000001);
        var balance = Math.Clamp((right - left) / denominator, -1.0, 1.0);
        var deadZone = Math.Clamp(settings.DirectionDeadZone, 0.02, 0.8);

        var direction = balance switch
        {
            < var value when value < -deadZone => HorizontalAudioDirection.Left,
            > var value when value > deadZone => HorizontalAudioDirection.Right,
            _ => HorizontalAudioDirection.Center
        };

        var confidence = Math.Clamp(Math.Abs(balance) / Math.Max(deadZone, 0.001), 0, 1);
        var clock = direction switch
        {
            HorizontalAudioDirection.Left => balance < -0.55 ? "9 o'clock" : "10 o'clock",
            HorizontalAudioDirection.Right => balance > 0.55 ? "3 o'clock" : "2 o'clock",
            _ => "12 o'clock"
        };

        return new AudioDirectionSnapshot(
            DateTimeOffset.UtcNow,
            direction,
            left,
            right,
            combined,
            balance,
            confidence,
            clock);
    }

    private static void Accumulate(
        double value,
        int channelIndex,
        int channelCount,
        ref double leftSquares,
        ref double rightSquares,
        ref int leftSamples,
        ref int rightSamples)
    {
        value = Math.Clamp(value, -1.0, 1.0);
        if (channelCount == 1 || channelIndex == 0)
        {
            leftSquares += value * value;
            leftSamples++;
            if (channelCount == 1)
            {
                rightSquares += value * value;
                rightSamples++;
            }
        }
        else if (channelIndex == 1)
        {
            rightSquares += value * value;
            rightSamples++;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        lock (_gate)
        {
            _capture?.Dispose();
            _capture = null;
        }
    }
}
