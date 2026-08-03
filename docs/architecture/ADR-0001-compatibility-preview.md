# ADR-0001: Compatibility-first preview

- **Status:** Accepted for v0.1
- **Date:** 2026-08-03

## Context

SightForge is intended to improve access for low-vision players across many Windows games. A universal tool must work without depending on individual game engines, private APIs, game memory, packet inspection, input automation, or anti-cheat bypasses.

Transparent overlays and injected rendering hooks can conflict with anti-cheat systems even when their purpose is accessibility. The first implementation therefore needs a conservative operating mode that can be tested independently of any particular game.

## Decision

SightForge v0.1 uses a separate compatibility preview window:

1. Capture pixels already visible on the primary Windows display.
2. Crop around the center when magnification is requested.
3. Downscale to a bounded processing resolution.
4. Apply tone correction, boundary enhancement, and non-classifying frame-difference motion emphasis.
5. Present the processed image inside the SightForge window.

The reference pipeline uses ordinary Windows desktop capture through GDI and CPU processing. This is not the final performance architecture. It establishes correct behavior and an auditable accessibility boundary before a GPU implementation is introduced.

## Explicit non-goals

SightForge will not:

- Read or scan game-process memory.
- Inject DLLs, shaders, drivers, or hooks into games.
- Inspect game network traffic.
- Identify enemies, teammates, loot, or hidden objects.
- Estimate exact in-game distance from private game data.
- Automate aim, firing, movement, or controller input.
- Conceal itself from or interfere with anti-cheat systems.

## Consequences

### Positive

- The behavior is game-agnostic and easy to audit.
- The first prototype can be built using only platform APIs.
- Image-processing algorithms can be tested before GPU optimization.
- A separate window is less invasive than an injected or hooked overlay.

### Negative

- The preview introduces latency and requires the player to view a separate window or monitor.
- Full-screen exclusive games may not be capturable with this reference path.
- GDI capture and CPU processing will not meet the final high-frame-rate performance target.
- General motion emphasis also responds to camera motion, foliage, weather, particles, and animated UI.

## Follow-up decisions

Future ADRs should address:

- Windows Graphics Capture versus Desktop Duplication.
- Direct3D shader processing and texture sharing.
- Capture-card processing on a second computer.
- Directional audio visualization through WASAPI loopback.
- A non-injected, documented overlay mode only after compatibility testing and legal review.
