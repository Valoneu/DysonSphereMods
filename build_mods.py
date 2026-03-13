import os
import shutil
import subprocess
import glob
import zipfile
import json
import re
import sys

SRC_DIR = "src"
ASSETS_DIR = "zip"
FINAL_DIR = "final"
TEMP_STAGE = "temp_stage"

EXCLUDE_DLLS = {
    "bepinex.dll", "0harmony.dll", "unityengine.dll", "unityengine.coremodule.dll",
    "assembly-csharp.dll", "system.dll", "mscorlib.dll"
}

MOD_README_DEFINITIONS = {
    "AdvancedPump": {
        "desc": "Repurposes Advanced Miners (Vein Collectors) into high-capacity planet pumps. When placed anywhere on a planet with a fluid (water, acid), they extract it at an extreme rate (150,000 speed).",
        "config": "N/A (Operates automatically on placement).",
        "tech": "Hooks into `onFactoryFrameEnd` to scan Vein Collectors every 30 ticks; forces `EMinerType.Water` and clears `entitySignPool` icons for advanced miners with zero veins. Overrides `BuildTool_Click` to allow placement on any terrain.",
        "deps": "BepInEx"
    },
    "BottleneckUI": {
        "desc": "Adds a comprehensive UI panel listing all crafting machines across the planet that are currently lacking input items or suffering from power shortages. Features real-time scanning and sorting to help you kill factory bottlenecks instantly.",
        "config": "- `Hotkey`: Default `LeftControl + B` to toggle the Bottleneck UI.\n- Filter by machine type, power status, or inventory state.",
        "tech": "Uses Harmony patches to collect machine states during simulation, injecting a custom Unity GUI overlay into the native DSP window framework with optimized list virtualization.",
        "deps": "BepInEx, CommonAPI"
    },
    "CloserStations": {
        "desc": "Reduces the minimum placement distance required between logistics stations, allowing you to pack Planetary and Interstellar Logistics Stations tightly together.",
        "config": "- `DistanceMultiplier`: Default `0.75`. Multiplier for the minimum placement radius.",
        "tech": "Transpiles `BuildTool_Click.CheckBuildConditions` and `BuildTool_BlueprintPaste.CheckBuildConditions` to scale down the magic distance constants used for station-to-station collision.",
        "deps": "BepInEx"
    },
    "CopyPasteStations": {
        "desc": "Seamlessly integrates with the game's native building copy/paste system to transfer detailed station configurations. Automatically refills missing drones and ships from your inventory during the paste operation.",
        "config": "Uses native Copy (`Shift+C`) and Paste (`Shift+V`) commands.",
        "tech": "Hooks `PlanetFactory.CopyBuildingSetting` and `PasteBuildingSetting` to serialize/deserialize `StationComponent` states. Logic verifies and transfers inventory drones/ships into the target station on-demand.",
        "deps": "BepInEx"
    },
    "DistributeSpray": {
        "desc": "Eliminates the need for spray coaters and belts by automatically applying proliferator spray to items as they enter machines planet-wide, drawing from any station storage with proliferator available.",
        "config": "- `ModEnabled`: Master toggle in BepInEx config.\n- Station slot must be set to 'Storage' (None) mode in the local logistics settings.",
        "tech": "Implements an optimized credit-based system. Intercepts `PlanetFactory.InsertInto` to apply spray immediately, then reconciles the material cost from station storage on a background task.",
        "deps": "BepInEx"
    },
    "DistributeWarpers": {
        "desc": "Automates Space Warper distribution across a planet. Aggregates warpers from any station storage and automatically refills the internal warper slots of all Interstellar Logistics Stations (ILS) on that planet.",
        "config": "- `TargetCount`: Default `50`. The number of warpers to maintain in a station's internal slot.\n- `CheckInterval`: How often (in ticks) to check and distribute.",
        "tech": "Subscribes to `onFactoryFrameEnd` to scan planetary logistics pools and transfer items without triggering expensive physics updates or breaking vanilla statistics.",
        "deps": "BepInEx"
    },
    "FactoryOverclock": {
        "desc": "Global throughput and speed multiplier for your entire factory. Scales production speeds for Assemblers, Smelters, Miners, Labs, and even Silos/Ejectors while dynamically scaling power requirements to balanced levels.",
        "config": "- `Hotkey`: `KeypadMinus` to toggle overclocking.\n- Independent multipliers for all machine types (Assemblers, Belts, Sorters, Silos, etc.).",
        "tech": "Uses Harmony transpilers to double-tick logistics (splitters/pilers) and pre-calculates component work-energy logic to ensure high performance even at 20x speed.",
        "deps": "BepInEx, CommonAPI"
    },
    "FarZoom": {
        "desc": "Dramatically expands the camera's zoom parameters and adds Field of View (FOV) controls. Zoom out lightyears in the star map or enjoy wide-angle planetary views.",
        "config": "- `Shift + Scroll`: Change Field of View (FOV).\n- `ZoomMultiplier`: Extends maximum zoom distance.\n- `ZoomSpeedMultiplier`: Adjusts camera zoom sensitivity.",
        "tech": "Patches `GameCamera`, `RTSPoser`, and `PlanetPoser` calculation logic, modifying FOV and distance clamp values before the camera matrix constraints are applied.",
        "deps": "BepInEx"
    },
    "HideWarnings": {
        "desc": "Selectively hide disruptive in-game warnings with a dedicated configuration UI. Supports hiding power failure, resource shortages, sorter jams, and even the research completion banner.",
        "config": "- `Hotkey`: Default `Keypad5` to toggle the filter window.\n- Toggles for: Power, Veins, Sorters, Damage, Dashboard, and Tech.",
        "tech": "Postfixes `WarningSystem.WarningLogic` to zero out hidden signal counts. Prefixes `UIGeneralTips.OnTechUnlocked` to safely suppress UI research popups.",
        "deps": "BepInEx, CommonAPI"
    },
    "InfinityTechnologies": {
        "desc": "Expands the late-game by adding 6 new custom infinite technologies to the research tree: Infinite Inventory, Wireless Power Boost, Dyson Sphere Efficiency, Proliferator Enhancement, Logistics Combat Fire Rate, and Research Productivity.",
        "config": "- Customizable technology multipliers and toggle overrides via BepInEx config manager.",
        "tech": "Utilizes a `ModifierManager` to recalculate game multipliers based on tech states. Uses Harmony patches on Dyson Sphere, Lab, and Combat logic to apply these dynamic bonuses.",
        "deps": "BepInEx"
    },
    "LessShipPower": {
        "desc": "Reduces the massive energy required for Interstellar Logistics Vessels to initiate trips, allowing easier interstellar transport without collapsing local energy grids in the mid-game.",
        "config": "- `VesselEnergyScale`: Multiplier (Default `0.25`) that scales back the energy cost of vessel travel.",
        "tech": "Applies a Harmony Postfix on `StationComponent.CalcTripEnergyCost`, intercepting the internal cost calculation and scaling it down before applying the drain.",
        "deps": "BepInEx"
    },
    "MaxLVLIncrease": {
        "desc": "Extends the level caps for infinite research technologies (like Vein Utilization and Drone Speed), allowing them to scale past vanilla limits.",
        "config": "- `MaxLevelValue`: Sets the new max level (Default `50,000`).",
        "tech": "Overwrites the game's native tech-tree maximum definitions during initialization by patching the prototype database and ensuring tech states are migrated correctly on save load.",
        "deps": "BepInEx"
    },
    "PinnedNamesEverywhere": {
        "desc": "Ensures pinned star and planet names (and distances) remain globally visible on your screen, regardless of camera distance or screen position.",
        "config": "- `PinnedNamesMinimumAlpha`: Ensures names remain readable even when far away.\n- `AlwaysShowPinnedDistances`: Optional toggle for distance persistence.",
        "tech": "Patches `UISpaceGuideEntry` to force text visibility and `UISpaceGuide.ClipEntryPool` to ensure pinned objects bypass the game's standard UI culling logic.",
        "deps": "BepInEx"
    },
    "PlanetMinerFast": {
        "desc": "An aggressively optimized mining system that pulls ores directly into Logistics Stations set to 'Local Demand'. Eliminates the need for miners, belts, and power lines over veins with minimal UPS impact.",
        "config": "Set a station slot to 'Local Demand' for the target ore to start mining planet-wide.",
        "tech": "Uses an optimized `PlanetVeinCache` (rebuilt on changes) to identify veins and directly inject items into station storage. Consumes 20MJ per operation from the station's energy pool.",
        "deps": "BepInEx"
    },
    "SortByStorage": {
        "desc": "Adds 'Stored Descending' and 'Stored Ascending' sorting options natively into the Production Statistics UI panel, letting you sort items by their total stored quantity across the current scope.",
        "config": "Select the new options in the sorting dropdown within the Production Statistics window.",
        "tech": "Hooks into `UIStatisticsWindow` routines to refresh item storage counts and apply a custom quicksort algorithm to the UI list elements.",
        "deps": "BepInEx"
    },
    "SpaciousStations": {
        "desc": "Comprehensive logistics station upgrade. Multiplies storage, drone/vessel capacity, and charging speed. Optimized drone dispatch prioritizes the closest available station.",
        "config": "- Extensive multipliers for PLS, ILS, and Exchange Stations.\n- `ShipReleasePerTick`: Controls how many ships launch per tick.\n- `DroneTaskInterval`: Controls how fast drones are dispatched.",
        "tech": "Modifies station prototypes via Harmony patches on `VFPreload`. Implements a distance-based comparer for `RematchLocalPairs` to ensure drones always fly the shortest path.",
        "deps": "BepInEx, CommonAPI"
    },
    "StacksizeMultiplier": {
        "desc": "Globally customizable stack size multiplier for all items. Includes a dedicated in-game UI for fine-tuning individual item stacks and saving separate overrides.",
        "config": "- `Hotkey`: Default `NumPad 2` to toggle the adjustment UI.\n- Separate global multipliers for Items, Buildings, and Drones/Vessels.",
        "tech": "Implements a custom `StacksizeMultiplierWindow` inheriting from native `WindowBase`. Directly modifies `ItemProto` stack sizes and updates the player's package in real-time.",
        "deps": "BepInEx, CommonAPI"
    },
    "StationLaunchSystem": {
        "desc": "Automated, high-performance Dyson construction system. Launches rockets and injects solar sails directly into Dyson shells from stations set to 'Storage' mode. Features equatorial priority.",
        "config": "- `RocketsPerTick`: Max rockets per tick per station.\n- `SailsPerTick`: Max solar sails per tick per station.",
        "tech": "Uses a 120-tick shell cache and 10-tick batch processing to minimize UPS lag. Prioritizes shell construction starting from the equator (absolute Y-coordinate).",
        "deps": "BepInEx"
    },
    "TechHashReduce": {
        "desc": "Allows scaling the hash requirement (cost) for research. Speed up your progression or add a challenge by adjusting the universal research investment required for all technologies.",
        "config": "- `HashrateScale`: Multiplier for technology hash requirements. Below 1.0 is cheaper; above 1.0 is more expensive.",
        "tech": "Patches `TechProto.GetHashNeeded` to apply the scale and `GameHistoryData.Import` to ensure save games remain consistent across requirement changes.",
        "deps": "BepInEx"
    },
    "VesselTrails": {
        "desc": "Visualizes galactic logistics routes with dynamic 3D travel trails. See your entire logistics web moving in real-time or as a heatmap of traffic volume. Data persists across sessions.",
        "config": "- `NumPad 1`: Toggle Logistics UI. `NumPad 3`: Toggle trail lines.\n- Configurable opacity, thickness, and color modes (Material vs Heatmap).",
        "tech": "Uses custom GL rendering to draw trails in 3D space. Tracks vessel history in a background manager and saves traffic data in `.vesseltrails` files alongside game saves.",
        "deps": "BepInEx, CommonAPI"
    }
}

