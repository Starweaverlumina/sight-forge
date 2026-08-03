# SightForge

**Universal low-vision gaming accessibility toolkit for Windows.**

SightForge is an accessibility-first desktop application designed to help low-vision players see and interpret game screens more clearly. It operates on visible screen output and deliberately avoids game-memory access, input automation, enemy classification, DLL injection, packet inspection, and anti-cheat bypassing.

## Planned accessibility features

- Center and picture-in-picture magnification
- Contrast, gamma, saturation, and sharpening controls
- Edge enhancement
- General motion emphasis without identifying targets
- Large high-visibility crosshair and screen guides
- Directional audio visualization
- Per-game profiles
- Compatibility-window and capture-card modes

## Initial technology

- .NET 8
- Windows Presentation Foundation (WPF)
- Windows Graphics Capture planned for low-latency capture
- GPU processing planned through Direct3D shaders
- JSON profile storage

## Safety boundary

SightForge enhances the visible image for accessibility. It must not determine whether a visible figure is an enemy, reveal hidden players, read game memory, automate aiming or input, or evade anti-cheat protections.

## Status

Early prototype and architecture stage. See the project issues for the active study and implementation plan.
