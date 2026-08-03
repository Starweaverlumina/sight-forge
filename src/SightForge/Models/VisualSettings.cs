namespace SightForge.Models;

public sealed class VisualSettings
{
    public double Magnification { get; set; } = 1.0;

    public double Contrast { get; set; } = 1.0;

    public double Gamma { get; set; } = 1.0;

    public bool EdgeEnhancementEnabled { get; set; }

    public bool MotionEmphasisEnabled { get; set; }

    public bool CenterGuideEnabled { get; set; } = true;

    public byte MotionThreshold { get; set; } = 28;

    public int PreviewWidth { get; set; } = 960;

    public int PreviewHeight { get; set; } = 540;

    public int TargetFramesPerSecond { get; set; } = 15;

    public VisualSettings Clone() => (VisualSettings)MemberwiseClone();
}
