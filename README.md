# SightForge

**Universal low-vision and blind gaming accessibility toolkit for Windows.**

SightForge is an independent accessibility application that captures visible desktop output and audible Windows output without reading game memory, injecting into games, automating input, inspecting packets, or bypassing anti-cheat systems.

## Testable beta features

### Accessibility Hub

SightForge launches into a large-control, keyboard-first hub with spoken focus. The hub opens VisionForge, SoundForge, local screen reading, GuideForge, profiles, and setup/training.

Global shortcuts while SightForge is running:

- `Ctrl+Shift+1` — VisionForge
- `Ctrl+Shift+2` — SoundForge
- `Ctrl+Shift+3` — Read the screen
- `Ctrl+Shift+4` — GuideForge
- `Ctrl+Shift+5` — Profiles
- `Ctrl+Shift+6` — Setup and training
- `F1` — spoken shortcut help in the hub
- `F2` — toggle spoken focus in the hub
- `F5` — scan the screen in the OCR reader

### VisionForge

- Primary-display live capture in a separate compatibility window
- Center magnification from 1× to 4×
- Contrast and gamma adjustment
- Strong-edge enhancement
- General visible-motion emphasis
- High-visibility center guide
- Safe scene-adaptive tuning
- Configurable in-memory rolling replay
- Local JSON settings persistence

### SoundForge

- Windows WASAPI loopback capture
- Left, center, and right stereo-direction estimation
- Clock-face direction wheel
- Optional spoken clock callouts
- Confidence filtering and rate limiting
- Custom 3×3 spatial cue pitches
- High-contrast and color-blind-safe cue styles

Stereo cannot reliably distinguish front from rear. True front/rear/height direction requires multichannel audio or spatial metadata exposed by the platform.

### Local screen reader

- Captures the visible primary display
- Uses Windows OCR locally
- Displays recognized text in a large high-contrast view
- Reads recognized text aloud
- Does not upload screenshots by default

### GuideForge

- User-initiated game research and walkthrough help
- Spoiler-aware question mode
- Local response cache
- Provider-independent research abstraction
- Session-only API-key field that is cleared after each request

### ProfileForge

- Versioned accessibility profile packs
- Vision, SoundForge, narration, OCR-region, source, and trust metadata
- Local save/list support
- Import/export-ready storage service
- High-contrast and spoken-direction preferences

### Setup and training

- Starting magnification selection
- Edge and motion preference selection
- Color-blind-safe theme selection
- Speech-rate testing
- Clock-callout and confidence preference selection
- Keyboard, controller-first, or voice-first preference recording

## Build and run

Requirements:

- Windows 10 version 2004 or newer, or Windows 11
- .NET 8 Desktop Runtime to run the framework-dependent build
- .NET 8 SDK to build from source

```powershell
git clone https://github.com/Starweaverlumina/sight-forge.git
cd sight-forge
git switch agent/compatibility-preview
dotnet run --project src/SightForge/SightForge.csproj
```

For initial game testing, use borderless-windowed mode and place SightForge on a second monitor when possible.

## Data behavior

- Replay frames remain in local memory in this prototype.
- Closing SightForge or clearing replay removes those frames.
- Accessibility profiles and cached guide answers are stored locally.
- GuideForge API keys are not written to profiles or cache files.
- SightForge does not scan game processes, drivers, memory, network traffic, peripherals, or other players' computers.

## Safety boundary

SightForge may enhance information already visible or audible to the player. It must not:

- Reveal players or objects hidden behind geometry
- Identify enemies from internal game state
- Read or scan game-process memory
- Inspect network packets
- Automate aiming, movement, firing, or other gameplay input
- Inject code or DLLs into games
- Evade anti-cheat systems
- Automatically accuse players, create public blacklists, or submit bulk reports

## Before public release

This branch is a testable beta candidate, not yet a medically validated assistive device or production release. It still requires:

- Real Windows hardware testing across GPUs, displays, headphones, and audio devices
- Testing with the intended low-vision user
- Usability testing with additional blind, low-vision, deaf, hard-of-hearing, and colorblind players
- Per-game compatibility testing and provider policy review
- Installer, code-signing certificate, update channel, crash reporting choice, and privacy review
- Performance profiling and GPU capture/processing work

User testing is essential: cue frequency, speech density, OCR accuracy, contrast, motion sensitivity, and cognitive load are personal and cannot be finalized from automated tests alone.
