namespace SightForge.Models;

public sealed class AccessibilityProfilePack
{
    public int SchemaVersion { get; set; } = 1;
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = "New accessibility profile";
    public string? GameTitle { get; set; }
    public string? Platform { get; set; }
    public string? ExecutableName { get; set; }
    public string CreatedBy { get; set; } = "Local user";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool CommunityReviewed { get; set; }
    public string ReviewStatus { get; set; } = "Local";
    public VisualSettings Vision { get; set; } = new();
    public SoundForgeSettings Sound { get; set; } = new();
    public NarrationSettings Narration { get; set; } = new();
    public OcrRegionSettings[] OcrRegions { get; set; } = Array.Empty<OcrRegionSettings>();
    public ProfileSource[] Sources { get; set; } = Array.Empty<ProfileSource>();
    public Dictionary<string, string> Notes { get; set; } = new();
}

public sealed class NarrationSettings
{
    public bool Enabled { get; set; } = true;
    public bool SpeakClockDirections { get; set; }
    public bool SpeakOcrResults { get; set; } = true;
    public bool SpeakFocusedControls { get; set; } = true;
    public int Rate { get; set; }
    public int Volume { get; set; } = 85;
    public string? VoiceName { get; set; }
    public string AssistanceLevel { get; set; } = "Normal";
}

public sealed class OcrRegionSettings
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = "Text region";
    public double LeftPercent { get; set; }
    public double TopPercent { get; set; }
    public double WidthPercent { get; set; } = 1;
    public double HeightPercent { get; set; } = 1;
    public bool AutoRead { get; set; }
    public int MinimumIntervalMilliseconds { get; set; } = 1500;
}

public sealed class ProfileSource
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset LastVerified { get; set; } = DateTimeOffset.UtcNow;
    public string TrustLevel { get; set; } = "User supplied";
}
