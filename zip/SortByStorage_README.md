# SortByStorage

Injects a highly requested 'Storage' sorting option natively into the Production Statistics UI panel, letting you sort the entire planetary or galactic network by the total quantity of stored items.

---

## Technical Information

### Mechanics and Configuration
- Operates natively inside the Production screen. No configs required.

### Deep Technical Details
Hooks into the game's internal `UIProductionStatWindow` UI routines via Harmony transpilation, injecting custom sorting parameter logic and rendering GUI list elements transparently.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
