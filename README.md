# SightForge

**Universal low-vision gaming accessibility toolkit for Windows.**

SightForge is an accessibility-first desktop application designed to help low-vision players see and interpret game screens more clearly. It operates on visible screen output and deliberately avoids game-memory access, input automation, enemy classification, DLL injection, packet inspection, and anti-cheat bypassing.

## Current prototype

The `agent/compatibility-preview` branch contains the first runnable Windows prototype:

- Primary-display capture in a separate compatibility window
- Center-focused magnification from 1× to 4×
- Live contrast and gamma adjustment
- Strong-edge enhancement
- General visible-motion emphasis using frame differences
- Adjustable motion sensitivity
- High-visibility center guide
- Local JSON profile persistence
- FPS display and guarded asynchronous frame processing

This is a reference implementation. It intentionally favors auditable behavior over maximum frame rate. GPU capture and shader processing come after the visual behavior and safety boundary are validated.

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

Use a borderless-windowed game for initial testing. Start SightForge, press **Start Compatibility Preview**, and place the SightForge preview on a second monitor when possible.

## Important limitations

- The current GDI reference capture is not intended for high-refresh competitive play.
- Full-screen exclusive applications may not appear correctly.
- Motion emphasis reacts to every qualifying pixel change, including camera movement, weather, particles, foliage, animation, and UI transitions.
- Magnification currently focuses on the center of the primary display.
- SightForge cannot know whether a moving shape is an enemy or determine exact in-game distance.
- Some games prohibit or restrict third-party capture and overlay tools. Players remain responsible for each game's rules.

## Planned accessibility features

- Windows Graphics Capture or Desktop Duplication
- GPU processing through Direct3D shaders
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

See [`docs/architecture/ADR-0001-compatibility-preview.md`](docs/architecture/ADR-0001-compatibility-preview.md) for the first architecture decision.

## Project status

Early functional prototype. The active research and implementation plan is tracked in GitHub issues.
