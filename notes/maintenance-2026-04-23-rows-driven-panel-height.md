# maintenance note - 2026-04-23 rows-driven panel height

A follow-up pass corrected the main panel sizing again.

## What actually changed

1. The coarse fixed-height-by-maxRows rule was removed for the main HUD.
2. The main expanded panel now sizes from the rows actually rendered in:
   - current combat
   - run total
   - last combat
3. The rule still stays bounded, so it does not balloon like the earlier broader content-driven auto-sizing experiment.

## Boundary to preserve

- Size the main HUD from rendered row counts, not from invisible future capacity.
- Avoid returning to full content-measurement auto-sizing unless a future design explicitly needs it.
