# CopyPasteStations

Seamlessly integrates with the game's native building copy/paste system to transfer detailed station configurations. Automatically refills missing drones and ships from your inventory during the paste operation.

---

## Technical Information

### Mechanics and Configuration
Uses native Copy (`Shift+C`) and Paste (`Shift+V`) commands.

### Deep Technical Details
Hooks `PlanetFactory.CopyBuildingSetting` and `PasteBuildingSetting` to serialize/deserialize `StationComponent` states. Logic verifies and transfers inventory drones/ships into the target station on-demand.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28485
**Developer:** Valoneu
