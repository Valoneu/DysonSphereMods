# HideWarnings

Selectively hide disruptive in-game warnings with a dedicated configuration UI. Supports hiding power failure, resource shortages, sorter jams, and even the research completion banner.

---

## Technical Information

### Mechanics and Configuration
- `Hotkey`: Default `Keypad5` to toggle the filter window.
- Toggles for: Power, Veins, Sorters, Damage, Dashboard, and Tech.

### Deep Technical Details
Postfixes `WarningSystem.WarningLogic` to zero out hidden signal counts. Prefixes `UIGeneralTips.OnTechUnlocked` to safely suppress UI research popups.

### Dependencies
* **BepInEx, CommonAPI**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
