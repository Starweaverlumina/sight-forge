# SightForge Platform Roadmap

SightForge is a modular, privacy-conscious accessibility platform for games. It improves access to information already visible or audible to the player without reading game memory, injecting into games, automating input, revealing hidden information, or bypassing anti-cheat systems.

## Product pillars

### VisionForge
- Live compatibility feed
- Magnification and movable focus regions
- Contrast, gamma, shadow lift, sharpening, and edge enhancement
- General motion emphasis
- Color remapping and color-blind presets
- Custom crosshair, cursor, and screen guides
- OCR region capture, history, and text-to-speech
- Rolling replay and frame stepping

### SoundForge
- WASAPI loopback capture
- Stereo-first direction analysis with future 5.1/7.1 support
- Fortnite-style direction wheel
- Clock-face callouts
- Binaural cues and customizable 3x3 cue map
- Mono-listening mode after direction analysis
- Custom event colors, shapes, pulses, tones, and priorities
- Hearing calibration and speech-preservation controls

### GuideForge
- Voice and typed assistant
- Spoiler-aware walkthroughs, quest help, puzzle hints, crafting lookup, menu help, and control explanations
- Approved-source web research with citations and local caching
- Per-game terminology and accessibility knowledge packs
- On-demand, critical-only, normal, and verbose assistance levels
- Priority speech scheduler with interruption rules

### ProfileForge
- Per-game profiles and automatic profile selection
- Personal calibration profiles
- Import/export, rollback, and backups
- Reviewed community profile packs
- Source provenance, game version, profile version, and trust state

### ReplayForge
- Bounded rolling replay
- User markers and bookmarks
- Frame stepping and accessibility review
- Optional user-initiated clip export
- Strict memory, storage, and retention budgets

### ReportForge
- Mark suspicious moments
- Preserve bounded evidence clips
- Generate neutral, human-reviewed incident summaries
- Redact unrelated names, chat, and private information
- Display official provider reporting routes
- Require explicit confirmation before any submission
- Never declare guilt, mass report, publish accusations, or create blacklists

### ForgeHub
- Permissioned module and plugin system
- Privacy dashboard and visible capture indicators
- Training and calibration mode
- Diagnostics, performance budgets, safe mode, and portable mode
- Accessibility report generator for game developers
- Moderated sharing of profiles, themes, cue packs, OCR layouts, and knowledge packs

## Delivery sequence

### Milestone 0.1 — Compatibility foundation
- [x] Separate live preview
- [x] Magnification and tone controls
- [x] Edge and general motion enhancement
- [x] Rolling replay
- [x] Safe scene adaptation
- [x] Profile persistence
- [x] Initial directional cue engine
- [x] Cue-style schema and editor foundation
- [x] Bounded game-research service foundation

### Milestone 0.2 — Platform core
- [x] Define module capabilities and lifecycle contracts
- [x] Define module catalog and permission state
- [ ] Add module dashboard to the main application
- [ ] Add persistent permission decisions
- [ ] Add privacy activity log
- [ ] Add assistance-priority scheduler
- [ ] Add shared event bus with bounded queues

### Milestone 0.3 — SoundForge
- [ ] WASAPI loopback frame source
- [ ] Direction analysis before mono downmix
- [ ] Direction wheel renderer
- [ ] Clock-face speech callouts
- [ ] User calibration and training mode
- [ ] Stereo benchmark and 5.1/7.1 architecture

### Milestone 0.4 — GuideForge and OCR
- [ ] Secure API-key storage using Windows-protected credentials
- [ ] Source display and citation panel
- [ ] Voice push-to-talk and wake-word-off-by-default controls
- [ ] OCR region editor, freeze-frame setup, and scan history
- [ ] Screen-reader output abstraction
- [ ] Spoiler and verbosity controls

### Milestone 0.5 — Trusted game packs
- [ ] Game detection with user confirmation
- [ ] Signed package schema
- [ ] Provenance, versioning, review, rollback, and compatibility metadata
- [ ] Community import/export with permission review

### Milestone 0.6 — ReportForge
- [ ] Suspicious-moment marker
- [ ] Evidence manifest and hashes
- [ ] Clip preview and redaction
- [ ] Neutral report generator
- [ ] Provider reporting-guide registry
- [ ] Human confirmation gate

### Milestone 1.0 — Public accessibility beta
- [ ] Disabled-gamer usability testing
- [ ] Installer and portable release
- [ ] Performance and privacy audit
- [ ] Accessibility conformance review
- [ ] Provider compatibility documentation

## Non-negotiable boundaries

SightForge does not identify hidden players, reveal hidden objects, automate aim or input, read game memory, inspect packets, inject code, install kernel anti-cheat components, evade anti-cheat, automatically accuse players, or submit bulk reports.
