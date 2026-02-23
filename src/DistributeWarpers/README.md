# DistributeWarpers

Automates the balancing of Space Warpers across an entire planet. Instead of manually belting warpers to every station, this mod aggregates warpers and automatically inserts them into the internal warper slots of any station requesting them.

---

## Technical Information

### Mechanics and Configuration
- `TargetCount`: Default `50`. The number of warpers to maintain in a station's internal slot.

### Deep Technical Details
Subscribes to `TickManager`'s slow-tick event (every 60 frames) to scan planetary logistics pools and silently transfer items without triggering expensive physics updates in the main game loop.

### Dependencies
* **BepInEx**

---
**Developer:** Valoneu
