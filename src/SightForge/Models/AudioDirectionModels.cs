namespace SightForge.Models;

public enum HorizontalAudioDirection
{
    Left,
    Center,
    Right
}

public sealed record AudioDirectionSnapshot(
    DateTimeOffset Timestamp,
    HorizontalAudioDirection Direction,
    double LeftLevel,
    double RightLevel,
    double CombinedLevel,
    double Balance,
    double Confidence,
    string ClockPosition)
{
    public static AudioDirectionSnapshot Silence { get; } = new(
        DateTimeOffset.MinValue,
        HorizontalAudioDirection.Center,
        0,
        0,
        0,
        0,
        0,
        "12 o'clock");
}

public sealed class SoundForgeSettings
{
    public bool Enabled { get; set; }
    public bool MonoListeningEnabled { get; set; }
    public bool AnnounceOnlyHighConfidence { get; set; } = true;
    public double MinimumAudibleLevel { get; set; } = 0.015;
    public double DirectionDeadZone { get; set; } = 0.12;
    public int AnalysisWindowMilliseconds { get; set; } = 80;
    public int MinimumEventIntervalMilliseconds { get; set; } = 180;
}
