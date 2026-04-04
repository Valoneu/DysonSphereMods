# StationLaunchSystem

Automated, high-performance Dyson construction system. Launches rockets and injects solar sails directly into Dyson shells from stations set to 'Storage' mode. Features equatorial priority.

---

## Technical Information

### Mechanics and Configuration
- `RocketsPerTick`: Max rockets per tick per station.
- `SailsPerTick`: Max solar sails per tick per station.

### Deep Technical Details
Uses a 120-tick shell cache and 10-tick batch processing to minimize UPS lag. Prioritizes shell construction starting from the equator (absolute Y-coordinate).

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28485
**Developer:** Valoneu
