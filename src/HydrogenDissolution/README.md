# HydrogenDissolution

Adds a new, highly-efficient chemical plant recipe designed exclusively to destroy excess Hydrogen. Perfect for late-game refining layouts or antimatter setups that stall due to trapped hydrogen backups.

---

## Technical Information

### Mechanics and Configuration
- No direct configuration required. The recipe is automatically injected into the central game registry.

### Deep Technical Details
Uses BepInEx to execute Harmony patches alongside CommonAPI's `ProtoRegistry` to safely inject `ERecipeType.Chemical` (ID 650) directly into the item database at runtime.

### Dependencies
* **BepInEx, CommonAPI**

---
**Developer:** Valoneu