README_TEMPLATE = """# {name}

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
**Game Version:** V0.10.34.28485
**Developer:** Valoneu
"""

# ============================================================================
# Utilities
# ============================================================================

def fatal(msg):
    print(f"\n[FATAL] {msg}")
    sys.exit(1)

def clean_and_create_dir(path):
    if os.path.exists(path):
        shutil.rmtree(path)
    os.makedirs(path)

def get_mod_folders():
    mods = []
    for item in os.listdir(SRC_DIR):
        if os.path.isdir(os.path.join(SRC_DIR, item)) and item not in ["Shared", "SeedScanner"] and not item.startswith("."):
            mods.append(item)
    return sorted(mods)

# ============================================================================
# Validation (runs first, fails fast)
# ============================================================================

def validate_all(mod_folders, versions):
    print("Validating project integrity...")
    errors = []

    for mod in mod_folders:
        if mod not in versions:
            errors.append(f"  {mod}: missing from versions.json")
        if not os.path.exists(os.path.join(ASSETS_DIR, f"{mod}_icon.png")):
            errors.append(f"  {mod}: missing {ASSETS_DIR}/{mod}_icon.png")
        if mod not in MOD_README_DEFINITIONS:
            errors.append(f"  {mod}: missing README definition in MOD_README_DEFINITIONS")
        if mod not in MOD_MANIFEST_DEPENDENCIES:
            errors.append(f"  {mod}: missing dependencies in MOD_MANIFEST_DEPENDENCIES")

    if errors:
        fatal("Validation failed:\n" + "\n".join(errors))

    print(f"  All {len(mod_folders)} mods validated OK.")

