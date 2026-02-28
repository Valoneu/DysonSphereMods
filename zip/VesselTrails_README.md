# VesselTrails

Activates high-performance rendering for beautiful, dynamic logistics vessel travel trails inside the Star Map. Gives you a live, visual heartbeat of your entire galactic logistics web moving between stars. Always records 60 minutes of data with a display-only slider, and persists trail data across save/load.

---

## Technical Information

### Mechanics and Configuration
- `HistoryMinutes`: Display range slider (1-60 minutes). Data always records full 60 minutes.
- Configurable toggles for trail rendering, thickness, color mode, and alpha fade.

### Deep Technical Details
Intersects standard UI rendering pools, safely pushing visual vertex elements directly into the 3D Star Map canvas layer. Trail data persists via `.vesseltrails` sidecar files alongside game saves.

### Dependencies
* **BepInEx, CommonAPI**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
