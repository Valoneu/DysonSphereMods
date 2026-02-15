# SpaciousStations
**Logistics Station Capacity & Power Multipliers**

---

![Category](https://img.shields.io/badge/Category-Logistics-blue?style=flat-square)
![Complexity](https://img.shields.io/badge/Utility-Performance-green?style=flat-square)

SpaciousStations allows you to scale up the capabilities of your logistics stations. Multiply drone/ship counts, storage capacity, and charging speed to handle massive industrial throughput.

---

## Key Features

### Capacity Scaling
* **Drone Multiplier:** Increase the maximum number of logistics drones per station.
* **Ship Multiplier:** Scale the maximum number of logistics vessels per interstellar station.
* **Storage Multiplier:** Multiply the maximum item capacity of each slot in the station.

### Throughput & Power
* **Charge Multiplier:** Increases both the maximum energy buffer and the charging power (workEnergyPerTick) of the station, allowing it to support more active ships and drones.

---

## Configuration
All multipliers are configurable via the BepInEx config file:
* **DroneMultiplier:** Default `2.0`
* **ShipMultiplier:** Default `2.0`
* **StorageMultiplier:** Default `2.0`
* **ChargeMultiplier:** Default `2.0` (Scaling for charging speed)
* **EnergyMultiplier:** Default `2.0` (Scaling for max energy buffer)

Multipliers are applied to station prototypes (affecting new stations and UI) and existing stations when loading a game.

---

## Version History

| Version | Description of Changes |
| :--- | :--- |
| **1.0.5** | Fixed a bug where the station storage slider was limited to vanilla values even when the mod allowed for more. |
| **1.0.1** | Fixed orbital collector energy costs causing negative production rates. Adjusted collector power scaling for stability. |
| **1.0.0** | Initial release. Added multipliers for drones, ships, storage, and charge power. |

---
**Developer:** Valoneu#8617 on Discord
