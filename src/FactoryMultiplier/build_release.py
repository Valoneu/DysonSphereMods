import os
import zipfile
import subprocess
import json
import sys

# Configuration
PROJECT_DIR = os.getcwd()
DLL_NAME = "com.Valoneu.FactoryOverclock.dll"
DLL_PATH = os.path.join(PROJECT_DIR, "bin", "Release", "netstandard2.1", DLL_NAME)
ZIP_NAME = "FactoryOverclock.zip"
MANIFEST_FILE = "manifest.json"
FILES_TO_INCLUDE = [
    ("icon.png", "icon.png"),
    (MANIFEST_FILE, "manifest.json"),
    ("README.md", "README.md"),
    (DLL_PATH, DLL_NAME) # Source path, Destination name in zip
]

def build_project():
    print("Building project...")
    # Clean previous builds to ensure we get the latest
    subprocess.run(["dotnet", "clean", "-c", "Release"], check=True)
    
    # Build release version
    result = subprocess.run(["dotnet", "build", "-c", "Release"], check=True, capture_output=True, text=True)
    print(result.stdout)
    
    if not os.path.exists(DLL_PATH):
        print(f"Error: DLL not found at {DLL_PATH}")
        sys.exit(1)
    print("Build successful.")

def update_manifest_version(version_type='patch'):
    # Simple version bumper if needed
    if not os.path.exists(MANIFEST_FILE):
        return

    with open(MANIFEST_FILE, 'r') as f:
        data = json.load(f)
    
    # If you want to implement auto-versioning, do it here.
    # For now, we just read it to print it.
    print(f"Current Manifest Version: {data.get('version_number', 'Unknown')}")
    
    # Example logic to bump version could go here if requested
    # ...

def create_zip():
    print(f"Creating {ZIP_NAME}...")
    
    # "Special things" - Standard Deflate compression is usually what's needed.
    # Some older unzippers handled ZIP_STORED (no compression) better, but ZIP_DEFLATED is standard.
    # If "special settings" referred to making it compatible with specific mod managers,
    # ensuring the file structure is FLAT (everything in root) is the most important part.
    
    with zipfile.ZipFile(ZIP_NAME, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for src, arcname in FILES_TO_INCLUDE:
            if os.path.exists(src):
                print(f"Adding {src} as {arcname}")
                zipf.write(src, arcname)
            else:
                print(f"Warning: File {src} not found! Skipping.")
                if "dll" in src: # Critical failure
                    print("Critical: DLL missing. Aborting.")
                    sys.exit(1)

    print(f"Successfully created {ZIP_NAME}")

if __name__ == "__main__":
    build_project()
    update_manifest_version()
    create_zip()
