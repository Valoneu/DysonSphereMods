# CopyPasteStations

Seamlessly integrates with the game's native building copy/paste system to transfer detailed station configurations. Automatically refills missing drones and ships from your inventory during the paste operation.

---

## Technical Information

### Mechanics and Configuration
Uses native Copy (`Shift+C`) and Paste (`Shift+V`) commands.

### Deep Technical Details
Hooks `PlanetFactory.CopyBuildingSetting/PasteBuildingSetting` to serialize/deserialize `StationComponent` state. Includes logic to verify and transfer inventory drones/ships into the target station on-demand.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
