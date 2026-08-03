namespace SightForge.Platform;

public static class BuiltInModuleCatalog
{
    public static IReadOnlyList<ModuleDescriptor> Create() =>
    [
        new(
            "vision.live-feed",
            "VisionForge Live Feed",
            "VisionForge",
            "Captures visible desktop pixels and applies low-vision enhancements in a separate compatibility window.",
            new Version(0, 1, 0),
            ModuleCapability.ScreenCapture | ModuleCapability.LocalStorage,
            EnabledByDefault: true),

        new(
            "sound.direction-wheel",
            "SoundForge Direction Wheel",
            "SoundForge",
            "Analyzes audible system-output direction and renders customizable visual, clock-face, and spatial cues.",
            new Version(0, 1, 0),
            ModuleCapability.AudioCapture | ModuleCapability.SpeechOutput | ModuleCapability.LocalStorage),

        new(
            "guide.assistant",
            "GuideForge Assistant",
            "GuideForge",
            "Provides voice or typed game help using approved web sources, cached guides, OCR context, and spoiler controls.",
            new Version(0, 1, 0),
            ModuleCapability.WebAccess | ModuleCapability.LocalStorage | ModuleCapability.SpeechOutput | ModuleCapability.Microphone),

        new(
            "profile.manager",
            "ProfileForge",
            "ProfileForge",
            "Loads, versions, imports, exports, and rolls back personal and per-game accessibility profiles.",
            new Version(0, 1, 0),
            ModuleCapability.LocalStorage,
            EnabledByDefault: true),

        new(
            "replay.rolling-buffer",
            "ReplayForge",
            "ReplayForge",
            "Maintains a bounded local replay buffer for review, learning, bookmarks, and user-initiated evidence clips.",
            new Version(0, 1, 0),
            ModuleCapability.ScreenCapture | ModuleCapability.LocalStorage | ModuleCapability.ClipExport),

        new(
            "report.suspicious-play",
            "ReportForge",
            "ReportForge",
            "Helps users preserve visible evidence, redact private information, and follow official suspicious-play reporting routes.",
            new Version(0, 1, 0),
            ModuleCapability.LocalStorage | ModuleCapability.ClipExport | ModuleCapability.WebAccess | ModuleCapability.ProviderReporting),

        new(
            "ocr.reader",
            "OCR Reader",
            "VisionForge",
            "Reads user-selected screen regions, maintains scan history, and sends text through the selected speech output.",
            new Version(0, 1, 0),
            ModuleCapability.ScreenCapture | ModuleCapability.LocalStorage | ModuleCapability.SpeechOutput),

        new(
            "training.calibration",
            "Accessibility Training",
            "ForgeHub",
            "Teaches clock directions, spatial cues, event styles, speech pacing, and personal visual preferences.",
            new Version(0, 1, 0),
            ModuleCapability.LocalStorage | ModuleCapability.SpeechOutput | ModuleCapability.ControllerInput)
    ];
}
