# maintenance note - 2026-04-23 overlay size + current champion pass

This pass kept all improvements inside tracker/UI presentation and did not alter the accepted main damage-capture path.

## What actually improved the panel

1. The current-combat section now has the same champion/highest-hit visual language as history cards.
   - A dedicated champion line is shown for the current top player.
   - Per-player current rows now show highest single hit in their detail line.

2. The panel is slightly larger and easier to read.
   - Width, spacing, and several key font sizes were nudged up together instead of only enlarging one element.

3. Expanded panel height now follows visible content.
   - The old mostly fixed expanded height could leave a large empty block under sparse states.
   - The overlay now sizes itself from the currently visible section container, which keeps between-combat states tighter.

## Boundary to preserve

- Keep current-combat polish derived from already finalized/current tracker snapshots.
- Do not create extra runtime hook paths just to power visual highlight features.
- If future UI work changes panel sizing again, preserve the content-driven behavior unless live testing proves a specific fixed height is needed.
