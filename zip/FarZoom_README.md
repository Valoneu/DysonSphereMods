# FarZoom

Dramatically expands the camera's zoom parameters and adds Field of View (FOV) controls. Zoom out lightyears in the star map or enjoy wide-angle planetary views.

---

## Technical Information

### Mechanics and Configuration
- `Shift + Scroll`: Change Field of View (FOV).
- `ZoomMultiplier`: Extends maximum zoom distance.
- `ZoomSpeedMultiplier`: Adjusts camera zoom sensitivity.

### Deep Technical Details
Patches `GameCamera`, `RTSPoser`, and `PlanetPoser` calculation logic, modifying FOV and distance clamp values before the camera matrix constraints are applied.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28485
**Developer:** Valoneu
