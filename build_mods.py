import os
import shutil
import subprocess
import glob
import zipfile
import json
import re

# --- Constants ---
SRC_DIR = "src"
ASSETS_DIR = "zip"
FINAL_DIR = "final"
TEMP_STAGE = "temp_stage"

ZIP_NAME_MAP = {
    "FactoryMultiplier": "FactoryOverclock.zip",
    "HydrogenDissolution": "HydrogenDissolution.zip"
}

VERSION_KEY_MAP = {
    "FactoryMultiplier": "FactoryOverclock",
    "DSPFactorySpaceStations": "FactorySpaceStation",
    "LessVesselPower": "LessShipPower"
}

EXCLUDE_DLLS = {
    "bepinex.dll", "0harmony.dll", "unityengine.dll", "unityengine.coremodule.dll", 
    "assembly-csharp.dll", "system.dll", "mscorlib.dll"
}

# --- Utilities ---

def clean_and_create_dir(path):
    if os.path.exists(path):
        shutil.rmtree(path)
    os.makedirs(path)

# --- Version Updating Functions ---

def update_csproj_version(csproj_path, new_version):
    try:
        with open(csproj_path, "r", encoding="utf-8") as f:
            content = f.read()
        
        content = re.sub(r"<Version>.*?</Version>", f"<Version>{new_version}</Version>", content)
        content = re.sub(r"<BepInExPluginVersion>.*?</BepInExPluginVersion>", f"<BepInExPluginVersion>{new_version}</BepInExPluginVersion>", content)

        with open(csproj_path, "w", encoding="utf-8") as f:
            f.write(content)
    except Exception as e:
        print(f"    [Error] Failed to update {csproj_path}: {e}")

def update_manifest_version(manifest_path, new_version):
    try:
        with open(manifest_path, "r", encoding="utf-8") as f:
            manifest_data = json.load(f)
        
        manifest_data["version_number"] = new_version
        
        with open(manifest_path, "w", encoding="utf-8") as f:
            json.dump(manifest_data, f, indent=4)
    except Exception as e:
        print(f"    [Error] Failed to update manifest {manifest_path}: {e}")

def update_plugin_source_version(mod_path, new_version):
    plugin_files = glob.glob(os.path.join(mod_path, "*Plugin.cs"))
    for file_path in plugin_files:
        try:
            with open(file_path, "r", encoding="utf-8") as f:
                content = f.read()
            
            # Match public const string MOD_VERSION or VERSION
            pattern_const = r"([ \t]*public const string (?:MOD_VERSION|VERSION) = \")(\d+\.\d+\.\d+)(\";)"
            # Match [BepInPlugin("GUID", "Name", "Version")]
            pattern_attr = r"(\[BepInPlugin\(\".*?\", \".*?\", \")(\d+\.\d+\.\d+)(\"\)\])"
            
            content_new = content
            if re.search(pattern_const, content_new):
                content_new = re.sub(pattern_const, f"\\g<1>{new_version}\\g<3>", content_new)
            if re.search(pattern_attr, content_new):
                content_new = re.sub(pattern_attr, f"\\g<1>{new_version}\\g<3>", content_new)

            if content != content_new:
                with open(file_path, "w", encoding="utf-8") as f:
                    f.write(content_new)
                print(f"    Updated version in {os.path.basename(file_path)}")
        except Exception as e:
            print(f"    [Error] Failed to update source version in {file_path}: {e}")

def update_readme_version(file_path, new_version):
    try:
        if not os.path.exists(file_path):
            return
        with open(file_path, "r", encoding="utf-8") as f:
            content = f.read()
        
        pattern1 = r"(^Version[:\s]+)(\d+\.\d+\.\d+)"
        pattern2 = r"(\| \*\*)(\d+\.\d+\.\d+)(\*\* \|)"
        
        content_new = content
        if re.search(pattern1, content_new, flags=re.IGNORECASE | re.MULTILINE):
            content_new = re.sub(pattern1, f"\\g<1>{new_version}", content_new, flags=re.IGNORECASE | re.MULTILINE)
        
        if re.search(pattern2, content_new):
            content_new = re.sub(pattern2, f"\\g<1>{new_version}\\g<3>", content_new, count=1)

        if content != content_new:
            with open(file_path, "w", encoding="utf-8") as f:
                f.write(content_new)
    except Exception as e:
        print(f"    [Error] Failed to update README {file_path}: {e}")

