# FactoryOverclock

Provides deep tools to multiply the production speed of machines (Assemblers, Smelters, Miners, Labs, Ejectors, Silos) while dynamically scaling power requirements and circumventing the 3600 items/minute station throughput and fractionator limitations.

---

## Technical Information

### Mechanics and Configuration
- `MultiplierEnabled`: Toggle the mod entirely (Hotkey: `KeypadMinus`).
- `AssemblerMultiplier`, `MinerMultiplier`, `StationMultiplier`, etc.: Defines exact scaling integers.
- `DrawMultiplier`: Power consumption scaling factor.

### Deep Technical Details
Uses Harmony prefixes/postfixes to double-tick logistical operations like fractionators and splitters, while pre-calculating modified arrays for component work-energy logic. Zero runtime reflection.

### Dependencies
* **BepInEx, CommonAPI**

---
**Developer:** Valoneu
