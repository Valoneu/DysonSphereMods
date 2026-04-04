# LessShipPower

Reduces the massive energy required for Interstellar Logistics Vessels to initiate trips, allowing easier interstellar transport without collapsing local energy grids in the mid-game.

---

## Technical Information

### Mechanics and Configuration
- `VesselEnergyScale`: Multiplier (Default `0.25`) that scales back the energy cost of vessel travel.

### Deep Technical Details
Applies a Harmony Postfix on `StationComponent.CalcTripEnergyCost`, intercepting the internal cost calculation and scaling it down before applying the drain.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28485
**Developer:** Valoneu
