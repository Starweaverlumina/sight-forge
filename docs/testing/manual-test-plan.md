# SightForge v0.1 manual test plan

## Test environment record

Record before each session:

- Windows version and build
- CPU, GPU, and installed memory
- Display count, resolution, refresh rate, and scaling percentage
- Game or application name
- Window mode: windowed, borderless, or exclusive fullscreen
- SightForge preview FPS
- Whether the preview is on the same display or a second display

## Startup and shutdown

1. Launch SightForge without administrator privileges.
2. Confirm the window opens with Compatibility Mode displayed.
3. Press **Start Compatibility Preview**.
4. Confirm the primary display appears in the preview.
5. Press **Stop Compatibility Preview**.
6. Confirm capture stops and the application remains responsive.
7. Close and reopen SightForge.
8. Confirm the previous visual settings are restored.

## Visual controls

### Magnification

- Test 1.0×, 1.5×, 2.0×, and 4.0×.
- Confirm magnification remains centered.
- Confirm the image fills the preview without tearing or an invalid crop.

### Contrast and gamma

- Test minimum, default, and maximum values.
- Confirm adjustments occur while capture is running.
- Confirm no channel wraps from bright to dark or dark to bright.

### Edge enhancement

- Test on text, menus, indoor geometry, outdoor foliage, and moving scenes.
- Confirm boundaries become more visible.
- Record cases where texture noise becomes distracting.

### Motion emphasis

- Test with a stationary camera and one moving object.
- Test camera rotation with a static environment.
- Test particles, weather, water, grass, and animated HUD elements.
- Confirm all qualifying motion is treated equally and no target classification occurs.
- Record a useful threshold range for each game.

### Center guide

- Confirm the guide can be toggled while capture is active.
- Confirm it remains centered when the window is resized.
- Confirm it does not become part of the captured source image unless the SightForge window itself is visible on the captured display.

## Compatibility cases

Test each application in:

- Normal windowed mode
- Borderless windowed mode
- Exclusive fullscreen, where available
- HDR off and HDR on, where available
- 100%, 125%, 150%, and 200% Windows display scaling, where practical

Do not attempt to bypass an application's capture restrictions or anti-cheat behavior. Stop testing if the game warns about, blocks, or penalizes the tool.

## Performance checks

- Observe preview FPS for five minutes.
- Watch for steadily increasing memory use.
- Confirm the UI remains responsive while frames are processing.
- Confirm skipped timer ticks do not create an unbounded processing queue.
- Record approximate input-to-preview latency using a high-speed camera when available.

## Accessibility session

The low-vision player should compare:

- Baseline game view
- Magnification only
- Tone controls only
- Edge enhancement only
- Motion emphasis only
- Their preferred combined profile

Record comfort, readability, visual fatigue, false emphasis, motion sickness, and whether the preview meaningfully improves independent play.

## Failure reporting

Include:

- Exact reproduction steps
- Expected and observed behavior
- Screenshot or short recording when safe to share
- Windows and hardware details
- SightForge settings
- Whether restarting capture or the application resolved the issue
