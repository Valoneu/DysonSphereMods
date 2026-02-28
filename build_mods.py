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
        "desc": "Activates high-performance rendering for beautiful, dynamic logistics vessel travel trails inside the Star Map. Gives you a live, visual heartbeat of your entire galactic logistics web moving between stars. Always records 60 minutes of data with a display-only slider, and persists trail data across save/load.",
        "config": "- `HistoryMinutes`: Display range slider (1-60 minutes). Data always records full 60 minutes.\n- Configurable toggles for trail rendering, thickness, color mode, and alpha fade.",
        "tech": "Intersects standard UI rendering pools, safely pushing visual vertex elements directly into the 3D Star Map canvas layer. Trail data persists via `.vesseltrails` sidecar files alongside game saves.",
        "deps": "BepInEx, CommonAPI"
    },
    "AdvancedPump": {
        "desc": "Repurposes Advanced Miners (Vein Collectors) into high-capacity planet pumps. When placed anywhere on a planet with a fluid (water, acid), they extract it at an extreme rate (150,000 speed). Completely removes the floating item icon for a cleaner look.",
        "config": "N/A (Operates automatically on placement).",
        "tech": "Hooks into `onFactoryFrameEnd` to scan Vein Collectors every 30 ticks; forces `EMinerType.Water` and clears `entitySignPool` icons for advanced miners with zero veins. Overrides `BuildTool_Click` to allow placement on any terrain.",
        "deps": "BepInEx"
    },
    "HideWarnings": {
        "desc": "Selectively hide disruptive in-game warnings with a dedicated configuration UI. Supports hiding power failure, resource shortages, sorter jams, and even the research completion banner.",
        "config": "- `Hotkey`: Default `Keypad5` to toggle the filter window.\n- Toggles available for: Power, Veins, Sorters, Damage, Dashboard, and Tech.",
        "tech": "Postfixes `WarningSystem.WarningLogic` to zero out hidden signal counts. Prefixes `UIGeneralTips.OnTechUnlocked` to safely suppress UI popups.",
        "deps": "BepInEx, CommonAPI"
    },
    "DistributeSpray": {
        "desc": "Eliminates the need for spray coaters and belts by automatically applying proliferator spray to items as they enter machines planet-wide, drawing from any station storage with proliferator available. Station slot needs to be set to 'Storage' mode.",
        "config": "- `ModEnabled`: Master toggle in BepInEx config.",
        "tech": "Implements an optimized debt-based credit system. Intercepts `PlanetFactory.InsertInto` to apply spray immediately, then reconciles the material cost from station storage on a background task every frame.",
        "deps": "BepInEx"
    },
    "CopyPasteStations": {
        "desc": "Seamlessly integrates with the game's native building copy/paste system to transfer detailed station configurations. Automatically refills missing drones and ships from your inventory during the paste operation.",
        "config": "Uses native Copy (`Shift+C`) and Paste (`Shift+V`) commands.",
        "tech": "Hooks `PlanetFactory.CopyBuildingSetting/PasteBuildingSetting` to serialize/deserialize `StationComponent` state. Includes logic to verify and transfer inventory drones/ships into the target station on-demand.",
        "deps": "BepInEx"
    },
    "StationLaunchSystem": {
        "desc": "Automated, high-performance Dyson construction system. Launches rockets and injects solar sails directly into Dyson shells from stations set to 'Storage' mode. Features equatorial priority and strict construction sequencing.",
        "config": "- `RocketsPerTick`: Max rockets per tick per station.\n- `SailsPerTick`: Max solar sails per tick per station.",
        "tech": "Uses a 120-tick shell cache and 10-tick batch processing to minimize UPS lag. Sorting logic prioritizing absolute Y-coordinates (equator). Restores missing Dyson Sphere statistics for direct injections.",
        "deps": "BepInEx"
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
**Game Version:** V0.10.34.28455
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
        if os.path.isdir(os.path.join(SRC_DIR, item)) and item != "Shared" and not item.startswith("."):
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
    "InfinityTechnologies": ["xiaoye97-BepInEx-5.4.17", "CommonAPI-CommonAPI-1.6.5"],
    "LessShipPower": ["xiaoye97-BepInEx-5.4.17"],
    "MaxLVLIncrease": ["xiaoye97-BepInEx-5.4.17", "CommonAPI-CommonAPI-1.6.5", "xiaoye97-LDBTool-3.0.2"],
    "PinnedNamesEverywhere": ["xiaoye97-BepInEx-5.4.17"],
    "PlanetMinerFast": ["xiaoye97-BepInEx-5.4.17"],
    "AdvancedPump": ["xiaoye97-BepInEx-5.4.17"],
    "SortByStorage": ["xiaoye97-BepInEx-5.4.17", "CommonAPI-CommonAPI-1.6.5"],
    "SpaciousStations": ["xiaoye97-BepInEx-5.4.17", "CommonAPI-CommonAPI-1.6.5", "xiaoye97-LDBTool-3.0.2"],
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
