# known issues

- The repo contains legacy StS1 reference material under `docs/reference-sts1/` and `examples/reference-sts1/`.
- Legacy StS1 material is useful background only and must not be used to infer StS2 APIs, hooks, loader behavior, packaging, or entrypoint behavior.
- StS2 workflow is still only partially confirmed.
- The only StS2-specific example currently stored in the repo is `examples/sts2/ModConfig-sts2/`.
- `docs/sts2/` currently contains evidence-tracking templates, not complete StS2 workflow documentation.
- Public guidance may be incomplete or outdated.
- Discord discussions may mention experiments rather than stable workflows.
- We should not treat random chat messages as final API documentation.

## current runtime limitations

- The current runtime path uses `CombatManager` for combat boundaries and roster seeding, and Harmony-patches `CombatHistory.DamageReceived(...)` for per-hit damage capture.
- This patch path compiles cleanly against the real local `sts2.dll`, but the exact best damage metric is still not fully verified.
- Current damage accounting may still need adjustment, for example around `TotalDamage`, `OverkillDamage`, or `UnblockedDamage` semantics.
- Poison-triggered damage is now confirmed working on the current tracked-output path after adding cached poison-source attribution for null-dealer history entries and first-tick cache seeding.
- Doom-triggered damage is now confirmed working on the current tracked-output path after defensive applier lookup plus fallback attribution from recent doom source or the current single-player combat owner.
- Current run-level lifetime totals can bleed across runs instead of resetting cleanly at the start of a new game, so the “overall” panel is not yet trustworthy as a per-run total.
- Weak / vulnerable / thorns / reflect / osty debug hooks are now throttled as short-window deduped probes, but they should still be treated as validation probes rather than clean event counts.
- Multiplayer grouping uses `player.NetId`, and pet damage is attributed to `PetOwner`.
- Display names are improved by seeding the combat roster up front and formatting multiplayer rows with stable slot prefixes, but we still do not know whether there is a better official player-facing name source.
- Whether `CombatSetUp` / `CombatEnded` are the correct final boundaries for all encounter types is still unverified.

## UI limitations

- The overlay is now visible and working again, but visual polish is still iterative rather than finalized.
- The current UI has been tuned by live trial in the real game rather than by any official STS2 UI guidance.
- Current compact/HUD tradeoffs are aesthetic judgments, not validated modding standards.
- If future layout regressions happen, the safest debug path is still to switch briefly to an exaggerated diagnostic panel.

## packaging / deployment limitations

- The exported `.pck` is currently not trusted as the live runtime path because the game runtime rejected a pack exported with a newer Godot version.
- Current live debugging should assume DLL-only deployment unless runtime compatibility of the `.pck` path is re-verified.
- The repo still contains packaging scripts and release flow, but successful packaging does not prove live runtime compatibility.
- Duplicate manifest files in the installed mod directory can still cause double-loading failures.

## save / progression risk

- Save/progression work remains the highest-risk part of this repo.
- A previous runtime progression path was able to corrupt modded `progress.save` epoch data.
- Steam Cloud can copy broken modded save files back to local storage after repair.
- Base/modded sync, rescue, unlock, and Neow-related code should all be treated as potentially dangerous until repeatedly verified.
- `FullUnlockBridge` is intentionally disabled for now and should stay that way unless replaced with a safer verified approach.

## remaining blockers / open questions

- Need to verify that `CombatHistory.DamageReceived(...)` fires for all important player damage sources in live runs beyond the now-confirmed poison/doom support.
- Need a verified source for per-player display name in multiplayer.
- Need confirmation of the minimal safe packaging/install shape for a code-only overlay mod.
- Need stronger confidence that current sync/rescue logic will not re-break timeline/progression state.

## learned stability constraints

- The `AbstractModel` / `ModHelper.SubscribeForCombatStateHooks(...)` experiment was not stable in this prototype.
- We hit concrete failure modes on that route including constructor/model-id/canonical-model issues.
- The current stable path is runtime subscription via `CombatManager` + direct patching of `CombatHistory.DamageReceived(...)`, not model instantiation.
