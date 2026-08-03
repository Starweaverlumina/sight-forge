using SightForge.Models;

namespace SightForge.Services;

public sealed class VisualProcessor
{
    private byte[]? _previousLuminance;
    private int _previousWidth;
    private int _previousHeight;

    public void ResetMotionHistory()
    {
        _previousLuminance = null;
        _previousWidth = 0;
        _previousHeight = 0;
    }

    public void Process(CapturedFrame frame, VisualSettings settings)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(settings);

        if (frame.Pixels.Length < frame.Stride * frame.Height)
        {
            throw new ArgumentException("The frame pixel buffer is smaller than its dimensions require.", nameof(frame));
        }

        var pixelCount = frame.Width * frame.Height;
        var currentLuminance = new byte[pixelCount];
        var sourceLuminance = settings.EdgeEnhancementEnabled
            ? new byte[pixelCount]
            : currentLuminance;

        ApplyToneAndMeasureLuminance(frame, settings, currentLuminance, sourceLuminance);

        if (settings.EdgeEnhancementEnabled)
        {
            ApplyEdgeEnhancement(frame, sourceLuminance);
        }

        if (settings.MotionEmphasisEnabled &&
            _previousLuminance is not null &&
            _previousWidth == frame.Width &&
            _previousHeight == frame.Height)
        {
            ApplyMotionEmphasis(frame, currentLuminance, _previousLuminance, settings.MotionThreshold);
        }

        _previousLuminance = currentLuminance;
        _previousWidth = frame.Width;
        _previousHeight = frame.Height;
    }

    private static void ApplyToneAndMeasureLuminance(
        CapturedFrame frame,
        VisualSettings settings,
        byte[] currentLuminance,
        byte[] sourceLuminance)
    {
        var contrast = Math.Clamp(settings.Contrast, 0.5, 2.5);
        var gamma = Math.Clamp(settings.Gamma, 0.4, 2.2);
        var inverseGamma = 1.0 / gamma;
        var gammaTable = BuildGammaTable(inverseGamma);

        var luminanceIndex = 0;

        for (var y = 0; y < frame.Height; y++)
        {
            var row = y * frame.Stride;

            for (var x = 0; x < frame.Width; x++)
            {
                var index = row + (x * 4);
                var blue = Adjust(frame.Pixels[index], contrast, gammaTable);
                var green = Adjust(frame.Pixels[index + 1], contrast, gammaTable);
                var red = Adjust(frame.Pixels[index + 2], contrast, gammaTable);

                frame.Pixels[index] = blue;
                frame.Pixels[index + 1] = green;
                frame.Pixels[index + 2] = red;
                frame.Pixels[index + 3] = 255;

                var luminance = (byte)Math.Clamp(
                    (int)Math.Round((red * 0.2126) + (green * 0.7152) + (blue * 0.0722)),
                    0,
                    255);

                currentLuminance[luminanceIndex] = luminance;
                sourceLuminance[luminanceIndex] = luminance;
                luminanceIndex++;
            }
        }
    }

    private static byte[] BuildGammaTable(double inverseGamma)
    {
        var table = new byte[256];

        for (var value = 0; value < table.Length; value++)
        {
            var normalized = value / 255.0;
            table[value] = (byte)Math.Clamp(
                (int)Math.Round(Math.Pow(normalized, inverseGamma) * 255.0),
                0,
                255);
        }

        return table;
    }

    private static byte Adjust(byte value, double contrast, IReadOnlyList<byte> gammaTable)
    {
        var contrasted = ((value / 255.0 - 0.5) * contrast) + 0.5;
        var clamped = (int)Math.Round(Math.Clamp(contrasted, 0.0, 1.0) * 255.0);
        return gammaTable[clamped];
    }

    private static void ApplyEdgeEnhancement(CapturedFrame frame, IReadOnlyList<byte> luminance)
    {
        const int edgeThreshold = 58;

        for (var y = 1; y < frame.Height - 1; y++)
        {
            for (var x = 1; x < frame.Width - 1; x++)
            {
                var topLeft = luminance[((y - 1) * frame.Width) + x - 1];
                var top = luminance[((y - 1) * frame.Width) + x];
                var topRight = luminance[((y - 1) * frame.Width) + x + 1];
                var left = luminance[(y * frame.Width) + x - 1];
                var right = luminance[(y * frame.Width) + x + 1];
                var bottomLeft = luminance[((y + 1) * frame.Width) + x - 1];
                var bottom = luminance[((y + 1) * frame.Width) + x];
                var bottomRight = luminance[((y + 1) * frame.Width) + x + 1];

                var gradientX = -topLeft + topRight - (2 * left) + (2 * right) - bottomLeft + bottomRight;
                var gradientY = -topLeft - (2 * top) - topRight + bottomLeft + (2 * bottom) + bottomRight;
                var magnitude = Math.Abs(gradientX) + Math.Abs(gradientY);

                if (magnitude < edgeThreshold)
                {
                    continue;
                }

                var index = (y * frame.Stride) + (x * 4);
                var strength = (byte)Math.Clamp(magnitude / 4, 80, 255);

                // Warm high-visibility outline. This marks visual boundaries only;
                // it does not classify people, enemies, or game objects.
                frame.Pixels[index] = (byte)Math.Min(frame.Pixels[index] + (strength / 8), 255);
                frame.Pixels[index + 1] = (byte)Math.Min(frame.Pixels[index + 1] + (strength / 2), 255);
                frame.Pixels[index + 2] = (byte)Math.Min(frame.Pixels[index + 2] + strength, 255);
            }
        }
    }

    private static void ApplyMotionEmphasis(
        CapturedFrame frame,
        IReadOnlyList<byte> current,
        IReadOnlyList<byte> previous,
        byte threshold)
    {
        var safeThreshold = Math.Clamp(threshold, (byte)5, (byte)120);

        for (var y = 0; y < frame.Height; y++)
        {
            for (var x = 0; x < frame.Width; x++)
            {
                var luminanceIndex = (y * frame.Width) + x;
                var difference = Math.Abs(current[luminanceIndex] - previous[luminanceIndex]);

                if (difference < safeThreshold)
                {
                    continue;
                }

                var index = (y * frame.Stride) + (x * 4);
                var intensity = (byte)Math.Clamp(110 + difference, 110, 255);

                // General visible-pixel motion only. All qualifying motion is treated equally.
                frame.Pixels[index] = 20;
                frame.Pixels[index + 1] = (byte)Math.Min(40 + (intensity / 5), 100);
                frame.Pixels[index + 2] = intensity;
                frame.Pixels[index + 3] = 255;
            }
        }
    }
}