# ============================================================================
# C# Cleanup (strip comments and empty lines from source)
# ============================================================================

def cleanup_cs_sources():
    print("Cleaning C# sources...")
    comment_pattern = r'//.*|/\*[\s\S]*?\*/|("(?:\\.|[^\\"])*")'

    def replacer(match):
        s = match.group(0)
        return "" if s.startswith('/') else s

    count = 0
    for root, dirs, files in os.walk(SRC_DIR):
        if 'obj' in dirs: dirs.remove('obj')
        if 'bin' in dirs: dirs.remove('bin')
        for file in files:
            if not file.endswith('.cs'):
                continue
            file_path = os.path.join(root, file)
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            cleaned = re.sub(comment_pattern, replacer, content)
            lines = [line for line in cleaned.splitlines() if line.strip()]
            cleaned = "\n".join(lines) + "\n"
            if content != cleaned:
                with open(file_path, 'w', encoding='utf-8') as f:
                    f.write(cleaned)
                count += 1
    print(f"  Cleaned {count} file(s).")

# ============================================================================
# README Generation (to both src/ and zip/)
# ============================================================================

def generate_readmes(mod_folders):
    print("Generating READMEs...")
    for mod in mod_folders:
        data = MOD_README_DEFINITIONS[mod]
        content = README_TEMPLATE.format(
            name=mod,
            desc=data['desc'],
            config=data['config'],
            tech=data['tech'],
            deps=data['deps']
        )
        src_path = os.path.join(SRC_DIR, mod, "README.md")
        zip_path = os.path.join(ASSETS_DIR, f"{mod}_README.md")
        with open(src_path, "w", encoding="utf-8") as f:
            f.write(content)
        with open(zip_path, "w", encoding="utf-8") as f:
            f.write(content)
    print(f"  Generated {len(mod_folders)} READMEs (src + zip).")

