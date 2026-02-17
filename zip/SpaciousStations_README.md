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
* **Storage Multiplier:** Multiply the maximum item capacity of each slot in the station. This multiplier also correctly applies to "Extra Storage" bonuses gained through research.
* **Split Config:** Differentiate settings for Planetary Logistics Stations (PLS) and Interstellar Logistics Stations (ILS).

### Throughput & Power
* **Charge Multiplier:** Increases both the maximum energy buffer and the charging power (workEnergyPerTick) of the station, allowing it to support more active ships and drones.
* **Drone Release Speed:** Configurable interval between drone dispatches. Setting this to 1 allows dispatching drones every tick (60 per second).
* **Vessel Release Speed:** Configurable interval between vessel dispatches and option to release multiple vessels per tick.

---

## Configuration
All multipliers are configurable via the BepInEx config file. Settings are split between `Planetary Logistics Station` and `Interstellar Logistics Station` sections.

### General
* **DroneTaskInterval:** Default `20`. The interval between drone dispatches. Lower is faster. Vanilla default is 20 (3 dispatches per second). Setting this to 1 will dispatch drones every tick (60 per second).
* **ShipTaskInterval:** Default `10`. The interval between vessel dispatches for high priority items. Lower is faster. Vanilla default is 10 (6 dispatches per second). Setting this to 1 will dispatch vessels every tick (60 per second). Note: other priority items use 3x and 6x this interval.

### Station Specific (PLS and ILS)
* **DroneMultiplier:** Default `2.0`
* **ShipMultiplier:** Default `2.0`
* **StorageMultiplier:** Default `2.0` (Scaling for item capacity)
* **ChargeMultiplier:** Default `2.0` (Scaling for charging speed)
* **EnergyMultiplier:** Default `2.0` (Scaling for max energy buffer)
* **ShipReleasePerTick:** (ILS Only) Default `1`. Maximum number of ships that can be dispatched from a single ILS in a single tick.

Multipliers are applied to station prototypes (affecting new stations and UI) and existing stations when loading a game.

---

## Version History

| Version | Description of Changes |
| :--- | :--- |
| **1.1.0** | Split configuration for PLS and ILS. Added `DroneTaskInterval`, `ShipTaskInterval`, and `ShipReleasePerTick` settings to increase dispatch speed. |
| **1.0.5** | Fixed a bug where the station storage slider was limited to vanilla values even when the mod allowed for more. |
| **1.0.1** | Fixed orbital collector energy costs causing negative production rates. Adjusted collector power scaling for stability. |
| **1.0.0** | Initial release. Added multipliers for drones, ships, storage, and charge power. |

---
**Developer:** Valoneu#8617 on Discord
