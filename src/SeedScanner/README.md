# SeedScanner (Standalone Dyson Sphere Program Universe Generator)

A high-performance C# CLI tool that perfectly replicates the **Dyson Sphere Program** universe generation logic to extract detailed seed data headlessly.

## Features
- **100% Game Accuracy**: Uses the actual decompiled game code for Universe, Star, and Planet generation.
- **Deep Strategic Scan (Complete Cluster Data)**:
    - **Star Network**: Warp-Jump Connections (Star Graph) matching the in-game constellation lines.
    - **Dark Fog**: Initial Hive Counts, Planet-level starting base counts, and Safety Factors.
    - **Detailed Stars**: Position, Dyson Luminosity (`Luminosity ^ 0.33`), Mass, Temperature, Radius, Resource Coefficient, and visual Asteroid Belt radii.
    - **Detailed Planets**: Full Orbital Data (Period, Inclination, Phases, Obliquity), Ratios (Solar/Wind), Ocean Types, and Moon support.
    - **Resource Aggregation**: Total system-wide vein counts and detailed planetary estimates.
- **English Mappings**: All themes and resources are mapped to their clean, one-word English names (e.g., `Titanium`, `Silicon`, `Magnet`).
- **High Performance**: Built with thread-safe logic for parallel scanning of thousands of seeds.

## Usage

### Configuration
Edit `src/SeedScanner/scanner_config.json`. 

#### Available Settings
| Key | Min | Max | Description |
| :--- | :--- | :--- | :--- |
| **SeedStart** | 0 | 99,999,999 | Starting galaxy seed. |
| **SeedEnd** | 0 | 99,999,999 | Ending galaxy seed. |
| **StarCount** | 32 | 64 | Total stars in the cluster (Game default is 64). |
| **ResourceMultiplier** | 0.1 | 100.0 | Resource richness (100.0 = Infinite in-game). |
| **Threads** | 1 | CPU Count | Parallel processing threads. |
| **OutputFolder** | - | - | Folder where JSON files will be saved. |
| **Aggressiveness** | 0.0 | 3.0 | Dark Fog aggressiveness level. |
| **InitialLevel** | 0.0 | 10.0 | Starting level of the Dark Fog. |
| **MaxDensity** | 0.0 | 3.0 | Maximum Dark Fog density in the system. |

#### Example `scanner_config.json`
```json
{
  "SeedStart": 0,
  "SeedEnd": 100,
  "StarCount": 64,
  "ResourceMultiplier": 1.0,
  "Threads": 16,
  "OutputFolder": "seeds",
  "CombatSettings": {
    "Aggressiveness": 1.0,
    "InitialLevel": 0.0,
    "MaxDensity": 1.0
  }
}
```

### Running the Scanner
1. Build the project:
   ```powershell
   dotnet build src/SeedScanner/SeedScanner.csproj -c Release
   ```
2. Run the executable:
   ```powershell
   .\src\SeedScanner\bin\Release\SeedScanner.exe src\SeedScanner\scanner_config.json
   ```

## Output Format
- `Id 1`: Always the **Birth Star** (at `0,0,0`).
- `Connections`: IDs of stars reachable via warp jump (Star Map constellation lines).
- `TotalVeins`: System-wide resource summary using one-word English names.
- `DysonLuminosity`: The map-view luminosity value (`Luminosity ^ 0.33`).
- `StartingFogBases`: Number of Dark Fog ground bases on a specific planet at start.
