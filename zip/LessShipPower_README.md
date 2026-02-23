# LessShipPower

Optimizes and reduces the massive energy blocks required for Interstellar Logistics Vessels to initiate hyperspace warps, allowing earlier game interstellar transport without collapsing local energy grids.

---

## Technical Information

### Mechanics and Configuration
- `VesselEnergyScale`: Float multiplier (Default `0.25`) that scales back the megawatt cost of interstellar travel.

### Deep Technical Details
Executes a rapid Harmony Postfix on `StationComponent.CalcTripEnergyCost`, intercepting the internal 64-bit integer mathematics block and scaling it down securely.

### Dependencies
* **BepInEx**

---
**Developer:** Valoneu