def update_mod_versions(versions):
    print("Updating versions from versions.json...")
    for mod_folder in os.listdir(SRC_DIR):
        mod_path = os.path.join(SRC_DIR, mod_folder)
        if not os.path.isdir(mod_path) or mod_folder == "Shared":
            continue

        version_key = VERSION_KEY_MAP.get(mod_folder, mod_folder)
        if version_key not in versions:
            print(f"  [Info] No version found for {mod_folder} (key: {version_key}) in versions.json.")
            continue
        
        new_version = versions[version_key]
        print(f"  Updating {mod_folder} to {new_version}...")

        # Update .csproj
        csproj_files = glob.glob(os.path.join(mod_path, "*.csproj"))
        if csproj_files:
            update_csproj_version(csproj_files[0], new_version)
        
        # Update Manifests
        manifest_pattern = os.path.join(ASSETS_DIR, f"{mod_folder}_manifest.json")
        manifests = glob.glob(manifest_pattern)
        if not manifests:
             manifests = glob.glob(os.path.join(ASSETS_DIR, "manifest.json"))
        for m in manifests:
            update_manifest_version(m, new_version)

        # Update READMEs
        update_readme_version(os.path.join(mod_path, "README.md"), new_version)
        for r in glob.glob(os.path.join(ASSETS_DIR, f"{mod_folder}_README.md")):
            update_readme_version(r, new_version)
        
        # Update Plugin Source
        update_plugin_source_version(mod_path, new_version)

def update_root_readme(versions):
    print("Updating root README.md versions...")
    readme_path = "README.md"
    if not os.path.exists(readme_path):
        print("  [Warning] Root README.md not found.")
        return

    try:
        with open(readme_path, "r", encoding="utf-8") as f:
            content = f.read()

        new_content = content
        for mod_name, version in versions.items():
            pattern = r"(\| \*\*"+ re.escape(mod_name) + r"\*\* \|.*?\| )(v?)(\d+\.\d+\.\d+)( \|)"
            if re.search(pattern, new_content):
                new_content = re.sub(pattern, f"\\g<1>\\g<2>{version}\\g<4>", new_content)
            else:
                 print(f"  [Info] Could not find table row for {mod_name} in README.md")

        if content != new_content:
            with open(readme_path, "w", encoding="utf-8") as f:
                f.write(new_content)
            print("  Root README.md updated.")
        else:
            print("  Root README.md is up to date.")
    except Exception as e:
        print(f"  [Error] Failed to update root README.md: {e}")

# --- Build Functions ---

def run_build():
    print("Building solution in Release mode...")
    try:
        subprocess.check_call(["dotnet", "build", "DysonSphereMods.slnx", "-c", "Release"])
    except subprocess.CalledProcessError as e:
        print("Build failed!")
        exit(1)

# --- Packaging Functions ---

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
    
    assets = glob.glob(os.path.join(ASSETS_DIR, f"{prefix}*"))
    for asset_path in assets:
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
    if mod_folder_name == "Shared":
        return

    print(f"Packaging {mod_folder_name}...")
    zip_filename = ZIP_NAME_MAP.get(mod_folder_name, f"{mod_folder_name}.zip")
    zip_path = os.path.join(FINAL_DIR, zip_filename)
    stage_dir = os.path.join(TEMP_STAGE, mod_folder_name)
    
    clean_and_create_dir(stage_dir)
    
    if not collect_dlls(mod_folder_name, stage_dir):
        print(f"  [Warning] No compiled DLL found for {mod_folder_name}.")

    collect_assets(mod_folder_name, stage_dir)
    create_zip(zip_path, stage_dir)
    print(f"  -> Created {zip_path}")

# --- Main Flow ---

def main():
    clean_and_create_dir(FINAL_DIR)
    
    versions = {}
    if os.path.exists("versions.json"):
        with open("versions.json", "r") as f:
            versions = json.load(f)

    if versions:
        update_mod_versions(versions)
        update_root_readme(versions)
    
    run_build()

    if os.path.exists(SRC_DIR):
        for item in os.listdir(SRC_DIR):
            if os.path.isdir(os.path.join(SRC_DIR, item)) and not item.startswith("."):
                package_mod(item)
    
    if os.path.exists(TEMP_STAGE):
        shutil.rmtree(TEMP_STAGE)
    
    print("All operations complete.")

if __name__ == "__main__":
    main()
