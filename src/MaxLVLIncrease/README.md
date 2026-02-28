# MaxLVLIncrease

Extends the level caps for infinite research technologies (like Vein Utilization and Drone Speed), allowing them to scale past vanilla limits.

---

## Technical Information

### Mechanics and Configuration
- `MaxLevelValue`: Sets the new max level (Default `50,000`).

### Deep Technical Details
Overwrites the game's native tech-tree maximum definitions during initialization by patching the prototype database and ensuring tech states are migrated correctly on save load.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
