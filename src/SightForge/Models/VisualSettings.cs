namespace SightForge.Models;

public sealed class VisualSettings
{
    public double Magnification { get; set; } = 1.0;
    public double Contrast { get; set; } = 1.0;
    public double Gamma { get; set; } = 1.0;
    public bool EdgeEnhancementEnabled { get; set; }
    public bool MotionEmphasisEnabled { get; set; }
    public bool CenterGuideEnabled { get; set; } = true;
    public bool AdaptiveTuningEnabled { get; set; }
    public bool RollingReplayEnabled { get; set; } = true;
    public bool DirectionalAudioEnabled { get; set; }
    public double DirectionalAudioVolume { get; set; } = 0.18;
    public int DirectionalAudioCooldownMilliseconds { get; set; } = 550;
    public int[] DirectionalAudioFrequencies { get; set; } =
    [
        920, 1000, 1080,
        560, 640, 720,
        280, 340, 400
    ];
    public byte MotionThreshold { get; set; } = 28;
    public int ReplaySeconds { get; set; } = 20;
    public int PreviewWidth { get; set; } = 960;
    public int PreviewHeight { get; set; } = 540;
    public int TargetFramesPerSecond { get; set; } = 15;

    public VisualSettings Clone()
    {
        var clone = (VisualSettings)MemberwiseClone();
        clone.DirectionalAudioFrequencies = (int[])DirectionalAudioFrequencies.Clone();
        return clone;
    }
}
