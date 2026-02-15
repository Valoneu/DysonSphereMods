# VesselTrails
**Galactic Trade Route Visualizer**

---

![Category](https://img.shields.io/badge/Category-Visual-blue?style=flat-square)
![Complexity](https://img.shields.io/badge/Utility-Information-green?style=flat-square)

VesselTrails transforms your Star Map into a living dashboard of your galactic empire. It draws connection lines between star systems that are currently exchanging resources via logistics vessels, providing both a beautiful visual and deep industrial insight.

---

## Key Features

### Trade Route Visualization
* **Active Connections:** See real-time paths between stars where vessels are currently in transit.
* **3D Prism Rendering:** High-performance GL rendering maintains consistent line thickness and visibility at all distances.
* **Material Mode:** Lines are colored based on the specific items being transported (e.g., Blue for Iron, Red for Circuits). Multiple items on one route are rendered as a "bundle" of lines.
* **Heatmap Mode:** Lines are colored from Green to Red based on traffic volume (Total vessels).

### Logistics Dashboard (`Ctrl + NumPad 1`)
* **Real-time Stats:** Track individual item throughput across every galactic connection.
* **History Window:** Configure a history window (0-60 minutes) to see cumulative trade statistics.
* **Table Columns:**
    * **Total:** Unique vessel trips started within the history window.
    * **/min:** The frequency of vessel departures (throughput).
    * **Load:** Traffic density—the average number of vessels concurrently in flight (this controls trail brightness).
* **Hover Tooltips:** Point at any trail in the Star Map to see its specific trade table immediately.

### Customization
* **Toggleable Trails (`Ctrl + NumPad 3`):** Quickly enable or disable the visualization.
* **UI Persistence:** The logistics window remembers its position and size across game restarts.
* **Opacity & Thickness:** Fine-tune the visual prominence of trails in the Star Map.

---

## Configuration
Settings can be adjusted in the `com.Valoneu.VesselTrails.cfg` file or via the in-game UI:
* **ShowTrails:** Master toggle for the effect.
* **TrailOpacity:** Controls the transparency of the lines.
* **TrailThickness:** Base thickness multiplier for the 3D beams.
* **ColorMode:** Set to `Material` or `Heatmap`.
* **HistoryMinutes:** How long statistics and trails persist after activity stops.

---

## Version History

| Version | Description of Changes |
| :--- | :--- |
| **1.2.0** | **UI & Stats Overhaul.** Added persistent Logistics Window with trip tracking (/min, Load, Total). Implemented "Bundle" rendering for multi-material routes. DSP-themed UI. |
| **1.1.0** | Initial release. Implemented Star Map route tracking and GL-based line rendering. |

---
**Developer:** Valoneu#8617 on Discord
