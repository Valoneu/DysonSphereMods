# DistributeSpray

Eliminates the need for spray coaters and belts by automatically applying proliferator spray to items as they enter machines planet-wide, drawing from any station storage with proliferator available. Station slot needs to be set to 'Storage' mode.

---

## Technical Information

### Mechanics and Configuration
- `ModEnabled`: Master toggle in BepInEx config.

### Deep Technical Details
Implements an optimized debt-based credit system. Intercepts `PlanetFactory.InsertInto` to apply spray immediately, then reconciles the material cost from station storage on a background task every frame.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
