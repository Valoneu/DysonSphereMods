import os
import shutil
import subprocess
import glob
import zipfile
import json
import re

# Configuration
SRC_DIR = "src"
ASSETS_DIR = "zip"
FINAL_DIR = "final"

# Mods that had specific zip names different from their folder names
ZIP_NAME_MAP = {
    "FactoryMultiplier": "FactoryOverclock.zip",
    "HydrogenDissolution": "Hydrogen dissolution.zip"
}

# Map Folder Name -> Version Key in versions.json
# If key is different from folder name
VERSION_KEY_MAP = {
    "FactoryMultiplier": "FactoryOverclock",
    "DSPFactorySpaceStations": "FactorySpaceStation",
    "LessVesselPower": "LessShipPower"
}

# DLLs to exclude from the zip (system/game libraries)
EXCLUDE_DLLS = {
    "bepinex.dll", "0harmony.dll", "unityengine.dll", "unityengine.coremodule.dll", 
    "assembly-csharp.dll", "system.dll", "mscorlib.dll"
}

def clean_and_create_dir(path):
    if os.path.exists(path):
        shutil.rmtree(path)
    os.makedirs(path)

def update_versions():
    print("Updating versions from versions.json...")
    
    if not os.path.exists("versions.json"):
        print("  [Warning] versions.json not found. Skipping version update.")
        return

    with open("versions.json", "r") as f:
        versions = json.load(f)

    for mod_folder in os.listdir(SRC_DIR):
        mod_path = os.path.join(SRC_DIR, mod_folder)
        if not os.path.isdir(mod_path) or mod_folder == "Shared":
            continue

        # Determine the key to use for looking up the version
        version_key = VERSION_KEY_MAP.get(mod_folder, mod_folder)
        
        if version_key not in versions:
            print(f"  [Info] No version found for {mod_folder} (key: {version_key}) in versions.json.")
            continue
        
        new_version = versions[version_key]
        print(f"  Updating {mod_folder} to {new_version}...")

        # 1. Update .csproj
        csproj_files = glob.glob(os.path.join(mod_path, "*.csproj"))
        if csproj_files:
            csproj_path = csproj_files[0]
            with open(csproj_path, "r", encoding="utf-8") as f:
                content = f.read()
            
            # Regex to replace <Version>1.0.0</Version>
            # Handles <Version>...</Version> and <BepInExPluginVersion>...</BepInExPluginVersion>
            content = re.sub(r"<Version>.*?</Version>", f"<Version>{new_version}</Version>", content)
            content = re.sub(r"<BepInExPluginVersion>.*?</BepInExPluginVersion>", f"<BepInExPluginVersion>{new_version}</BepInExPluginVersion>", content)

            with open(csproj_path, "w", encoding="utf-8") as f:
                f.write(content)
        
        # 2. Update manifest.json in zip/ folder
        # We need to find the manifest file which might have a prefix like "ModName_manifest.json"
        manifest_pattern = os.path.join(ASSETS_DIR, f"{mod_folder}_manifest.json")
        manifests = glob.glob(manifest_pattern)
        
        # Also try exact match if prefix lookup failed (though we standardized on prefixes)
        if not manifests:
             manifests = glob.glob(os.path.join(ASSETS_DIR, "manifest.json"))

        for manifest_path in manifests:
            try:
                with open(manifest_path, "r", encoding="utf-8") as f:
                    manifest_data = json.load(f)
                
                manifest_data["version_number"] = new_version
                
                with open(manifest_path, "w", encoding="utf-8") as f:
                    json.dump(manifest_data, f, indent=4)
            except Exception as e:
                print(f"  [Error] Failed to update manifest {manifest_path}: {e}")

def run_build():
    print("Building solution in Release mode...")
    try:
        subprocess.check_call(["dotnet", "build", "DysonSphereMods.slnx", "-c", "Release"])
    except subprocess.CalledProcessError as e:
        print("Build failed!")
        exit(1)

def package_mod(mod_folder_name):
    # Skip the Shared library project itself, it's not a mod to be zipped alone
    if mod_folder_name == "Shared":
        return

    print(f"Packaging {mod_folder_name}...")

    # Determine output zip name
    zip_filename = ZIP_NAME_MAP.get(mod_folder_name, f"{mod_folder_name}.zip")
    zip_path = os.path.join(FINAL_DIR, zip_filename)

    # Temporary directory for staging this mod's zip content
    stage_dir = os.path.join("temp_stage", mod_folder_name)
    clean_and_create_dir(stage_dir)

    # 1. Find and Copy Compiled DLLs
    # Look in bin/Release/netstandard2.1/ (or similar)
    bin_dir = os.path.join(SRC_DIR, mod_folder_name, "bin", "Release")
    
    found_dll = False
    if os.path.exists(bin_dir):
        for root, dirs, files in os.walk(bin_dir):
            for file in files:
                if file.endswith(".dll") and file.lower() not in EXCLUDE_DLLS:
                    src_dll = os.path.join(root, file)
                    dst_dll = os.path.join(stage_dir, file)
                    shutil.copy2(src_dll, dst_dll)
                    found_dll = True
    
    if not found_dll:
        print(f"  [Warning] No compiled DLL found for {mod_folder_name}. Did the build succeed?")

    # 2. Find and Copy Assets from 'zip' folder
    # We look for files starting with "{mod_folder_name}_"
    prefix = f"{mod_folder_name}_"
    
    # Use glob to find all matching files/folders in the zip directory
    asset_pattern = os.path.join(ASSETS_DIR, f"{prefix}*")
    assets = glob.glob(asset_pattern)

    for asset_path in assets:
        filename = os.path.basename(asset_path)
        # Strip the prefix to restore original name (e.g. "CustomWarpSound_manifest.json" -> "manifest.json")
        original_name = filename[len(prefix):]
        
        dest_path = os.path.join(stage_dir, original_name)
        
        if os.path.isdir(asset_path):
            if os.path.exists(dest_path): shutil.rmtree(dest_path)
            shutil.copytree(asset_path, dest_path)
        else:
            shutil.copy2(asset_path, dest_path)

    # 3. Create the Zip file
    with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zf:
        for root, dirs, files in os.walk(stage_dir):
            for file in files:
                file_path = os.path.join(root, file)
                # Create relative path for inside the zip
                arcname = os.path.relpath(file_path, stage_dir)
                zf.write(file_path, arcname)
    
    print(f"  -> Created {zip_path}")

def main():
    clean_and_create_dir(FINAL_DIR)
    
    update_versions()
    run_build()

    # Iterate over directories in src
    if os.path.exists(SRC_DIR):
        for item in os.listdir(SRC_DIR):
            item_path = os.path.join(SRC_DIR, item)
            # Check if it is a directory and not a hidden folder (like .vs)
            if os.path.isdir(item_path) and not item.startswith("."):
                package_mod(item)
    
    # Cleanup staging directory
    if os.path.exists("temp_stage"):
        shutil.rmtree("temp_stage")
    
    print("All operations complete.")

if __name__ == "__main__":
    main()
