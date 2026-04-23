# maintenance note - 2026-04-23 history panel polish

This pass improved the in-run combat history panel without changing the stable hit-capture path.

## What actually fixed the feature request

1. `DpsTracker` now keeps `HighestSingleHit` inside the same player damage state that already accumulates total damage.
   - This avoids inventing a second stats pipeline.
   - Historical combat cards can now show both total output and peak single-hit output.

2. History cards are now expandable.
   - Collapsed state shows a short top-player summary.
   - Expanded state shows more players from that combat without leaving the in-panel history page.

3. History browsing now pages and scrolls better for longer runs.
   - The page keeps a bounded number of combat cards visible at once.
   - The scroll area is taller and cleaner, so long runs do not turn the panel into one huge stack.

## Boundary to preserve

- Keep peak-hit stats derived from the existing accepted damage events only.
- Do not add a separate capture path just to support richer UI summaries.
- If future history UI needs more metrics, prefer extending finalized snapshot data rather than re-reading combat state after the fact.
