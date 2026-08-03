namespace SightForge.Models;

public sealed record RecordedFrame(
    DateTimeOffset Timestamp,
    int Width,
    int Height,
    int Stride,
    byte[] Pixels,
    double MeanLuminance,
    double ContrastSpread,
    double MotionRatio);
