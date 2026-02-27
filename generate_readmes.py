import os

mods = {
    "BottleneckUI": {
        "desc": "Adds a comprehensive UI panel listing all crafting machines across the planet that are currently lacking input items or suffering from power shortages, allowing you to instantly identify factory bottlenecks.",
        "config": "- `Hotkey`: Default `LeftControl + B` to toggle the Bottleneck UI.\n- All settings save directly to BepInEx `.cfg`.",
        "tech": "Uses Harmony prefixes on machine simulation elements to collect state data, injecting a custom Unity GUI overlay built on the native DSP window framework.",
        "deps": "BepInEx, CommonAPI"
    },
    "CloserStations": {
        "desc": "Significantly reduces the minimum placement distance required between logistics stations, allowing you to pack Planetary and Interstellar Logistics Stations tightly together to form compact super-hubs.",
        "config": "- `MinStationDistance`: Float configurable value dictating the required placement radius between stations.",
        "tech": "Transpiles the game's internal `BuildTool_Click` and placement validation methods to forcefully override the planetary distance checks for station entities.",
        "deps": "BepInEx"
    },
    "DistributeWarpers": {
        "desc": "Automates the balancing of Space Warpers across an entire planet. Instead of manually belting warpers to every station, this mod aggregates warpers and automatically inserts them into the internal warper slots of any station requesting them.",
        "config": "- `TargetCount`: Default `50`. The number of warpers to maintain in a station's internal slot.",
        "tech": "Subscribes to `TickManager`'s slow-tick event (every 60 frames) to scan planetary logistics pools and silently transfer items without triggering expensive physics updates in the main game loop.",
        "deps": "BepInEx"
    },
    "FactoryOverclock": {
        "desc": "Provides deep tools to multiply the production speed of machines (Assemblers, Smelters, Miners, Labs, Ejectors, Silos) while dynamically scaling power requirements and circumventing the 3600 items/minute station throughput and fractionator limitations.",
        "config": "- `MultiplierEnabled`: Toggle the mod entirely (Hotkey: `KeypadMinus`).\n- `AssemblerMultiplier`, `MinerMultiplier`, `StationMultiplier`, etc.: Defines exact scaling integers.\n- `DrawMultiplier`: Power consumption scaling factor.",
        "tech": "Uses Harmony prefixes/postfixes to double-tick logistical operations like fractionators and splitters, while pre-calculating modified arrays for component work-energy logic. Zero runtime reflection.",
        "deps": "BepInEx, CommonAPI"
    },
    "FarZoom": {
        "desc": "Dramatically expands the camera's zoom parameters, allowing you to zoom significantly further away from the mech in planetary view, and expanding the Starmap zoom capabilities for a vastly better galaxy overview.",
        "config": "- `MaxZoomDistance`: Configuration for the maximum planetary camera distance.\n- `StarmapZoom`: Configuration for interstellar camera bounds.",
        "tech": "Patches the game's `PlayerCamera` and `UIStarmap` camera clamp logic, modifying maximum distance floating-point values before the camera matrix constraints are applied.",
        "deps": "BepInEx"
    },
    "InfinityTechnologies": {
        "desc": "Allows for more granular, balanced control over the infinite research levels by recalculating tech requirements, ensuring late-game research paths scale infinitely efficiently.",
        "config": "- Customizable technology multipliers and toggle overrides via BepInEx config manager.",
        "tech": "Utilizes fast MSIL Delegates (`AccessTools.FieldRefAccess`) to bypass standard reflection overhead during Dyson Sphere power updates and technology node recalculations to ensure high UPS.",
        "deps": "BepInEx"
    },
    "LessShipPower": {
        "desc": "Optimizes and reduces the massive energy blocks required for Interstellar Logistics Vessels to initiate hyperspace warps, allowing earlier game interstellar transport without collapsing local energy grids.",
        "config": "- `VesselEnergyScale`: Float multiplier (Default `0.25`) that scales back the megawatt cost of interstellar travel.",
        "tech": "Executes a rapid Harmony Postfix on `StationComponent.CalcTripEnergyCost`, intercepting the internal 64-bit integer mathematics block and scaling it down securely.",
        "deps": "BepInEx"
    },
    "MaxLVLIncrease": {
        "desc": "Strips out the arbitrary level caps injected by late-game infinite research technologies, allowing all repeating tech (like Vein Utilization and Drone Speed) to scale literally forever.",
        "config": "- `MaxLevel`: Default `99999` (or infinite). Standard configuration cap variable.",
        "tech": "Overwrites the game's native tech-tree maximum definitions during initialization by patching the proto database immediately after the localized dictionary load sequence.",
        "deps": "BepInEx"
    },
    "PinnedNamesEverywhere": {
        "desc": "Forces pinned star and planet names alongside their distances to remain globally visible on your screen, even when you are lightyears away on the opposite side of the cluster.",
        "config": "- `Alpha`: Configuration for GUI text transparency levels.\n- `Visibility`: Toggle distance culling bounds.",
        "tech": "Replaces logic inside `UIStarmap` and localized GUI renderers. Uses an optimized cache mechanism to limit string allocations while rendering external 3D space text onto the 2D UI plane.",
        "deps": "BepInEx"
    },
    "PlanetMinerFast": {
        "desc": "An aggressively optimized fork of PlanetMiner. It completely eliminates the need for mining machines, pulling ores directly into associated Logistics Stations set to 'Local Demand'. Practically zero UPS and GPU impact.",
        "config": "- Miner activation configuration inside Station context menus.",
        "tech": "Compiles Weaver compatibility validation checks into `FastInvokeHandler` delegates to completely skip standard slow C# reflection overhead during every fast DSP tick.",
        "deps": "BepInEx"
    },
    "SortByStorage": {
        "desc": "Injects a highly requested 'Storage' sorting option natively into the Production Statistics UI panel, letting you sort the entire planetary or galactic network by the total quantity of stored items.",
        "config": "- Operates natively inside the Production screen. No configs required.",
        "tech": "Hooks into the game's internal `UIProductionStatWindow` UI routines via Harmony transpilation, injecting custom sorting parameter logic and rendering GUI list elements transparently.",
        "deps": "BepInEx"
    },
    "SpaceTargetEverything": {
        "desc": "Improves standard targeting logic for space vessels, allowing idle space fleets to acquire and bombard alternative active ground-based Dark Fog elements instead of wandering randomly and waiting.",
        "config": "- Target priority tuning via configuration menu integration.",
        "tech": "Overhauls fleet targeting sweeps through precise Harmony Pre-Fixing, redirecting raycasts downward into localized planetary grids rather than solely checking spherical deep-space targets.",
        "deps": "BepInEx"
    },
    "SpaciousStations": {
        "desc": "Dramatically scales up every metric for logistics stations. Specifically multiplies the maximum allowed drones, interstellar vessels, storage capacity numbers, and station charging power limits seamlessly.",
        "config": "- `CapacityMultiplier`: Multiplies total item slots.\n- `DroneShipMultiplier`: Fleet scaling multiplier.\n- `ChargeMultiplier`: Power limit scaling.",
        "tech": "Overrides property injection for Station component proto setups utilizing internal DSP native arrays. Evaluates edge logic loops to prevent overflow issues on rendering limits.",
        "deps": "BepInEx"
    },
    "StacksizeMultiplier": {
        "desc": "Provides a massive, globally customizable stack size multiplier to all game items, saving massive amounts of inventory and storage space. Includes a dedicated UI panel for live fine-tuning.",
        "config": "- `Hotkey`: Default `LeftAlt + S` to open the Stack Size adjustment UI panel in-game.\n- `GlobalMultiplier`: Multiplies all items globally on load.\n- Contains unique save states for individual custom item overrides.",
        "tech": "Implements a custom `StacksizeMultiplierWindow` completely inheriting from native `WindowBase` to ensure seamless scrolling/interaction with the native Canvas DSP pipeline.",
        "deps": "BepInEx, CommonAPI"
    },
    "TechHashReduce": {
        "desc": "Injects a dynamic scaling mechanism for technology research costs (Universal Matrix Hashes), drastically reducing the astronomical matrix requirements typically found deep in very late-game repeating technologies.",
        "config": "- `HashScale`: Float multiplier adjusting and reducing global tech hash costs universally.",
        "tech": "Patches the technology prototype database at runtime immediately upon load, modifying `hashNeeded` mathematically against predefined tier limits without breaking the tech-tree UI rendering formats.",
        "deps": "BepInEx"
    },
    "VesselTrails": {
        "desc": "Activates high-performance rendering for beautiful, dynamic logistics vessel travel trails inside the Star Map. Gives you a live, visual heartbeat of your entire galactic logistics web moving between stars.",
        "config": "- Configurable toggles for trail rendering limits, segment length, and alpha fade timing.",
        "tech": "Intersects standard UI rendering pools, safely pushing visual vertex elements directly into the 3D Star Map canvas layer using the game's ultra-fast native object pooling methods.",
        "deps": "BepInEx"
    }
}

template = """# {name}

{desc}

---

## Technical Information

### Mechanics and Configuration
{config}

### Deep Technical Details
{tech}

### Dependencies
* **{deps}**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
"""

def generate_docs():
    for mod, data in mods.items():
        path = f"src/{mod}/README.md"
        if os.path.exists(f"src/{mod}"):
            content = template.format(
                name=mod,
                desc=data['desc'],
                config=data['config'],
                tech=data['tech'],
                deps=data['deps']
            )
            with open(path, "w", encoding="utf-8") as f:
                f.write(content)
            print(f"Generated clean detailed documentation for {mod}")

if __name__ == "__main__":
    generate_docs()
    print("All READMEs have been overhauled.")
