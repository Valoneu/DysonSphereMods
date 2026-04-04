# DistributeSpray

Eliminates the need for spray coaters and belts by automatically applying proliferator spray to items as they enter machines planet-wide, drawing from any station storage with proliferator available.

---

## Technical Information

### Mechanics and Configuration
- `ModEnabled`: Master toggle in BepInEx config.
- Station slot must be set to 'Storage' (None) mode in the local logistics settings.

### Deep Technical Details
Implements an optimized credit-based system. Intercepts `PlanetFactory.InsertInto` to apply spray immediately, then reconciles the material cost from station storage on a background task.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28485
**Developer:** Valoneu
