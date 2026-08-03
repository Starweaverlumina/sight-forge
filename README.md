# SightForge

**Universal low-vision gaming accessibility toolkit for Windows.**

SightForge is an independent accessibility application that captures visible desktop output, processes it in a separate live-feed window, and deliberately avoids game-memory access, target classification, input automation, injection, packet inspection, and anti-cheat bypassing.

## Current prototype

The `agent/compatibility-preview` branch contains a runnable Windows prototype with:

- Primary-display live capture in a separate compatibility window
- Center-focused magnification from 1× to 4×
- Live contrast and gamma adjustment
- Strong-edge enhancement
- General visible-motion emphasis using frame differences
- Adjustable motion sensitivity
- High-visibility center guide
- Local JSON profile persistence
- Guarded asynchronous frame processing and FPS reporting
- A configurable 5–120 second in-memory rolling replay
- Instant playback and replay-memory clearing
- Safe adaptive tuning based on scene luminance, contrast spread, and overall motion noise

Adaptive tuning does not train an enemy detector. It gradually adjusts gamma, contrast, and motion-noise thresholds using statistics from visible frames. The rolling replay provides recent visual context for review without accessing the game process.

## Requirements

- Windows 10 version 2004 or newer, or Windows 11
- .NET 8 SDK to build from source
- A 64-bit Windows computer

## Build and run

```powershell
git clone https://github.com/Starweaverlumina/sight-forge.git
cd sight-forge
git switch agent/compatibility-preview
dotnet run --project src/SightForge/SightForge.csproj
```

Use borderless-windowed mode for initial testing. Start SightForge, press **Start Live Feed**, and place its window on a second monitor when possible. Enable rolling replay to retain recent processed frames in memory, then use **Play Rolling Replay** to review them.

## Data behavior

- Replay frames are kept in memory only in this prototype.
- Closing SightForge or clearing replay memory removes those frames.
- Profile settings are saved locally as JSON.
- No frames are uploaded or transmitted by SightForge.
- No game executable, process memory, network traffic, or controller input is inspected.

## Important limitations

- GDI capture and CPU processing are reference implementations, not the final low-latency architecture.
- Full-screen exclusive applications may not capture correctly.
- Motion emphasis reacts to qualifying pixel changes including camera motion, weather, particles, foliage, animation, and UI transitions.
- Magnification currently focuses on the center of the primary display.
- SightForge cannot know whether a moving shape is an enemy or determine exact in-game distance.
- Some games restrict third-party capture or overlay tools. Players remain responsible for each game's rules.
- Long replay windows use substantial RAM because frames are currently stored uncompressed.

## Planned accessibility features

- Windows Graphics Capture or Desktop Duplication
- GPU processing through Direct3D shaders
- Compressed replay storage and optional user-initiated clip export
- Picture-in-picture and movable magnification regions
- Saturation, color remapping, sharpening, and shadow controls
- Directional audio visualization
- Controller-first global controls
- Per-game automatic profile selection
- Capture-card mode for a fully separate processing computer

## Safety boundary

SightForge enhances the visible image for accessibility. It must not:

- Determine whether a visible figure is an enemy or teammate
- Reveal players or objects hidden behind geometry
- Read or scan game-process memory
- Inspect network packets
- Inject code, shaders, drivers, or hooks into a game
- Automate aiming, firing, movement, or other input
- Conceal itself from or interfere with anti-cheat protections

See [`docs/architecture/ADR-0001-compatibility-preview.md`](docs/architecture/ADR-0001-compatibility-preview.md).

## Project status

Early functional prototype. The active research and implementation plan is tracked in GitHub issues and draft pull request #2.
