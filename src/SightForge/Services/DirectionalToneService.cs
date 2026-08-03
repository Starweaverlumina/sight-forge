using System.Media;
using SightForge.Models;

namespace SightForge.Services;

public sealed class DirectionalToneService : IDisposable
{
    private readonly object _gate = new();
    private DateTimeOffset _lastCue = DateTimeOffset.MinValue;
    private SoundPlayer? _player;
    private MemoryStream? _stream;

    public void TryPlay(GridMotionResult result, IReadOnlyList<int> frequencies, double volume, int cooldownMilliseconds)
    {
        if (frequencies.Count < 9 || result.Strength <= 0) return;

        var now = DateTimeOffset.UtcNow;
        if ((now - _lastCue).TotalMilliseconds < Math.Clamp(cooldownMilliseconds, 100, 3000)) return;

        _lastCue = now;
        var index = (int)result.Direction;
        var strengthVolume = Math.Clamp(volume, 0.02, 0.65) * Math.Clamp(0.55 + result.Strength, 0.55, 1.0);
        PlaySpatialTone(result.Direction, Math.Clamp(frequencies[index], 120, 2400), strengthVolume, 115);
    }

    public void Preview(GridDirection direction, IReadOnlyList<int> frequencies, double volume)
    {
        var index = (int)direction;
        if (frequencies.Count < 9) return;
        PlaySpatialTone(direction, Math.Clamp(frequencies[index], 120, 2400), Math.Clamp(volume, 0.02, 0.65), 220);
    }

    private void PlaySpatialTone(GridDirection direction, int frequency, double volume, int durationMilliseconds)
    {
        var (azimuth, elevation) = GetPosition(direction);
        lock (_gate)
        {
            _player?.Stop();
            _player?.Dispose();
            _stream?.Dispose();
            _stream = new MemoryStream(CreateBinauralWave(frequency, azimuth, elevation, volume, durationMilliseconds));
            _player = new SoundPlayer(_stream);
            _player.Play();
        }
    }

    private static (double AzimuthDegrees, double ElevationDegrees) GetPosition(GridDirection direction) => direction switch
    {
        GridDirection.TopLeft => (-45, 35),
        GridDirection.TopCenter => (0, 45),
        GridDirection.TopRight => (45, 35),
        GridDirection.MiddleLeft => (-70, 0),
        GridDirection.Center => (0, 0),
        GridDirection.MiddleRight => (70, 0),
        GridDirection.BottomLeft => (-45, -30),
        GridDirection.BottomCenter => (0, -40),
        GridDirection.BottomRight => (45, -30),
        _ => (0, 0)
    };

    private static byte[] CreateBinauralWave(int frequency, double azimuthDegrees, double elevationDegrees, double volume, int durationMilliseconds)
    {
        const int sampleRate = 44_100;
        const short channels = 2;
        const short bitsPerSample = 16;
        var sampleCount = sampleRate * durationMilliseconds / 1000;
        var dataLength = sampleCount * channels * (bitsPerSample / 8);

        var azimuth = azimuthDegrees * Math.PI / 180.0;
        var elevation = elevationDegrees * Math.PI / 180.0;
        var interauralDelaySeconds = 0.00063 * Math.Sin(azimuth);
        var delaySamples = (int)Math.Round(Math.Abs(interauralDelaySeconds) * sampleRate);
        var nearEarGain = 1.0;
        var farEarGain = 0.52 + (0.38 * Math.Cos(azimuth));
        var leftGain = azimuthDegrees <= 0 ? nearEarGain : farEarGain;
        var rightGain = azimuthDegrees >= 0 ? nearEarGain : farEarGain;

        // Elevation is encoded by a subtle harmonic balance shift in addition to pitch.
        var harmonicGain = 0.12 + (0.10 * Math.Max(0, Math.Sin(elevation)));
        var lowShelf = 1.0 - (0.18 * Math.Max(0, -Math.Sin(elevation)));

        using var stream = new MemoryStream(44 + dataLength);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bitsPerSample / 8);
        writer.Write((short)(channels * bitsPerSample / 8));
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataLength);

        for (var sample = 0; sample < sampleCount; sample++)
        {
            var fadeSamples = Math.Max(1, sampleRate / 120);
            var envelope = Math.Min(1.0, sample / (double)fadeSamples) *
                           Math.Min(1.0, (sampleCount - sample) / (double)fadeSamples);

            var leftIndex = azimuthDegrees > 0 ? sample - delaySamples : sample;
            var rightIndex = azimuthDegrees < 0 ? sample - delaySamples : sample;
            var left = leftIndex < 0 ? 0 : SpatialSample(leftIndex, frequency, sampleRate, harmonicGain, lowShelf);
            var right = rightIndex < 0 ? 0 : SpatialSample(rightIndex, frequency, sampleRate, harmonicGain, lowShelf);

            writer.Write((short)Math.Clamp(left * envelope * volume * leftGain * short.MaxValue, short.MinValue, short.MaxValue));
            writer.Write((short)Math.Clamp(right * envelope * volume * rightGain * short.MaxValue, short.MinValue, short.MaxValue));
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static double SpatialSample(int sample, int frequency, int sampleRate, double harmonicGain, double lowShelf)
    {
        var phase = 2.0 * Math.PI * frequency * sample / sampleRate;
        return (Math.Sin(phase) * lowShelf) + (Math.Sin(phase * 2.0) * harmonicGain);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _player?.Stop();
            _player?.Dispose();
            _stream?.Dispose();
            _player = null;
            _stream = null;
        }
    }
}
