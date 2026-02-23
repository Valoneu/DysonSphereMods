import os
import glob
import json
import re
import shutil

def sync_metadata():
    src_dir = "src"
    zip_dir = "zip"
    
    # 1. Parse main README for descriptions
    descriptions = {}
    with open("README.md", "r", encoding="utf-8") as f:
        readme_content = f.read()
        
    # Table format: | **ModName** | Description text | v1.0.0 |
    # Regex grabs the ModName and the Description text
    matches = re.findall(r'\|\s*\*\*([a-zA-Z0-9_]+)\*\*\s*\|\s*(.*?)\s*\|', readme_content)
    for mod_name, desc in matches:
        descriptions[mod_name] = desc.strip()
        
    # 2. Iterate through all mods
    for mod_folder in os.listdir(src_dir):
        mod_path = os.path.join(src_dir, mod_folder)
        if not os.path.isdir(mod_path) or mod_folder == "Shared":
            continue
            
        # 3. Copy src/ README to zip/ README
        src_readme = os.path.join(mod_path, "README.md")
        zip_readme = os.path.join(zip_dir, f"{mod_folder}_README.md")
        if os.path.exists(src_readme):
            shutil.copy2(src_readme, zip_readme)
            print(f"Synced README for {mod_folder}")
        else:
            print(f"Warning: No src README found for {mod_folder}")

        # 4. Update Manifests
        manifest_path = os.path.join(zip_dir, f"{mod_folder}_manifest.json")
        if os.path.exists(manifest_path):
            with open(manifest_path, "r", encoding="utf-8") as f:
                manifest_data = json.load(f)
                
            manifest_data["website_url"] = f"https://github.com/Valoneu/DysonSphereMods/tree/main/src/{mod_folder}"
            
            if mod_folder in descriptions:
                # Thunderstore manifest description max length is 250
                desc = descriptions[mod_folder]
                if len(desc) > 250:
                    desc = desc[:247] + "..."
                manifest_data["description"] = desc
            
            with open(manifest_path, "w", encoding="utf-8") as f:
                json.dump(manifest_data, f, indent=4)
            print(f"Updated Manifest for {mod_folder}")
        else:
            print(f"Warning: No manifest found for {mod_folder}")

if __name__ == "__main__":
    sync_metadata()
    print("Metadata sync complete!")
