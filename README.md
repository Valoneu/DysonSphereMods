# Dyson Sphere Program Mods

A collection of mods for [Dyson Sphere Program](https://store.steampowered.com/app/1366540/Dyson_Sphere_Program/), maintained by Valoneu.

## 📦 Included Mods

| Mod Name | Description | Current Version |
| :--- | :--- | :--- |
| **CustomWarpSound** | Customizes the sound effects for warping. | 1.0.10 |
| **FactoryOverclock** | Allows overclocking of factory buildings for higher speed and power consumption. | 2.1.1 |
| **HydrogenDissolution** | Adds recipes to dissolve excess Hydrogen. | 1.0.1 |
| **LessShipPower** | Reduces the power consumption of logistics vessels. | 1.0.5 |
| **MaxLVLIncrease** | Increases the maximum level for infinite technologies. | 1.0.4 |
| **PilerMax** | Enhances the capabilities of the Piler sorter. | 1.0.0 |
| **TechHashReduce** | Reduces the hash requirement for technologies (cheaper research). | 1.1.2 |

## 🛠️ Project Structure

The repository is organized as follows:

- **`src/`**: Contains the source code for all active mods.
  - Each mod has its own folder (e.g., `src/FactoryMultiplier`).
  - **`src/Shared/`**: Common utility code used across multiple mods.
- **`zip/`**: Contains static assets (manifests, icons, READMEs) for each mod.
  - Files are prefixed with the mod name (e.g., `FactoryMultiplier_manifest.json`).
- **`versions.json`**: The single source of truth for mod versions.
- **`build_mods.py`**: Python script to build and package all mods.
- **`DysonSphereMods.slnx`**: The unified Visual Studio solution file.

## 🚀 Building the Mods

### Prerequisites
- [.NET SDK](https://dotnet.microsoft.com/download) (compatible with netstandard2.1)
- Python 3.x (for the build script)

### Build Instructions

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/Valoneu/DysonSphereMods.git
    cd DysonSphereMods
    ```

2.  **Run the build script:**
    ```bash
    python build_mods.py
    ```

    This script performs the following actions:
    *   Reads versions from `versions.json`.
    *   Updates version numbers in all `.csproj` files, `manifest.json` files, and `README.md` files.
    *   Builds the entire solution in `Release` mode using `dotnet build`.
    *   Packages the compiled DLLs and assets into `.zip` files.

3.  **Locate the artifacts:**
    The packaged mods will be available in the `final/` directory.

## 📝 Updating Versions

To update the version of a mod:
1.  Open `versions.json`.
2.  Change the version string for the desired mod.
3.  Run `python build_mods.py`.

The script will automatically propagate the new version to all necessary files.
