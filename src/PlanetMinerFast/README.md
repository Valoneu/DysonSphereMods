# PlanetMinerFast

An aggressively optimized mining system that pulls ores directly into Logistics Stations set to 'Local Demand'. Eliminates the need for miners, belts, and power lines over veins with minimal UPS impact.

---

## Technical Information

### Mechanics and Configuration
Set a station slot to 'Local Demand' for the target ore to start mining planet-wide.

### Deep Technical Details
Uses an optimized `PlanetVeinCache` (rebuilt on changes) to identify veins and directly inject items into station storage. Consumes 20MJ per operation from the station's energy pool.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28485
**Developer:** Valoneu
