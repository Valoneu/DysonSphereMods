# CloserStations

Significantly reduces the minimum placement distance required between logistics stations, allowing you to pack Planetary and Interstellar Logistics Stations tightly together to form compact super-hubs.

---

## Technical Information

### Mechanics and Configuration
- `MinStationDistance`: Float configurable value dictating the required placement radius between stations.

### Deep Technical Details
Transpiles the game's internal `BuildTool_Click` and placement validation methods to forcefully override the planetary distance checks for station entities.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