MOD_MANIFEST_DEPENDENCIES = {
    "BottleneckUI": ["xiaoye97-BepInEx-5.4.17", "CommonAPI-CommonAPI-1.6.5"],
    "CloserStations": ["xiaoye97-BepInEx-5.4.17"],
    "CopyPasteStations": ["xiaoye97-BepInEx-5.4.17", "CommonAPI-CommonAPI-1.6.5"],
    "DistributeSpray": ["xiaoye97-BepInEx-5.4.17"],
    "DistributeWarpers": ["xiaoye97-BepInEx-5.4.17"],
    "FactoryOverclock": ["xiaoye97-BepInEx-5.4.17", "CommonAPI-CommonAPI-1.6.5"],
    "FarZoom": ["xiaoye97-BepInEx-5.4.17"],
    "HideWarnings": ["xiaoye97-BepInEx-5.4.17", "CommonAPI-CommonAPI-1.6.5"],
    "InfinityTechnologies": ["xiaoye97-BepInEx-5.4.17"],
    "LessShipPower": ["xiaoye97-BepInEx-5.4.17"],
    "MaxLVLIncrease": ["xiaoye97-BepInEx-5.4.17"],
    "PinnedNamesEverywhere": ["xiaoye97-BepInEx-5.4.17"],
    "PlanetMinerFast": ["xiaoye97-BepInEx-5.4.17"],
    "AdvancedPump": ["xiaoye97-BepInEx-5.4.17"],
    "SortByStorage": ["xiaoye97-BepInEx-5.4.17"],
    "SpaciousStations": ["xiaoye97-BepInEx-5.4.17", "CommonAPI-CommonAPI-1.6.5"],
    "StacksizeMultiplier": ["xiaoye97-BepInEx-5.4.17", "CommonAPI-CommonAPI-1.6.5"],
    "StationLaunchSystem": ["xiaoye97-BepInEx-5.4.17"],
    "TechHashReduce": ["xiaoye97-BepInEx-5.4.17"],
    "VesselTrails": ["xiaoye97-BepInEx-5.4.17", "CommonAPI-CommonAPI-1.6.5"],
}

# ============================================================================
# Manifest Generation
# ============================================================================

