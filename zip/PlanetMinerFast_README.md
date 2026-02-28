# PlanetMinerFast

An aggressively optimized fork of PlanetMiner. It completely eliminates the need for mining machines, pulling ores directly into associated Logistics Stations set to 'Local Demand'. Practically zero UPS and GPU impact.

---

## Technical Information

### Mechanics and Configuration
- Miner activation configuration inside Station context menus.

### Deep Technical Details
Compiles Weaver compatibility validation checks into `FastInvokeHandler` delegates to completely skip standard slow C# reflection overhead during every fast DSP tick.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
