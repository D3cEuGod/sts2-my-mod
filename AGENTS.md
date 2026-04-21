# AGENTS.md

## Project
This repository is for building a Slay the Spire 2 mod for DPS (damage per turn / per person if in multi-player mode).

## Current phase
Baseline bootstrap is no longer purely exploratory.
The repo already has a working live-debug path for a DLL-loaded STS2 mod, but runtime behavior still must be verified against the actual installed game and real save files/logs.

## Primary goal
Keep the mod working in the real installed game, preserve save safety, and iterate toward correct DPS tracking plus a clean in-game overlay.
Do not introduce speculative abstractions.
Prefer fixes that are easy to trace, build, deploy, and verify.

## Source of truth
Use these in order:
1. docs/
2. examples/
3. notes/decisions.md
4. existing source files in this repo
5. real local runtime evidence (installed mod files, logs, reflected STS2 assemblies, actual save files)

If information is missing, say so explicitly.
Do not invent undocumented APIs, hooks, event names, or loader behavior.
Do not use legacy StS1 references as proof of StS2 runtime behavior.

## Working rules
- Prefer minimal, reversible changes.
- Reuse patterns from examples before creating new architecture.
- Explain every new file you create.
- Keep functions and files small unless there is a strong reason not to.
- When unsure, leave a short TODO comment and document the uncertainty in notes/known-issues.md.
- Do not rename large parts of the repo unless requested.
- Do not add dependencies unless required by the confirmed modding workflow.
- For this repo, do local investigation first and keep user retests minimal. Prefer reading logs/code over asking the user to blind-try steps.
- During live debugging, verify whether the game is loading the real installed artifact before treating the repo build as authoritative.
- Prefer edit -> build -> deploy -> verify loops.
- Keep `CHANGELOG.md` updated for substantive repo work so each code change can be traced later.

## Code generation rules
- Match the style and structure already used in docs/examples where they are actually relevant.
- Prefer explicit code over clever code.
- Avoid speculative abstractions.
- Keep debug helpers isolated when practical.
- Add reusable helpers only after at least two real use cases appear.
- When UI behavior is uncertain, prove visibility first with an obvious diagnostic before tuning layout details.

## Documentation rules
Whenever you change build steps, packaging steps, loader assumptions, entrypoint behavior, save-handling behavior, or live-debug workflow:
- update README.md
- update notes/decisions.md
- update notes/known-issues.md if something is uncertain
- update CHANGELOG.md with a short dated summary

## Validation
Before claiming work is done:
1. confirm which verified doc/example/runtime evidence the implementation follows
2. list what was created/changed
3. state what is still assumed or unknown
4. say whether the change was only in-repo or also deployed to the actual runtime target
5. provide exact steps to run/build/test if available

## Definition of done
A task is done only if:
- the implementation matches available docs/examples/runtime evidence,
- the changed files are explained clearly,
- uncertainties are called out explicitly,
- README or notes are updated when needed,
- CHANGELOG.md is updated for substantive repo work.

## Confirmed local macOS findings
- The game scans mods from `SlayTheSpire2.app/Contents/MacOS/mods`, not the outer game-root `mods/` folder.
- PCK export must be compatible with the game's Godot runtime. A PCK exported with Godot 4.6.2 failed in the game's 4.5.1 runtime.
- Live deployment currently works most reliably as DLL-only. Do not assume the exported `.pck` is safe to ship into the running game until engine compatibility is verified.
- Keep only one manifest JSON in the installed mod folder. Duplicate manifests can cause the same mod to be discovered twice.
- Do not let `examples/` source files compile into the shipped mod assembly or export into the shipped PCK.
- Overlay UI should be input-pass-through by default. Fullscreen `Control` nodes can block menu and gameplay interaction if they receive input.
- If UI visibility is unclear, use an exaggerated diagnostic panel first to prove the render path before chasing subtle anchoring/layout bugs.
- Save/progression work is high-risk. Prefer file-level verification and backups over unchecked runtime writeback.
- Steam Cloud can reintroduce bad modded saves, so local-only repair may not stick.

## Additional StS2 example guidance
- `examples/sts2/DamageMeter/` is a binary-only example (`.json` + `.dll` + `.pck`) with no source code. Treat it as packaging and UX evidence, not as API or hook proof.
- Binary string inspection can reveal likely product ideas (for example segmented views like Current/Overall/Fight, compact mode, dashboard-style categories, localization, analytics sync), but those strings are not sufficient evidence for exact loader contracts or runtime APIs.
- If a release package lands under a nested folder such as `mods/<release-name>/<mod-id>/...`, do not assume the game will scan it correctly. Verify the final installed mod root shape explicitly before blaming loader or code.

## Current priority tasks
- keep DPS tracking correct against verified real STS2 hooks
- keep the installed mod deployment path in sync with repo changes
- preserve save safety and avoid progression corruption
- keep the overlay compact, readable, and usable in the real game UI
- continue documenting verified runtime findings instead of guessing from StS1 or incomplete examples
