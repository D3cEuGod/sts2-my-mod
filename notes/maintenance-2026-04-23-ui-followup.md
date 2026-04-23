# maintenance note - 2026-04-23 UI/stats follow-up

This pass intentionally avoided touching the stable main damage-capture path.

## What actually changed safely

1. `PrototypeSettings.Load()` now reads `showPanel` from ModConfig again.
   - The setting existed in the config UI but was being overwritten to `true` on load.
   - Result: users could think panel visibility persisted when it actually did not.

2. `DpsOverlay` now respects the configured visible-row count.
   - Before this, `maxRows` was exposed in ModConfig but the overlay still hardcoded row counts.
   - The panel now grows vertically when more current-combat rows are shown, instead of silently clipping by fixed expectations.

3. Lifetime / last-combat summaries now filter out zero-damage roster entries.
   - Keeping seeded roster rows is useful during live combat.
   - In summary views, those zero-damage rows made the panel look less trustworthy, so they are now filtered.

## Boundary to preserve

- Do not touch the stable Harmony damage-capture mainline just to do UI polish.
- Keep UI/settings fixes isolated unless runtime evidence shows a real stats bug in the capture path.
