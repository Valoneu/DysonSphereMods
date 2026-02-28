# CloserStations

Reduces the minimum placement distance required between logistics stations, allowing you to pack Planetary and Interstellar Logistics Stations tightly together.

---

## Technical Information

### Mechanics and Configuration
- `DistanceMultiplier`: Default `0.75`. Multiplier for the minimum placement radius.

### Deep Technical Details
Transpiles `BuildTool_Click.CheckBuildConditions` and `BuildTool_BlueprintPaste.CheckBuildConditions` to scale down the magic distance constants used for station-to-station collision.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
