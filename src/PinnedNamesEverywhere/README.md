# PinnedNamesEverywhere

Makes pinned star and planet names visible in the normal camera view even at long distances or screen edges.

## Features
- Pinned objects (stars, planets, hives, etc.) always show their name label.
- Labels remain visible even when they are at the edge of the screen (no more need to hover over the arrow icon).
- Labels remain clearly visible even at long distances (configurable minimum opacity).
- Configurable option to also always show distances for pinned objects.

## Configuration
All settings can be adjusted via the standard BepInEx configuration framework.
- `AlwaysShowPinnedNames`: Enable or disable the core feature.
- `AlwaysShowPinnedDistances`: Whether to also always show distances for pinned objects.
- `PinnedNamesMinimumAlpha`: Minimum opacity for pinned labels (default: 0.8).
