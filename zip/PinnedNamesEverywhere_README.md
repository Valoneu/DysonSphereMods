# PinnedNamesEverywhere

Forces pinned star and planet names alongside their distances to remain globally visible on your screen, even when you are lightyears away on the opposite side of the cluster.

---

## Technical Information

### Mechanics and Configuration
- `Alpha`: Configuration for GUI text transparency levels.
- `Visibility`: Toggle distance culling bounds.

### Deep Technical Details
Replaces logic inside `UIStarmap` and localized GUI renderers. Uses an optimized cache mechanism to limit string allocations while rendering external 3D space text onto the 2D UI plane.

### Dependencies
* **BepInEx**

---
**Developer:** Valoneu
