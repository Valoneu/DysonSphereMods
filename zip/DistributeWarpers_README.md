# DistributeWarpers

Automates Space Warper distribution across a planet. Aggregates warpers from any station storage and automatically refills the internal warper slots of all Interstellar Logistics Stations (ILS) on that planet.

---

## Technical Information

### Mechanics and Configuration
- `TargetCount`: Default `50`. The number of warpers to maintain in a station's internal slot.
- `CheckInterval`: How often (in ticks) to check and distribute.

### Deep Technical Details
Subscribes to `onFactoryFrameEnd` to scan planetary logistics pools and transfer items without triggering expensive physics updates or breaking vanilla statistics.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28485
**Developer:** Valoneu
