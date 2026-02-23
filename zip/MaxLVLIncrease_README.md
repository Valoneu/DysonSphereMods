# MaxLVLIncrease

Strips out the arbitrary level caps injected by late-game infinite research technologies, allowing all repeating tech (like Vein Utilization and Drone Speed) to scale literally forever.

---

## Technical Information

### Mechanics and Configuration
- `MaxLevel`: Default `99999` (or infinite). Standard configuration cap variable.

### Deep Technical Details
Overwrites the game's native tech-tree maximum definitions during initialization by patching the proto database immediately after the localized dictionary load sequence.

### Dependencies
* **BepInEx**

---
**Developer:** Valoneu
