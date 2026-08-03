using SightForge.Models;

namespace SightForge.Services;

public sealed class DirectionalCueEngine : IDisposable
{
    private readonly MotionGridAnalyzer _analyzer = new();
    private readonly DirectionalToneService _tones = new();
    private byte[]? _previousPixels;

    public GridMotionResult? Process(RecordedFrame frame, VisualSettings settings)
    {
        var result = _analyzer.FindStrongestRegion(
            frame.Pixels,
            _previousPixels,
            frame.Width,
            frame.Height,
            frame.Stride,
            settings.MotionThreshold);

        _previousPixels = (byte[])frame.Pixels.Clone();

        if (settings.DirectionalAudioEnabled && result is not null)
        {
            _tones.TryPlay(
                result,
                settings.DirectionalAudioFrequencies,
                settings.DirectionalAudioVolume,
                settings.DirectionalAudioCooldownMilliseconds);
        }

        return result;
    }

    public void Preview(GridDirection direction, VisualSettings settings) =>
        _tones.Preview(direction, settings.DirectionalAudioFrequencies, settings.DirectionalAudioVolume);

    public void Reset() => _previousPixels = null;

    public void Dispose() => _tones.Dispose();
}
