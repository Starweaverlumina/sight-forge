# SightForge Checkpoint 0002 — SoundForge hard-coding

Date: 2026-08-03
Branch: `agent/compatibility-preview`

## Completed in this pass

- Fixed WPF / Windows Forms namespace collisions through explicit global aliases.
- Added NAudio 2.2.1 as the Windows audio capture dependency.
- Implemented `WasapiLoopbackDirectionService` using Windows WASAPI loopback capture.
- Added stereo RMS level analysis and left/center/right direction classification.
- Added confidence, balance, and analog clock callouts (9/10/12/2/3 o'clock for stereo).
- Added configurable audible threshold, direction dead zone, analysis interval, and event rate limiting.
- Added SoundForge data models and settings.
- Added deterministic xUnit tests for left, right, center, and unsupported/silent input.
- Updated Windows CI to restore, build, test, publish, and upload the application artifact.
- Implemented `DirectionWheelControl`, a reusable WPF clock-face renderer accepting audio and visual-motion direction updates.

## Current boundaries

- Stereo audio can estimate left/center/right but cannot reliably distinguish front from rear.
- True front/rear/height requires multichannel capture or spatial-audio metadata where the platform exposes it.
- No sound-event classifier is wired yet; the wheel currently accepts generic direction snapshots.
- The wheel control and WASAPI service still need to be connected to the main window.
- Mono-listening-safe playback is modeled but not yet routed to an output device.

## Next hard-coding sequence

1. Wire `WasapiLoopbackDirectionService` lifecycle into the main application.
2. Place `DirectionWheelControl` in the main UI and update it on the WPF dispatcher.
3. Add start/stop, threshold, dead-zone, and clock-callout controls.
4. Implement optional mono-safe monitor output without creating feedback loops.
5. Add event classifier interfaces and a local, user-disabled-by-default classifier host.
6. Connect event cue styles to wheel colors, shapes, pulse patterns, opacity, and thickness.
7. Wire the cue appearance editor into the main control panel.
8. Add privacy and permission status indicators for screen, audio, microphone, web, and stored replay.
9. Add the module dashboard and assistance priority controls.
10. Run Windows hardware tests with headphones and multiple audio channel layouts.

## Safety boundary

SightForge analyzes only audio already delivered to the user's output device. It does not inspect game memory, network packets, hidden spatial data, anti-cheat internals, or other players' computers.
