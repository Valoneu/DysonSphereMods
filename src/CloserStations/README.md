# CloserStations
**Precision Logistics Station Placement**

---

![Category](https://img.shields.io/badge/Category-Building-blue?style=flat-square)
![Complexity](https://img.shields.io/badge/Utility-QualityOfLife-green?style=flat-square)

CloserStations reduces the mandatory clearance distance between logistics stations, allowing for more compact factory designs and optimized belt layouts.

---

## Key Features

### Placement Flexibility
* **Reduced Clearance:** Decreases the minimum distance required between Planetary Logistics Stations, Interstellar Logistics Stations, and Orbital Collectors.
* **Compact Layouts:** Enables tighter clusters of stations, ideal for resource-constrained environments or high-density factory zones.
* **Vein Collector Support:** Also applies reduced distance constraints to Advanced Miners (Vein Collectors).

### Customization
* **Adjustable Proximity:** Fully configurable distance multiplier. Tighten or loosen the constraints based on your preference.
* **Dynamic Loading:** Changes are applied immediately upon configuration update (standard BepInEx config).

---

## Configuration
The minimum distance is governed by a `DistanceMultiplier`. 
* **Default:** `0.75` (25% closer than vanilla).
* Setting this to `0.5` allows stations to be placed 50% closer, while `1.0` restores vanilla behavior.

---

## Version History

| Version | Description of Changes |
| :--- | :--- |
| **1.0.0** | Initial release. Implemented distance reduction for all logistics station types and Vein Collectors. |

---
**Developer:** Valoneu#8617 on Discord
