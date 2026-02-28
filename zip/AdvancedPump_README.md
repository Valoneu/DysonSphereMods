# AdvancedPump

Repurposes Advanced Miners (Vein Collectors) into high-capacity planet pumps. When placed anywhere on a planet with a fluid (water, acid), they extract it at an extreme rate (150,000 speed). Completely removes the floating item icon for a cleaner look.

---

## Technical Information

### Mechanics and Configuration
N/A (Operates automatically on placement).

### Deep Technical Details
Hooks into `onFactoryFrameEnd` to scan Vein Collectors every 30 ticks; forces `EMinerType.Water` and clears `entitySignPool` icons for advanced miners with zero veins. Overrides `BuildTool_Click` to allow placement on any terrain.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
