# ADR-0002: Bounded game research assistant

- **Status:** Accepted for prototype
- **Date:** 2026-08-03

## Context

SightForge needs current, game-specific information for voice help, walkthroughs, menu explanations, control guidance, accessibility recommendations, and patch-aware answers. Static bundled knowledge becomes stale and cannot cover every game.

Unrestricted autonomous browsing would create privacy, security, reliability, copyright, and gameplay-fairness risks. Research must therefore be user-controlled, source-backed, and separated from the live visual-processing path.

## Decision

SightForge will expose a provider-independent `IGameResearchProvider` behind `GameResearchService`.

The research workflow is:

1. The user enables web research explicitly.
2. SightForge resolves or asks for the game title and platform.
3. The user asks a question by voice or text.
4. The provider searches official documentation and approved guide sources.
5. SightForge returns a short answer with source titles and links.
6. Suggested accessibility settings are presented for user approval.
7. Results are cached locally for a bounded period.

Research never runs inside the frame-processing loop and cannot directly control the game.

## Source policy

- Prefer official developer, publisher, platform, and accessibility documentation.
- Allow reputable community guides only when official material is insufficient.
- Require HTTPS.
- Require user approval before using a previously unapproved domain.
- Display the source and retrieval time with every answer.
- Distinguish verified facts from inference or community advice.

## Adaptation policy

Web research may recommend:

- Magnification and contrast profiles.
- Color-blind presets.
- Subtitle and HUD settings available in the game.
- Controller remapping suggestions.
- Menu navigation instructions.
- Quest, puzzle, crafting, and onboarding help.
- Game-specific terminology for voice commands.

Web research may not enable:

- Hidden-player or hidden-loot discovery.
- Enemy classification from private game data.
- Aim, firing, movement, or input automation.
- Anti-cheat avoidance.
- Exploit instructions for competitive advantage.
- Automatic application of gameplay-affecting settings without user approval.

## Privacy and credentials

- API keys must be stored using Windows Credential Manager or another OS-protected secret store, never in profile JSON or the repository.
- Queries should contain only the game title, platform, the user's question, and necessary accessibility context.
- Screenshots, replay frames, microphone recordings, account names, and personal data are not uploaded by default.
- Any future screenshot-assisted research requires a separate explicit consent control.

## Failure behavior

When offline, rate-limited, or lacking credentials, SightForge should use cached research and clearly display its age. It must never fabricate a source or present stale advice as current.

## Implementation stages

1. Provider interface, policy model, validation, and local cache.
2. Credential-manager integration and settings UI.
3. Source extraction and citation rendering.
4. Voice question routing and text-to-speech answers.
5. Active-game resolution using user confirmation.
6. Accessibility-profile recommendations requiring explicit approval.
7. Optional local guide packs for offline use.
