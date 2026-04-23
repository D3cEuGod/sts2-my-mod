# maintenance note - 2026-04-23 main panel height tuning

A follow-up pass rolled back the earlier content-driven height behavior for the main expanded panel.

## What actually changed

1. The previous fully dynamic expanded-height behavior was removed.
   - In live use it could grow larger than desired.
   - That made the HUD feel heavier instead of cleaner.

2. The main expanded panel now uses a fixed height tuned for the three primary sections.
   - It still scales mildly with visible row count.
   - It no longer tries to exactly match whatever content is currently visible.

## Boundary to preserve

- Keep height tuning simple and predictable for the main HUD.
- Prefer small fixed-size adjustments over live auto-sizing unless a future design explicitly needs resizable content areas.
