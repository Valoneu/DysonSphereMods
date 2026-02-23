# BottleneckUI

Adds a comprehensive UI panel listing all crafting machines across the planet that are currently lacking input items or suffering from power shortages, allowing you to instantly identify factory bottlenecks.

---

## Technical Information

### Mechanics and Configuration
- `Hotkey`: Default `LeftControl + B` to toggle the Bottleneck UI.
- All settings save directly to BepInEx `.cfg`.

### Deep Technical Details
Uses Harmony prefixes on machine simulation elements to collect state data, injecting a custom Unity GUI overlay built on the native DSP window framework.

### Dependencies
* **BepInEx, CommonAPI**

---
**Developer:** Valoneu
