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

### Post-release stats refresh fix
- Corrected the earlier over-fix to restore live in-combat accumulation for the current-combat panel.
- Current combat now shows continuously accumulating damage during a fight, but still resets only on combat boundaries instead of per-turn.
- Lifetime totals stay visible and include ongoing combat damage while a fight is in progress.
- Last-combat summary still updates on combat settlement.
- Removed the fallback "no damage for 8 seconds means combat ended" heuristic after it refreshed too early in real fights.
- Combat settlement now trusts the game's real `CombatEnded` event instead of a damage-gap timeout.
- Added draggable panel behavior, so the overlay can be moved by dragging the title area.
- Kept collapse/expand behavior intact while making the default spawn position resolve from the current viewport.
- Switched the main per-player metric from DPS to true DPT using the runtime `CombatState.RoundNumber` value.
- Removed on-panel timing details so the UI no longer shows seconds-based status text.
- Rebuilt and redeployed the live DLL to the actual game mod directory.

### Release prep for 1.0.0
- Updated README and install docs to describe the current recommended DLL-only cross-platform release path.
- Switched the packaging script to produce a DLL-only cross-platform zip instead of bundling an untrusted `.pck`.
- Set repo release version fields to `1.0.0`.
- Added a repo `.gitignore` for local build/IDE artifacts.

### Release prep for 1.0.1
- Rolled the latest post-1.0.0 fixes and polish into a new release baseline.
- Integrated the draggable overlay panel behavior into the release package.
- Integrated the corrected combat-end handling so fights no longer settle early from a damage-gap timeout.
- Kept current-combat damage accumulating live during a fight while preserving combat-boundary resets.
- Switched the main displayed metric from DPS to true DPT using `CombatState.RoundNumber`.
- Removed seconds-based UI timing text from the panel.
- Fixed the last-combat panel so a newly started fight can correctly show the immediately previous completed combat.
- Fixed `F9` reset so it clears lifetime damage totals as well as current/last-combat display state.
- Updated repo docs and manifest descriptions to reflect the DPT-focused behavior.
- Set repo release version fields to `1.0.1`.

### Release prep for 1.0.2
- Rolled in the fix for the last-combat panel data source.
- Rolled in the fix for `F9` so full stats reset clears lifetime totals too.
- Set repo release version fields to `1.0.2`.

### Follow-up investigation notes
- Added backlog items for vulnerable/weak damage-contribution tracking and non-attack damage classification.
- Confirmed by local reflection that `WeakPower` and `VulnerablePower` both expose `ModifyDamageMultiplicative(...)` hooks.
- Confirmed several likely non-attack damage paths worth future classification, including `ThornsPower`, `ReflectPower`, `OstyCmd`, and HP-loss related hooks.
- Added a log-validation build that now logs final damage events plus weak/vulnerable/thorns/reflect/osty-related debug signals during combat.

## Conventions for future entries
- Append new dated sections, do not rewrite old entries unless correcting facts.
- Prefer short bullets describing user-visible or debug-relevant code changes.
- When live-debugging, note whether changes were only in-repo or also deployed to the actual runtime target.
