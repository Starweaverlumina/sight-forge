namespace SightForge.Models;

public sealed class EventCueStyle
{
    public string EventId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#FFFFFF";
    public string Shape { get; set; } = "Wedge";
    public string PulsePattern { get; set; } = "Steady";
    public double Opacity { get; set; } = 1.0;
    public double Thickness { get; set; } = 6.0;

    public EventCueStyle Clone() => (EventCueStyle)MemberwiseClone();
}

public static class EventCueStyleDefaults
{
    public static EventCueStyle[] CreateStandard() =>
    [
        new()
        {
            EventId = "gunfire",
            DisplayName = "Gunfire",
            ColorHex = "#FF2B2B",
            Shape = "SolidWedge",
            PulsePattern = "FastPulse",
            Opacity = 1.0,
            Thickness = 10.0
        },
        new()
        {
            EventId = "footsteps",
            DisplayName = "Footsteps",
            ColorHex = "#00E5FF",
            Shape = "DashedWedge",
            PulsePattern = "RhythmicPulse",
            Opacity = 1.0,
            Thickness = 8.0
        },
        new()
        {
            EventId = "explosion",
            DisplayName = "Explosion",
            ColorHex = "#FF8A00",
            Shape = "FilledArc",
            PulsePattern = "ExpandingRing",
            Opacity = 1.0,
            Thickness = 12.0
        },
        new()
        {
            EventId = "vehicle",
            DisplayName = "Vehicle / heavy movement",
            ColorHex = "#FFE600",
            Shape = "DoubleOutline",
            PulsePattern = "SlowSweep",
            Opacity = 1.0,
            Thickness = 9.0
        },
        new()
        {
            EventId = "user-ping",
            DisplayName = "User ping",
            ColorHex = "#44FF66",
            Shape = "Diamond",
            PulsePattern = "Flash",
            Opacity = 1.0,
            Thickness = 7.0
        },
        new()
        {
            EventId = "general-motion",
            DisplayName = "General visible motion",
            ColorHex = "#FFFFFF",
            Shape = "OutlineWedge",
            PulsePattern = "Steady",
            Opacity = 0.85,
            Thickness = 6.0
        }
    ];

    public static EventCueStyle[] CreateHighContrastColorBlindSafe() =>
    [
        new() { EventId = "gunfire", DisplayName = "Gunfire", ColorHex = "#FF3B30", Shape = "SolidWedge", PulsePattern = "FastPulse", Thickness = 12.0 },
        new() { EventId = "footsteps", DisplayName = "Footsteps", ColorHex = "#00FFFF", Shape = "DashedWedge", PulsePattern = "RhythmicPulse", Thickness = 9.0 },
        new() { EventId = "explosion", DisplayName = "Explosion", ColorHex = "#FFFFFF", Shape = "FilledArc", PulsePattern = "ExpandingRing", Thickness = 14.0 },
        new() { EventId = "vehicle", DisplayName = "Vehicle / heavy movement", ColorHex = "#FFD60A", Shape = "DoubleOutline", PulsePattern = "SlowSweep", Thickness = 10.0 },
        new() { EventId = "user-ping", DisplayName = "User ping", ColorHex = "#BF5AF2", Shape = "Diamond", PulsePattern = "Flash", Thickness = 8.0 },
        new() { EventId = "general-motion", DisplayName = "General visible motion", ColorHex = "#64D2FF", Shape = "OutlineWedge", PulsePattern = "Steady", Opacity = 0.9, Thickness = 7.0 }
    ];
}
