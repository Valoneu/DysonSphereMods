# SpaciousStations

Comprehensive logistics station upgrade. Multiplies storage, drone/vessel capacity, and charging speed. Optimized drone dispatch prioritizes the closest available station.

---

## Technical Information

### Mechanics and Configuration
- Extensive multipliers for PLS, ILS, and Exchange Stations.
- `ShipReleasePerTick`: Controls how many ships launch per tick.
- `DroneTaskInterval`: Controls how fast drones are dispatched.

### Deep Technical Details
Modifies station prototypes via Harmony patches on `VFPreload`. Implements a distance-based comparer for `RematchLocalPairs` to ensure drones always fly the shortest path.

### Dependencies
* **BepInEx, CommonAPI**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