def generate_manifests(mod_folders, versions):
    print("Generating manifests...")
    for mod in mod_folders:
        desc = MOD_README_DEFINITIONS[mod]["desc"]
        if len(desc) > 250:
            desc = desc[:247] + "..."
        manifest = {
            "name": mod,
            "version_number": versions.get(mod, "1.0.0"),
            "website_url": f"https://github.com/Valoneu/DysonSphereMods/tree/main/src/{mod}",
            "description": desc,
            "dependencies": MOD_MANIFEST_DEPENDENCIES.get(mod, ["xiaoye97-BepInEx-5.4.17"])
        }
        manifest_path = os.path.join(ASSETS_DIR, f"{mod}_manifest.json")
        with open(manifest_path, "w", encoding="utf-8") as f:
            json.dump(manifest, f, indent=4)
    print(f"  Generated {len(mod_folders)} manifests.")

# ============================================================================
# Version Updating
# ============================================================================

def update_csproj_version(csproj_path, new_version):
    with open(csproj_path, "r", encoding="utf-8") as f:
        content = f.read()
    updated = re.sub(r"<Version>[^<]*</Version>", f"<Version>{new_version}</Version>", content)
    updated = re.sub(r"<BepInExPluginVersion>[^<]*</BepInExPluginVersion>", f"<BepInExPluginVersion>{new_version}</BepInExPluginVersion>", updated)
    if content != updated:
        with open(csproj_path, "w", encoding="utf-8") as f:
            f.write(updated)
        return True
    return False

def update_manifest_version(manifest_path, new_version):
    with open(manifest_path, "r", encoding="utf-8") as f:
        manifest_data = json.load(f)
    if manifest_data.get("version_number") == new_version:
        return False
    manifest_data["version_number"] = new_version
    with open(manifest_path, "w", encoding="utf-8") as f:
        json.dump(manifest_data, f, indent=4)
    return True

def update_plugin_source_version(mod_path, new_version):
    pattern_const = r"([ \t]*public const string (?:MOD_VERSION|VERSION) = \")([^\"\\]+)(\";)"
    pattern_attr = r"(\[BepInPlugin\(\".*?\", \".*?\", \")([^\"\\]+)(\"\)\])"
    changed = False
    for file_path in glob.glob(os.path.join(mod_path, "*.cs")):
        with open(file_path, "r", encoding="utf-8") as f:
            content = f.read()
        content_new = content
        if re.search(pattern_const, content_new):
            content_new = re.sub(pattern_const, f"\\g<1>{new_version}\\g<3>", content_new)
        if re.search(pattern_attr, content_new):
            content_new = re.sub(pattern_attr, f"\\g<1>{new_version}\\g<3>", content_new)
        if content != content_new:
            with open(file_path, "w", encoding="utf-8") as f:
                f.write(content_new)
            changed = True
    return changed

def update_readme_version(file_path, new_version):
    if not os.path.exists(file_path):
        return False
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()
    content_new = content
    p1 = r"(^Version[:\s]+)(\S+)"
    p2 = r"(\| \*\*)(\S+)(\*\* \|)"
    if re.search(p1, content_new, flags=re.IGNORECASE | re.MULTILINE):
        content_new = re.sub(p1, f"\\g<1>{new_version}", content_new, flags=re.IGNORECASE | re.MULTILINE)
    if re.search(p2, content_new):
        content_new = re.sub(p2, f"\\g<1>{new_version}\\g<3>", content_new, count=1)
    if content != content_new:
        with open(file_path, "w", encoding="utf-8") as f:
            f.write(content_new)
        return True
    return False

def update_mod_versions(versions):
    updated_count = 0
    for mod_folder in os.listdir(SRC_DIR):
        mod_path = os.path.join(SRC_DIR, mod_folder)
        if not os.path.isdir(mod_path) or mod_folder == "Shared":
            continue
        if mod_folder not in versions:
            continue
        new_version = versions[mod_folder]
        changed = False
        csproj_files = glob.glob(os.path.join(mod_path, "*.csproj"))
        if csproj_files:
            changed |= update_csproj_version(csproj_files[0], new_version)
        manifest_path = os.path.join(ASSETS_DIR, f"{mod_folder}_manifest.json")
        if os.path.exists(manifest_path):
            changed |= update_manifest_version(manifest_path, new_version)
        changed |= update_readme_version(os.path.join(mod_path, "README.md"), new_version)
        for r in glob.glob(os.path.join(ASSETS_DIR, f"{mod_folder}_README.md")):
            changed |= update_readme_version(r, new_version)
        changed |= update_plugin_source_version(mod_path, new_version)
        if changed:
            print(f"  Updated {mod_folder} -> {new_version}")
            updated_count += 1
    if updated_count > 0:
        print(f"  {updated_count} mod(s) updated.")
    else:
        print("  All versions up to date.")

