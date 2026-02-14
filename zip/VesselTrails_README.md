# VesselTrails
**Galactic Trade Route Visualizer**

---

![Category](https://img.shields.io/badge/Category-Visual-blue?style=flat-square)
![Complexity](https://img.shields.io/badge/Utility-Information-green?style=flat-square)

VesselTrails transforms your Star Map into a living dashboard of your galactic empire. It draws connection lines between star systems that are currently exchanging resources via logistics vessels.

---

## Key Features

### Trade Route Visualization
* **Active Connections:** See real-time paths between stars where vessels are currently in transit.
* **Dynamic Intensity:** The brightness and opacity of the lines scale with the number of active vessels on that route.
* **Empire Insight:** Quickly identify your major industrial hubs and remote resource outposts.

### Customization
* **Toggleable Trails:** Enable or disable the visualization via config.
* **Color Modes:** Choose between **Material** (colors based on what's inside the vessels) or **Heatmap** (colors based on traffic volume).
* **Opacity Control:** Adjust how subtle or prominent the lines appear in the Star Map.
* **Throughput Scaling:** Configure how the mod reacts to high-traffic routes.

---

## Configuration
Settings can be adjusted in the `com.Valoneu.VesselTrails.cfg` file:
* **ShowTrails:** (Default: `true`) Master toggle for the effect.
* **TrailOpacity:** (Default: `0.6`) Controls the transparency of the lines.
* **TrailThickness:** (Default: `1.0`) Base thickness multiplier.
* **ColorMode:** (Default: `Material`) Set to `Material` or `Heatmap`.

---

## Version History

| Version | Description of Changes |
| :--- | :--- |
| **1.1.0** | Merged duplicate routes in UI, added resizable Logistics window, enhanced tooltips to show all resources per route, and added a tooltip toggle. |
| **1.1.0** | Added in-game Logistics UI (Default: `Ctrl + NumPad1`), rebindable keys via CommonAPI, and hover tooltips for trails. |
| **1.0.0** | Initial release. Implemented Star Map route tracking and GL-based line rendering. |

---
**Developer:** Valoneu#8617 on Discord
