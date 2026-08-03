using System.Media;
using SightForge.Models;

namespace SightForge.Services;

public sealed class DirectionalToneService : IDisposable
{
    private readonly object _gate = new();
    private DateTimeOffset _lastCue = DateTimeOffset.MinValue;
    private SoundPlayer? _player;
    private MemoryStream? _stream;

    public void TryPlay(
        GridMotionResult result,
        IReadOnlyList<int> frequencies,
        double volume,
        int cooldownMilliseconds)
    {
        if (frequencies.Count < 9 || result.Strength <= 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if ((now - _lastCue).TotalMilliseconds < Math.Clamp(cooldownMilliseconds, 100, 3000))
        {
            return;
        }

        _lastCue = now;
        var index = (int)result.Direction;
        var frequency = Math.Clamp(frequencies[index], 120, 2400);
        var column = index % 3;
        var pan = column switch
        {
            0 => -0.82,
            2 => 0.82,
            _ => 0.0
        };

        var strengthVolume = Math.Clamp(volume, 0.02, 0.65) *
                             Math.Clamp(0.55 + result.Strength, 0.55, 1.0);
        PlayTone(frequency, pan, strengthVolume, 115);
    }

    public void Preview(GridDirection direction, IReadOnlyList<int> frequencies, double volume)
    {
        var index = (int)direction;
        if (frequencies.Count < 9) return;
        var pan = index % 3 switch { 0 => -0.82, 2 => 0.82, _ => 0.0 };
        PlayTone(Math.Clamp(frequencies[index], 120, 2400), pan, Math.Clamp(volume, 0.02, 0.65), 180);
    }

    private void PlayTone(int frequency, double pan, double volume, int durationMilliseconds)
    {
        lock (_gate)
        {
            _player?.Stop();
            _player?.Dispose();
            _stream?.Dispose();

            _stream = new MemoryStream(CreateStereoWave(frequency, pan, volume, durationMilliseconds));
            _player = new SoundPlayer(_stream);
            _player.Play();
        }
    }

    private static byte[] CreateStereoWave(int frequency, double pan, double volume, int durationMilliseconds)
    {
        const int sampleRate = 44_100;
        const short channels = 2;
        const short bitsPerSample = 16;
        var sampleCount = sampleRate * durationMilliseconds / 1000;
        var dataLength = sampleCount * channels * (bitsPerSample / 8);
        var leftGain = Math.Sqrt((1.0 - pan) / 2.0) * volume;
        var rightGain = Math.Sqrt((1.0 + pan) / 2.0) * volume;

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
            var fadeSamples = Math.Max(1, sampleRate / 100);
            var envelope = Math.Min(1.0, sample / (double)fadeSamples) *
                           Math.Min(1.0, (sampleCount - sample) / (double)fadeSamples);
            var wave = Math.Sin(2.0 * Math.PI * frequency * sample / sampleRate) * envelope;
            writer.Write((short)(wave * short.MaxValue * leftGain));
            writer.Write((short)(wave * short.MaxValue * rightGain));
        }

        writer.Flush();
        return stream.ToArray();
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
