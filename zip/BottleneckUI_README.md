# BottleneckUI

Adds a comprehensive UI panel listing all crafting machines across the planet that are currently lacking input items or suffering from power shortages. Features real-time scanning and sorting to help you kill factory bottlenecks instantly.

---

## Technical Information

### Mechanics and Configuration
- `Hotkey`: Default `LeftControl + B` to toggle the Bottleneck UI.
- Filter by machine type, power status, or inventory state.

### Deep Technical Details
Uses Harmony patches to collect machine states during simulation, injecting a custom Unity GUI overlay into the native DSP window framework with optimized list virtualization.

### Dependencies
* **BepInEx, CommonAPI**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
