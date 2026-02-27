# FarZoom

Dramatically expands the camera's zoom parameters, allowing you to zoom significantly further away from the mech in planetary view, and expanding the Starmap zoom capabilities for a vastly better galaxy overview.

---

## Technical Information

### Mechanics and Configuration
- `MaxZoomDistance`: Configuration for the maximum planetary camera distance.
- `StarmapZoom`: Configuration for interstellar camera bounds.

### Deep Technical Details
Patches the game's `PlayerCamera` and `UIStarmap` camera clamp logic, modifying maximum distance floating-point values before the camera matrix constraints are applied.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
