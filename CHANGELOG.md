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

## 2026-04-22

### Damage validation follow-up
- Excluded self-damage from the main tracked-output path so HP-cost / self-hit events no longer inflate DPT totals.
- Added explicit `[DAMAGE_SKIPPED_SELF]` logging to confirm when a damage event was intentionally ignored for self-damage reasons.
- Corrected weak/vulnerable debug logging to treat `ModifyDamageMultiplicative(...)` as a multiplier hook instead of a final-damage value.
- Updated debug logs to emit `input`, `multiplier`, `estimatedFinal`, and `estimatedDelta` fields, reducing misleading negative-delta spam from the earlier logging format.
- Rebuilt the mod successfully after the logging/self-damage fixes.
- Fixed the local build deployment target so post-build copy now writes to the real macOS game mod directory (`SlayTheSpire2.app/Contents/MacOS/mods`) instead of the stale top-level `$(Sts2Dir)/mods` path.
- Stopped copying `mod_manifest.json` into the live game mod directory because having both runtime manifest files caused duplicate mod discovery and a second-load error.
- Updated repo notes to track the newly confirmed live issues: per-run lifetime totals bleeding across runs, poison/doom-style debuff damage missing from the panel, and weak/vulnerable validation logs still needing noise reduction.
- Reset the tracker on `RunManager.RunStarted` so run-total damage, combat state, and last-combat carryover no longer bleed into the next run.
- Updated the overlay wording from startup-lifetime phrasing to current-run phrasing (`本局累计` / `当前这一局累计总伤害`).
- Delayed combat finalization by one tick so end-of-combat lethal hits are still included before snapshots and run totals are published.
- Tried a `CreatureCmd.Damage(...)` capture path for lethal-hit coverage, but rolled it back after it regressed normal damage tracking. The stable live path is back to `CombatHistory.DamageReceived(...)` while lethal-hit handling is investigated separately.
- Added an isolated lethal-hit fallback: normal damage still records through `CombatHistory.DamageReceived(...)`, while `CreatureCmd.Damage(...)` observers now supplement only missing player-caused lethal hits that never reach combat history. Expanded the observer from one overload to all single-target `CreatureCmd.Damage(...)` overloads used by this build.
- Added `CombatManager.CombatWon` logging of final enemy HP/block/dead state to diagnose the final-enemy kill path without changing the main damage-tracking flow.
- Added a final-victory fallback that caches the last targeted enemy's pre-hit HP/block from `PlayCardAction.ExecuteAction` and, if combat is won with no matching damage history record, infers the missing final-hit contribution as `preHp + preBlock`.
- Began isolated debuff/HP-loss support without touching the stable main damage path: cache owner/target context from `Hook.ModifyHpLostAfterOsty(...)`, then, when `Creature.LoseHpInternal(...)` resolves with no matching combat-history hit, attribute that HP-loss result through a separate fallback path.
- Immediately rolled back that first HP-loss fallback attempt after live testing showed it could break Neow reward flow with `Hook.AfterModifyingHpLostAfterOsty(...)` null-source failures during non-combat max-HP loss handling. Normal combat and the previously fixed final-hit behavior remain the stable baseline.
- Replaced the broad HP-loss experiment with narrower combat-only debuff hooks on `PoisonPower.AfterSideTurnStart(...)` and `DoomPower.DoomKill(...)`, so poison / doom fallback attribution stays inside combat-specific power code paths instead of touching generic non-combat HP-loss flow.
- Hardened combat-history capture for combat-only debuff testing after live logs showed poison can reach `CombatHistory.DamageReceived(...)` with a null `dealer`; the tracker now tolerates null dealers instead of throwing and freezing the enemy turn.
- Hardened the poison-only fallback again after live logs showed `PoisonPower.CalculateTotalDamageNextTurn()` can throw during lethal poison resolution. The fallback now reads the poison amount from internal power fields instead of calling the unstable getter during the combat-end edge case.
- Updated tracked-damage semantics to use the game's `DamageResult.TotalDamage` directly instead of subtracting `OverkillDamage` a second time. User testing showed the runtime total being reported here already reflects the real dealt damage they expect to see on the panel.
- Fixed poison double-counting by limiting the poison fallback to `CombatSide.Enemy` in `PoisonPower.AfterSideTurnStart(...)`, so it only records once at the player-turn-end boundary instead of again at the next player-turn start.
- Simplified poison fallback timing again so it now records synchronously at the player-turn-end / enemy-side-start boundary instead of waiting on the async power task. This matches the desired panel timing and avoids poison-kill edge cases caused by async completion ordering.
- Added a combat-local poison-source cache so poison kills can still be attributed when the final `PoisonPower` tick no longer exposes a live applier on the lethal turn boundary.
- Reworked poison fallback to wait for `PoisonPower.AfterSideTurnStart(...)` completion, then only infer damage if no matching `CombatHistory` entry appeared for that exact tick, avoiding poison double-counting while still covering missing lethal/mid-combat poison entries.
- Relaxed poison history dedupe to match by poisoned target instead of requiring a non-null dealer, and seed the poison-owner cache from poison card plays so normal poison ticks still resolve an owner when `PoisonPower` arrives without a live applier.
- If `CombatHistory.DamageReceived(...)` arrives for a poison-style null-card tick with `dealer == null`, it now reuses the cached poison source before recording DPS, so ordinary poison ticks can count on the main history path instead of being skipped as ownerless.
- Added temporary poison diagnostics around `PoisonPower.AfterSideTurnStart(...)`, poison-source cache updates, and null-card `CombatHistory.DamageReceived(...)` so the missing ordinary poison-tick path could be traced exactly instead of guessing.
- Seed poison-source cache from direct poison-card `CombatHistory` hits as well, so the first ordinary poison tick on a target can resolve a cached owner even when the poison tick's own history arrives before the power callback updates cache.
- Removed the temporary poison diagnostic log spam after confirming poison tick attribution is stable again.
- Hardened doom fallback attribution so `DoomPower.DoomKill(...)` no longer dies when power-applier reflection is unavailable, and now falls back to a recent doom-card dealer cache or the current single-player combat owner when the live doom power instance does not expose an applier.
- Wrote repo-maintenance summaries into `AGENTS.md` and `notes/decisions.md` describing the stable poison and doom repair strategy, so future updates can reuse the proven fix path instead of rediscovering it.
- Updated workspace `USER.md` and `SOUL.md` so successful bug fixes and completed features should be followed by a short repo-local maintenance summary in that repo's memo/docs.
- Updated repo issue/backlog notes so poison and doom debuff attribution are marked as fixed current behavior instead of remaining active blockers.
- Reduced weak/vulnerable validation log spam by turning `WEAK_MOD` / `VULN_MOD` into short-window deduped probes instead of logging every repeated identical multiplier evaluation.
- Applied the same short-window dedupe pattern to `THORNS_PRE`, `REFLECT_POST`, and `OSTY_SUMMON` so debug validation stays available without flooding combat logs.

