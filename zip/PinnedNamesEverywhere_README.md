# PinnedNamesEverywhere

Ensures pinned star and planet names (and distances) remain globally visible on your screen, regardless of camera distance or screen position.

---

## Technical Information

### Mechanics and Configuration
- `PinnedNamesMinimumAlpha`: Ensures names remain readable even when far away.
- `AlwaysShowPinnedDistances`: Optional toggle for distance persistence.

### Deep Technical Details
Patches `UISpaceGuideEntry` to force text visibility and `UISpaceGuide.ClipEntryPool` to ensure pinned objects bypass the game's standard UI culling logic.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
