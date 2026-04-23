# maintenance note - 2026-04-23 player row backgrounds

This pass improved row readability in the main HUD without changing the data path.

## What actually changed

1. Main player rows are now wrapped in subtle tinted background cards.
2. The tint is deterministic from `playerId`, so the same player keeps the same color identity instead of changing color randomly between refreshes.
3. The background treatment is intentionally light so it separates rows visually without overpowering the existing HUD styling.

## Boundary to preserve

- Keep row-color identity derived from stable player identity, not current rank.
- Do not add visual treatments that make the panel noisy enough to hurt readability.