## 2026-04-23

### Release prep for 1.1.1
- Set repo release version fields to `1.1.1`.
- Fixed optional ModConfig registration by calling `ModConfigBridge.DeferredRegister()` during initialization.
- Updated release docs to reflect the `1.1.1` DLL-only package name and current install expectations.
- Kept the recommended public release shape as DLL-only, because the repo's last verified stable runtime path still avoids shipping an exported `.pck`.

### Overlay polish and stats accuracy follow-up
- Fixed the panel visibility setting so `showPanel` now actually reloads from ModConfig instead of always forcing the overlay visible.
- Made the overlay respond to the configured visible-row count and grow its panel height with that setting instead of silently ignoring it.
- Tightened lifetime / last-combat rows and summaries so they show real damage dealers only, avoiding misleading zero-damage roster entries in those views.
- Added per-run combat history retention in the tracker so the UI can browse older completed fights instead of only keeping one previous-combat snapshot.
- Turned the last-combat header into a clickable entry point that opens a dedicated in-panel combat-history view for earlier fights in the current run.
- Added per-player and per-combat highest-single-hit tracking so the history view can show stronger summary stats without touching the stable damage-capture mainline.
- Upgraded the history view with expandable combat cards, better scrollable presentation, and simple paging for longer runs.
- Highlighted the top-damage champion for each historical combat card so standout runs are easier to scan at a glance.
- Brought the current-combat panel into the same visual language with champion highlighting plus highest-single-hit detail.
- Increased overall panel sizing and font sizing slightly for readability in the live UI.
- Replaced the earlier broad content-driven expanded-height experiment with a narrower rows-driven main-panel height rule, so the main HUD follows actual rendered rows instead of either growing from all content or reserving too much fixed empty space.

### Release prep for 1.1.2
- Rolled in the recent overlay/UI fixes and combat-history improvements into version `1.1.2`.
- Centered the title badge correctly by making the title container fill the real header area instead of only centering text inside a narrower region.
- Tightened main-panel height behavior so the main HUD sizes from rows actually rendered, avoiding both giant dynamic-height growth and large fixed empty space.
- Preserved the combat-history enhancements from this pass: champion highlighting, highest single hit display, expandable fight cards, and paged history browsing.
- Updated repo version fields to `1.1.2` and prepared a new DLL-only release package.

## Conventions for future entries
- Append new dated sections, do not rewrite old entries unless correcting facts.
- Prefer short bullets describing user-visible or debug-relevant code changes.
- When live-debugging, note whether changes were only in-repo or also deployed to the actual runtime target.