def update_root_readme(versions):
    print("Updating root README.md versions...")
    readme_path = "README.md"
    if not os.path.exists(readme_path):
        return
    with open(readme_path, "r", encoding="utf-8") as f:
        content = f.read()
    new_content = content
    for mod_name, version in versions.items():
        pattern = r"(\| \*\*" + re.escape(mod_name) + r"\*\* \|.*?\| )(v?)([^\s|]+)( \|)"
        if re.search(pattern, new_content):
            new_content = re.sub(pattern, f"\\g<1>\\g<2>{version}\\g<4>", new_content)
    if content != new_content:
        with open(readme_path, "w", encoding="utf-8") as f:
            f.write(new_content)
        print("  Root README.md updated.")
    else:
        print("  Root README.md is up to date.")

# ============================================================================
# Build & Package
# ============================================================================

def run_build():
    print("Building solution in Release mode...")
    try:
        subprocess.check_call(["dotnet", "build", "DysonSphereMods.slnx", "-c", "Release"])
    except subprocess.CalledProcessError:
        fatal("dotnet build failed!")

def collect_dlls(mod_folder_name, stage_dir):
    bin_dir = os.path.join(SRC_DIR, mod_folder_name, "bin", "Release")
    found_dll = False
    if os.path.exists(bin_dir):
        for root, dirs, files in os.walk(bin_dir):
            for file in files:
                if file.endswith(".dll") and file.lower() not in EXCLUDE_DLLS:
                    shutil.copy2(os.path.join(root, file), os.path.join(stage_dir, file))
                    found_dll = True
    return found_dll

def collect_assets(mod_folder_name, stage_dir):
    prefix = f"{mod_folder_name}_"
    for asset_path in glob.glob(os.path.join(ASSETS_DIR, f"{prefix}*")):
        filename = os.path.basename(asset_path)
        original_name = filename[len(prefix):]
        dest_path = os.path.join(stage_dir, original_name)
        if os.path.isdir(asset_path):
            if os.path.exists(dest_path): shutil.rmtree(dest_path)
            shutil.copytree(asset_path, dest_path)
        else:
            shutil.copy2(asset_path, dest_path)

def create_zip(zip_path, stage_dir):
    with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zf:
        for root, dirs, files in os.walk(stage_dir):
            for file in files:
                file_path = os.path.join(root, file)
                zf.write(file_path, os.path.relpath(file_path, stage_dir))

def package_mod(mod_folder_name):
    print(f"Packaging {mod_folder_name}...")
    stage_dir = os.path.join(TEMP_STAGE, mod_folder_name)
    clean_and_create_dir(stage_dir)
    if not collect_dlls(mod_folder_name, stage_dir):
        print(f"  [Warning] No compiled DLL found for {mod_folder_name}.")
    collect_assets(mod_folder_name, stage_dir)
    zip_path = os.path.join(FINAL_DIR, f"{mod_folder_name}.zip")
    create_zip(zip_path, stage_dir)
    print(f"  -> Created {zip_path}")

# ============================================================================
# Main
# ============================================================================

def run_step(name, func, *args):
    try:
        func(*args)
    except SystemExit:
        raise
    except Exception as e:
        fatal(f"{name} failed: {e}")

def main():
    versions = {}
    if os.path.exists("versions.json"):
        try:
            with open("versions.json", "r") as f:
                versions = json.load(f)
        except Exception as e:
            fatal(f"Failed to parse versions.json: {e}")
    else:
        fatal("versions.json not found!")

    mod_folders = get_mod_folders()
    if not mod_folders:
        fatal("No mod folders found in src/!")

    run_step("Validation", validate_all, mod_folders, versions)
    run_step("C# cleanup", cleanup_cs_sources)
    run_step("README generation", generate_readmes, mod_folders)
    run_step("Manifest generation", generate_manifests, mod_folders, versions)
    run_step("Version update", update_mod_versions, versions)
    run_step("Root README update", update_root_readme, versions)

    clean_and_create_dir(FINAL_DIR)
    run_step("Build", run_build)

    for mod in mod_folders:
        run_step(f"Packaging {mod}", package_mod, mod)

    if os.path.exists(TEMP_STAGE):
        shutil.rmtree(TEMP_STAGE)

    print(f"\nAll operations complete. {len(mod_folders)} mods packaged.")

if __name__ == "__main__":
    main()
