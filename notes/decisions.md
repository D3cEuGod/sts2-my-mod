# decisions

## repository state
- The repo contains legacy StS1 reference material under `docs/reference-sts1/` and `examples/reference-sts1/`.
- StS1 reference material must not be treated as evidence for StS2 behavior.
- The repo contains one StS2-specific example under `examples/sts2/ModConfig-sts2/`.
- StS2 evidence tracking lives in `docs/sts2/evidence-checklist.md`.
- The repo now also contains a repo-level `CHANGELOG.md` and should keep it updated for substantive work.

## confirmed
- [x] Legacy StS1 docs/examples are separated into reference-only paths.
- [x] A StS2-specific example repo is present at `examples/sts2/ModConfig-sts2/`.
- [x] The current prototype follows the StS2 example pattern: C# + `Godot.NET.Sdk/4.5.1` + `MegaCrit.Sts2.Core.Modding` + `ModInitializer`.
- [x] The prototype entrypoint is `MainFile.cs` with `[ModInitializer(nameof(Initialize))]` and `public static void Initialize()`.
- [x] The prototype uses a code-built overlay panel instead of a Godot scene file.
- [x] The prototype isolates external/demo damage injection behind `Scripts/DamageEventBridge.cs`.
- [x] The active DPS path uses Harmony patching of `CombatHistory.DamageReceived(...)` plus `CombatManager` for combat boundaries.
- [x] The tracker now keeps three user-facing views: current combat, lifetime damage for this launch, and last-combat summary.
- [x] The overlay now has a compact right-side panel with a collapse/expand control.
- [x] The real game on local macOS loads the installed mod from `SlayTheSpire2.app/Contents/MacOS/mods`.
- [x] Current live debugging works most reliably as DLL-only deployment.

## likely
- [ ] Community knowledge is fragmented across Discord.
- [ ] Real damage capture may still have edge cases not yet covered by `CombatHistory.DamageReceived(...)`.
- [ ] A future shippable package may still want `.pck`, but the current live-debug path should not depend on it.

## unknown
- [ ] Whether `CombatHistory.DamageReceived(...)` is the final correct source for all DPS-relevant damage in StS2.
- [ ] The correct stable source for multiplayer player display name, even though reflected platform APIs suggest `SteamPlatformUtilStrategy.GetPlayerName(UInt64)` may exist.
- [ ] Whether `CombatSetUp` / `CombatEnded` are the right final boundaries for every encounter type.
- [ ] Whether the current save sync / rescue behavior is fully safe across all progression states.
- [ ] When the `.pck` path can safely be considered runtime-compatible again.

## blockers / risk areas
- No local StS2 docs fully confirm the complete packaging/install/debug workflow.
- No local StS2 docs fully confirm the complete save/progression mutation contract.
- Save/progression repair is risky because runtime writeback can corrupt `progress.save`.
- Steam Cloud can restore broken modded saves after local repair.

## prototype decisions
- Start from the smallest workable mod pattern, but prefer real runtime evidence over static example assumptions once the project is live-debugging.
- Keep the panel on the right side and optimize for low obstruction rather than dramatic UI.
- Keep demo hotkeys (`F7`/`F8`/`F9`) for local validation.
- Do not use the earlier `AbstractModel` combat-hook experiment as the active path for this prototype.
- Use runtime subscription via `CombatManager.CombatSetUp` and `CombatManager.CombatEnded` for combat boundaries.
- Read real damage by Harmony-patching `CombatHistory.DamageReceived(...)` against the real local `sts2.dll` signature.
- Keep `CombatHistory.DamageReceived(...)` as the stable normal-damage mainline. Do not replace it with broad HP-loss or generic lethal fallback paths just to cover poison/doom edge cases.
- Use `player.NetId` as the grouping key, seed the full roster at combat start, and attribute pet damage to `PetOwner` to avoid inflated player counts.
- Format multiplayer display names with stable slot prefixes until a better official player-facing name source is confirmed.
- Keep end-of-combat results visible, and clear them automatically only when the next combat starts.
- Treat ModConfig integration as optional via reflection, matching the stored StS2 example.
- Treat save/progression features as high-risk and prefer file-level verification plus backups over unchecked runtime save writes.
- If the overlay disappears, prove the render chain first with an exaggerated diagnostic panel before debugging subtle layout issues.
- During live debugging, always distinguish between repo changes and what has actually been deployed to the installed game.
- Keep UI polish additive and isolated from the stable damage-capture path. Small panel/layout improvements should not require touching the main Harmony damage hooks.
- ModConfig-backed overlay settings should always round-trip through `PrototypeSettings.Load()` correctly; if a setting is exposed in ModConfig, do not silently hardcode over it at load time.
- Lifetime and last-combat summary views should prefer real damage-dealer rows only; seeded zero-damage roster entries are acceptable in the live combat view but misleading in summary panels.
- Keep per-run combat-history browsing in tracker/UI state only. Earlier completed fights can be retained for viewing, but this should stay separate from the stable live-combat damage mainline.
- Additional per-combat stats like highest single hit should be derived from the same already-accepted damage events, not from a second parallel capture path.
- Overlay panel height should follow visible content for the active view when possible; fixed-height empty space makes the HUD feel less trustworthy during sparse states between combats.
- Poison was stabilized by an additive combat-local design: `PoisonPower.AfterSideTurnStart(...)` observation, poison-source cache per target, first-tick cache seeding from poison-card history hits, and cached-owner recovery for null-dealer poison history entries.
- Doom support must stay isolated on `DoomPower.DoomKill(...)` fallback attribution. Its applier lookup must be defensive, and when the live doom power instance does not expose an applier, fallback attribution may use recent doom-source cache or the current single-player combat owner.
- After successful bug fixes or completed features, write a short maintenance summary into repo memo docs so later debugging can reuse the real fix instead of rediscovering it.
