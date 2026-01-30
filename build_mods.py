import os
import shutil
import subprocess
import glob
import zipfile
import json
import re

SRC_DIR = "src"
ASSETS_DIR = "zip"
FINAL_DIR = "final"

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

        version_key = VERSION_KEY_MAP.get(mod_folder, mod_folder)
        
        if version_key not in versions:
            print(f"  [Info] No version found for {mod_folder} (key: {version_key}) in versions.json.")
            continue
        
        new_version = versions[version_key]
        print(f"  Updating {mod_folder} to {new_version}...")

        csproj_files = glob.glob(os.path.join(mod_path, "*.csproj"))
        if csproj_files:
            csproj_path = csproj_files[0]
            with open(csproj_path, "r", encoding="utf-8") as f:
                content = f.read()
            
            content = re.sub(r"<Version>.*?</Version>", f"<Version>{new_version}</Version>", content)
            content = re.sub(r"<BepInExPluginVersion>.*?</BepInExPluginVersion>", f"<BepInExPluginVersion>{new_version}</BepInExPluginVersion>", content)

            with open(csproj_path, "w", encoding="utf-8") as f:
                f.write(content)
        
        manifest_pattern = os.path.join(ASSETS_DIR, f"{mod_folder}_manifest.json")
        manifests = glob.glob(manifest_pattern)
        
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

        readme_src = os.path.join(mod_path, "README.md")
        if os.path.exists(readme_src):
            update_readme_version(readme_src, new_version)
            
        readme_zip_pattern = os.path.join(ASSETS_DIR, f"{mod_folder}_README.md")
        readmes_zip = glob.glob(readme_zip_pattern)
        for r_path in readmes_zip:
            update_readme_version(r_path, new_version)

def update_readme_version(file_path, new_version):
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            content = f.read()
        
        pattern = r"(^Version[:\s]+)(\d+\.\d+\.\d+)"
        if re.search(pattern, content, flags=re.IGNORECASE | re.MULTILINE):
            content_new = re.sub(pattern, f"\\g<1>{new_version}", content, flags=re.IGNORECASE | re.MULTILINE)
        else:
            content_new = f"Version: {new_version}\n\n{content}"
        
        if content != content_new:
            with open(file_path, "w", encoding="utf-8") as f:
                f.write(content_new)
            
    except Exception as e:
        print(f"  [Error] Failed to update README {file_path}: {e}")

def run_build():
    print("Building solution in Release mode...")
    try:
        subprocess.check_call(["dotnet", "build", "DysonSphereMods.slnx", "-c", "Release"])
    except subprocess.CalledProcessError as e:
        print("Build failed!")
        exit(1)

def package_mod(mod_folder_name):
    if mod_folder_name == "Shared":
        return

    print(f"Packaging {mod_folder_name}...")

    zip_filename = ZIP_NAME_MAP.get(mod_folder_name, f"{mod_folder_name}.zip")
    zip_path = os.path.join(FINAL_DIR, zip_filename)

    stage_dir = os.path.join("temp_stage", mod_folder_name)
    clean_and_create_dir(stage_dir)

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

    prefix = f"{mod_folder_name}_"
    
    asset_pattern = os.path.join(ASSETS_DIR, f"{prefix}*")
    assets = glob.glob(asset_pattern)

    for asset_path in assets:
        filename = os.path.basename(asset_path)
        original_name = filename[len(prefix):]
        
        dest_path = os.path.join(stage_dir, original_name)
        
        if os.path.isdir(asset_path):
            if os.path.exists(dest_path): shutil.rmtree(dest_path)
            shutil.copytree(asset_path, dest_path)
        else:
            shutil.copy2(asset_path, dest_path)

    with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zf:
        for root, dirs, files in os.walk(stage_dir):
            for file in files:
                file_path = os.path.join(root, file)
                arcname = os.path.relpath(file_path, stage_dir)
                zf.write(file_path, arcname)
    
    print(f"  -> Created {zip_path}")

def update_root_readme():
    print("Updating root README.md versions...")
    readme_path = "README.md"
    if not os.path.exists(readme_path):
        print("  [Warning] Root README.md not found.")
        return

    if not os.path.exists("versions.json"):
        return

    with open("versions.json", "r") as f:
        versions = json.load(f)

    try:
        with open(readme_path, "r", encoding="utf-8") as f:
            content = f.read()

        new_content = content
        for mod_name, version in versions.items():
            # Regex to match the table row: | **ModName** | ... | Version |
            # strict matching for the mod name to avoid partial matches
            pattern = r"(\| \*\*"+ re.escape(mod_name) + r"\*\* \|.*?\| )(\d+\.\d+\.\d+)( \|)"
            
            if re.search(pattern, new_content):
                new_content = re.sub(pattern, f"\\g<1>{version}\\g<3>", new_content)
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

def main():
    clean_and_create_dir(FINAL_DIR)
    
    update_versions()
    update_root_readme()
    run_build()

    if os.path.exists(SRC_DIR):
        for item in os.listdir(SRC_DIR):
            item_path = os.path.join(SRC_DIR, item)
            if os.path.isdir(item_path) and not item.startswith("."):
                package_mod(item)
    
    if os.path.exists("temp_stage"):
        shutil.rmtree("temp_stage")
    
    print("All operations complete.")

if __name__ == "__main__":
    main()