# FactoryOverclock
**High-Performance Production Scaling**

---

![Category](https://img.shields.io/badge/Category-Optimization-blue?style=flat-square)
![Complexity](https://img.shields.io/badge/Utility-Advanced-orange?style=flat-square)

FactoryOverclock provides tools to multiply the production speed of most buildings while maintaining game balance through adjusted power requirements.

---

## Key Features

### Production & Throughput
* **Building Overclock:** Direct speed multiplication for Assemblers, Smelters, Miners, Labs, Ejectors, and Silos.
* **Station Buffering:** Specialized patch for logistics stations allowing up to **3600 items/minute** per slot to match overclocked belt speeds.
* **Safety Protocol:** Hard-capped belt speeds to prevent internal game buffer overflows and ensure stability.

### Resource Management
* **Quadratic Power Draw:** Energy consumption increases quadratically relative to speed multipliers.
* **Real-time Toggle:** Instant activation/deactivation via hotkey (Default: **KeypadMinus**).

---

## Configuration
All multipliers and hotkeys are fully customizable via the standard BepInEx configuration framework. Changes can be applied in-game using a Configuration Manager or by editing the `.cfg` file.

---

## Version History

| Version | Description of Changes |
| :--- | :--- |
| **2.1.4** | Added 3600/m throughput support for Fractionators and Pilers (Aggressive loading/unloading and cooldown removal). |
| **2.1.1** | Implemented 3600/m station throughput fix, belt speed safety cap, and persistent keybind support. |
| **2.1.0** | Resolved bidirectional sorter synchronization issues and improved overall efficiency. |
| **2.0.9** | Implemented robust sorter speed logic using effective power multiplication. |
| **2.0.8** | Corrected sorter default speed logic and expanded patch coverage. |
| **2.0.5** | Addressed game updates regarding InserterComponent field changes. |
| **2.0.0** | Major update for game multithreading compatibility. |
| **1.1.0** | Added overclocking support for power generators. |
| **1.0.0** | Initial release. |

---
**Developer:** Valoneu#8617 on Discord



