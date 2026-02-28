# StationLaunchSystem

Automated, high-performance Dyson construction system. Launches rockets and injects solar sails directly into Dyson shells from stations set to 'Storage' mode. Features equatorial priority and strict construction sequencing.

---

## Technical Information

### Mechanics and Configuration
- `RocketsPerTick`: Max rockets per tick per station.
- `SailsPerTick`: Max solar sails per tick per station.

### Deep Technical Details
Uses a 120-tick shell cache and 10-tick batch processing to minimize UPS lag. Sorting logic prioritizing absolute Y-coordinates (equator). Restores missing Dyson Sphere statistics for direct injections.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
