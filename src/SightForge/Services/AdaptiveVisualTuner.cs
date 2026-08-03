using SightForge.Models;

namespace SightForge.Services;

public sealed class AdaptiveVisualTuner
{
    private double _smoothedLuminance = 0.5;
    private double _smoothedSpread = 0.25;
    private double _smoothedMotion;

    public RecordedFrame AnalyzeAndRecord(byte[] pixels, int width, int height, int stride, byte[]? previous)
    {
        long luminanceSum = 0;
        long luminanceSquaredSum = 0;
        long motionPixels = 0;
        var sampleCount = 0;

        for (var y = 0; y < height; y += 4)
        {
            var row = y * stride;
            for (var x = 0; x < width; x += 4)
            {
                var index = row + (x * 4);
                var b = pixels[index];
                var g = pixels[index + 1];
                var r = pixels[index + 2];
                var luminance = (54 * r + 183 * g + 19 * b) >> 8;
                luminanceSum += luminance;
                luminanceSquaredSum += luminance * luminance;
                sampleCount++;

                if (previous is not null && previous.Length == pixels.Length)
                {
                    var difference = Math.Abs(r - previous[index + 2]) +
                                     Math.Abs(g - previous[index + 1]) +
                                     Math.Abs(b - previous[index]);
                    if (difference > 72) motionPixels++;
                }
            }
        }

        var mean = sampleCount == 0 ? 0.5 : luminanceSum / (sampleCount * 255.0);
        var meanSquare = sampleCount == 0 ? 0 : luminanceSquaredSum / (double)sampleCount;
        var variance = Math.Max(0, meanSquare - Math.Pow(mean * 255.0, 2));
        var spread = Math.Sqrt(variance) / 128.0;
        var motion = sampleCount == 0 ? 0 : motionPixels / (double)sampleCount;

        _smoothedLuminance = Smooth(_smoothedLuminance, mean, 0.08);
        _smoothedSpread = Smooth(_smoothedSpread, spread, 0.08);
        _smoothedMotion = Smooth(_smoothedMotion, motion, 0.12);

        return new RecordedFrame(DateTimeOffset.UtcNow, width, height, stride,
            (byte[])pixels.Clone(), mean, spread, motion);
    }

    public bool ApplySafeAdaptation(VisualSettings settings)
    {
        var desiredGamma = _smoothedLuminance switch
        {
            < 0.20 => 1.45,
            < 0.32 => 1.25,
            > 0.80 => 0.82,
            > 0.68 => 0.92,
            _ => 1.0
        };

        var desiredContrast = _smoothedSpread switch
        {
            < 0.18 => 1.35,
            < 0.28 => 1.18,
            > 0.72 => 0.92,
            _ => 1.0
        };

        var desiredThreshold = _smoothedMotion > 0.35 ? 44 :
                               _smoothedMotion > 0.18 ? 34 : 26;

        var oldGamma = settings.Gamma;
        var oldContrast = settings.Contrast;
        var oldThreshold = settings.MotionThreshold;

        settings.Gamma = MoveToward(settings.Gamma, desiredGamma, 0.025, 0.65, 1.65);
        settings.Contrast = MoveToward(settings.Contrast, desiredContrast, 0.025, 0.8, 1.55);
        settings.MotionThreshold = (byte)Math.Clamp(
            oldThreshold + Math.Sign(desiredThreshold - oldThreshold), 16, 60);

        return Math.Abs(settings.Gamma - oldGamma) > 0.0001 ||
               Math.Abs(settings.Contrast - oldContrast) > 0.0001 ||
               settings.MotionThreshold != oldThreshold;
    }

    public void Reset()
    {
        _smoothedLuminance = 0.5;
        _smoothedSpread = 0.25;
        _smoothedMotion = 0;
    }

    private static double Smooth(double current, double incoming, double rate) =>
        current + ((incoming - current) * rate);

    private static double MoveToward(double value, double target, double step, double minimum, double maximum)
    {
        var next = value + Math.Clamp(target - value, -step, step);
        return Math.Clamp(next, minimum, maximum);
    }
}
