# maintenance - 2026-05-04 - overlay language toggle

## What actually worked

- Added a dedicated overlay text/localization helper instead of sprinkling language branching through combat hook code.
- Exposed language selection through ModConfig as a dropdown (`Auto` / `English` / `简体中文`) so the in-game HUD stayed visually clean.
- Kept runtime text refresh inside the overlay view layer, so switching language updates the panel copy without changing the combat-tracking pipeline.

## What not to disturb

- Do not add another always-visible language button into the combat HUD unless explicitly requested.
- Do not mix localization work into Harmony damage hooks or tracker attribution logic.
- Keep language behavior reversible through settings; the clean boundary is `PrototypeSettings` + `OverlayText` + overlay refresh.
