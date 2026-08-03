using SightForge.Models;

namespace SightForge.Services;

public sealed class MotionGridAnalyzer
{
    public GridMotionResult? FindStrongestRegion(
        byte[] current,
        byte[]? previous,
        int width,
        int height,
        int stride,
        byte threshold)
    {
        if (previous is null || previous.Length != current.Length)
        {
            return null;
        }

        var changed = new long[9];
        var sampled = new long[9];

        for (var y = 0; y < height; y += 4)
        {
            var row = Math.Min(2, (y * 3) / Math.Max(height, 1));
            var rowOffset = y * stride;

            for (var x = 0; x < width; x += 4)
            {
                var column = Math.Min(2, (x * 3) / Math.Max(width, 1));
                var cell = (row * 3) + column;
                var index = rowOffset + (x * 4);

                var difference = Math.Abs(current[index] - previous[index]) +
                                 Math.Abs(current[index + 1] - previous[index + 1]) +
                                 Math.Abs(current[index + 2] - previous[index + 2]);

                sampled[cell]++;
                if (difference >= threshold * 3)
                {
                    changed[cell]++;
                }
            }
        }

        var strongestCell = -1;
        var strongestRatio = 0.0;

        for (var cell = 0; cell < 9; cell++)
        {
            var ratio = sampled[cell] == 0 ? 0 : changed[cell] / (double)sampled[cell];
            if (ratio > strongestRatio)
            {
                strongestRatio = ratio;
                strongestCell = cell;
            }
        }

        return strongestCell >= 0 && strongestRatio >= 0.025
            ? new GridMotionResult((GridDirection)strongestCell, strongestRatio)
            : null;
    }
}
