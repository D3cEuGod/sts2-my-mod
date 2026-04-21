# CHANGELOG

This file tracks human-readable repo changes so live-debug work stays traceable.

## 2026-04-21

### DPS tracking and runtime hooks
- Switched DPS capture onto a Harmony patch of `CombatHistory.DamageReceived(...)`.
- Split responsibilities between runtime combat state tracking and damage-event attribution.
- Improved roster seeding and current-combat tracking for the live STS2 runtime.

### Packaging and release flow
- Added multi-platform packaging flow.
- Added `tools/package_release.sh`.
- Added `README-install.md`.
- Synced packaged mod version to the installed local STS2 version during packaging.
- Kept live deployment DLL-only for now because exported `.pck` was not compatible with the game runtime version.

### Save sync and progression safety
- Added base/modded profile sync with backups.
- Evolved save sync toward bidirectional reconciliation with diff-based writes and sync summaries.
- Added rescue/sync work around progression state after timeline corruption issues.
- Disabled unsafe full-unlock runtime writeback path for now to avoid further `progress.save` corruption.

### Live install debugging
- Verified the real game was loading the installed mod from the app `mods/` directory, not just the repo copy.
- Fixed installed-manifest duplication issues.
- Rebuilt and redeployed the actual loaded DLL during live debugging.
- Repaired UI visibility by validating the overlay render path with an exaggerated diagnostic panel first.

### DPS overlay UI
- Restored the right-side DPS overlay after proving rendering worked.
- Reintroduced three sections:
  - 当前战斗
  - 累计伤害
  - 上一场结算
- Iterated the panel toward a more compact, game-like UI.
- Added a collapse/expand button.
- Tuned the header, typography, and hierarchy after live user feedback.
- Tried a more HUD-like header treatment, then rolled back to the prior header style while increasing readability with larger fonts.

### Repo documentation and workflow
- Updated the repo `AGENTS.md` to reflect the real live-debug workflow instead of a pure discovery-phase starter state.
- Added a repo-level `CHANGELOG.md` and established the convention that substantive repo work should append dated summaries.
- Updated `README.md` to document the real installed-game workflow, DLL-only live deployment preference, and current overlay/save-risk status.
- Updated `notes/decisions.md` with the current active runtime path, deployment assumptions, and debug lessons.
- Updated `notes/known-issues.md` with current packaging, UI, and save/progression risk notes.

### Further UI sizing polish
- Increased the overlay panel size slightly to improve readability in-game.
- Increased header, section, row, and detail font sizes by one step.
- Slightly enlarged the collapse button to match the larger text treatment.
- Rebuilt and redeployed the live DLL to the actual game mod directory.

### Release prep for 1.0.0
- Updated README and install docs to describe the current recommended DLL-only cross-platform release path.
- Switched the packaging script to produce a DLL-only cross-platform zip instead of bundling an untrusted `.pck`.
- Set repo release version fields to `1.0.0`.
- Added a repo `.gitignore` for local build/IDE artifacts.

## Conventions for future entries
- Append new dated sections, do not rewrite old entries unless correcting facts.
- Prefer short bullets describing user-visible or debug-relevant code changes.
- When live-debugging, note whether changes were only in-repo or also deployed to the actual runtime target.
