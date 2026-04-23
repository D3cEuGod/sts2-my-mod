# maintenance note - 2026-04-23 combat history panel

This change adds per-run browsing of older completed fights without touching the stable damage-capture path.

## What actually fixed the feature request

1. `DpsTracker` now retains a small per-run list of finalized combat records.
   - Each record stores the combat index, round count, and finalized player snapshots.
   - This is appended only when a combat is finalized, so it does not interfere with live hit capture.

2. The old "上一场结算" section became the entry point to a dedicated history view.
   - Clicking the header opens an in-panel history page.
   - That page lists earlier fights from the current run, newest first.

3. The history page intentionally shows older fights only.
   - If you are between combats, the just-finished fight still belongs to the main settlement view.
   - The history page shows fights before that, which keeps the main panel and the history page from duplicating the same combat.

## Boundary to preserve

- Do not rework live damage capture just to support history browsing.
- Keep historical fight viewing derived from finalized snapshots only.
- If future UI work expands this page, prefer additive tracker/UI state instead of mixing archive logic into Harmony hit processing.
