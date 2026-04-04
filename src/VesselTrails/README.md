# VesselTrails

Visualizes galactic logistics routes with dynamic 3D travel trails. See your entire logistics web moving in real-time or as a heatmap of traffic volume. Data persists across sessions.

---

## Technical Information

### Mechanics and Configuration
- `NumPad 1`: Toggle Logistics UI. `NumPad 3`: Toggle trail lines.
- Configurable opacity, thickness, and color modes (Material vs Heatmap).

### Deep Technical Details
Uses custom GL rendering to draw trails in 3D space. Tracks vessel history in a background manager and saves traffic data in `.vesseltrails` files alongside game saves.

### Dependencies
* **BepInEx, CommonAPI**

---
**Game Version:** V0.10.34.28485
**Developer:** Valoneu
