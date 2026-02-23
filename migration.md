# Migration Guide: `oldsrc` → `src`

## Overall Refactoring Patterns (applies to all mods)

All new versions adopted these patterns:

- **Shared library** (`DysonSphereMods.Shared`) with `Log.Init()` replacing direct `ManualLogSource` usage
- **`MyPluginInfo`** auto-generated constants replacing hardcoded GUID/NAME/VERSION strings
- **`[BepInProcess("DSPGAME.exe")]`** attribute added consistently
- **`TickManager`** for periodic operations replacing per-frame modulo checks
- **`MultiplierService`** / `WindowBase` shared abstractions where applicable

---

## Mod-by-Mod Missing Features & Logic Differences

| Mod | Feature/Logic | Old (`oldsrc`) | New (`src`) | Status |
|---|---|---|---|---|
| **FactoryMultiplier → FactoryOverclock** | Fractionator prefix (extra belt input) | `FractionatorComponent_InternalUpdate_Prefix` manually picks extra items from belts before the vanilla update runs | Prefix added — pulls extra items from belt1/belt2 before vanilla fractionator runs | ✅ **Ported** |
| **FactoryMultiplier → FactoryOverclock** | Fractionator transpiler (limit & output multiplier) | Transpiler patches `Ldc_R8 30.0` to `GetFractionatorLimit()` and patches `TryInsertItemAtHead`/`TryUpdateItemAtHeadAndFillBlank` counters to use `GetBeltMultiplier()` | Transpiler added — replaces 30.0 limit and output counters with dynamic methods | ✅ **Ported** |
| **FactoryMultiplier → FactoryOverclock** | Fractionator postfix (extra output pushes) | Extra loops pushing product & fluid outputs to belts after vanilla update | Postfix added — pushes extra product/fluid outputs after vanilla update | ✅ **Ported** |
| **FactoryMultiplier → FactoryOverclock** | Station `UpdateInputSlots` multiplier | Postfix calling `UpdateInputSlots` multiple times via reflection | Postfix added — calls UpdateInputSlots (multi-1) extra times via reflection | ✅ **Ported** |
| **FactoryMultiplier → FactoryOverclock** | Station `UpdateSlots` transpiler (counter patch) | Transpiler replaces `Ldc_I4_1` → `GetSlotCounterValue()` for both `UpdateOutputSlots` and `UpdateInputSlots` | Transpiler added — patches counter field writes to use GetSlotCounterValue() | ✅ **Ported** |
| **FactoryMultiplier → FactoryOverclock** | Station `Import` postfix (storage migration) | Patches storage max on save load | Present but logic may differ | ✅ Present |
| **FactoryMultiplier → FactoryOverclock** | `UIStationStorage.RefreshValues` transpiler | Transpiler patches `set_maxValue` to use `GetAdjustedSliderMax()` for extended station storage slider | Transpiler added — adjusts slider max to reflect extended storage limits | ✅ **Ported** |
| **FactoryMultiplier → FactoryOverclock** | Inserter bidirectional transpiler | Transpiler patches `InternalUpdate_Bidirectional` to replace `Ldc_I4_1` with `inserterMultiplier` for local variable initialization | Transpiler added — replaces Ldc_I4_1 → inserterMultiplier for Stloc_0..3 | ✅ **Ported** |
| **FactoryMultiplier → FactoryOverclock** | MoreMegaStructure compat `UpdateInputSlots` | Patches `ExchangeStationComponent.UpdateInputSlots` | Prefix added — multi-calls UpdateInputSlots N times | ✅ **Ported** |
| **FactoryMultiplier → FactoryOverclock** | MoreMegaStructure compat `UpdateSlots` transpiler | Transpiler for `UpdateOutputSlots`, `UpdateInputSlots`, `UpdateSlots` methods | Transpiler added — delegates to StationComponent_UpdateSlots_Transpiler | ✅ **Ported** |
| **FactoryMultiplier → FactoryOverclock** | MoreMegaStructure compat `InternalTickRemote` | Patches `ExchangeStationComponent.InternalTickRemote` | Prefix added — multi-calls InternalTickRemote N times | ✅ **Ported** |
| **FactoryMultiplier → FactoryOverclock** | MoreMegaStructure compat `UpdateOutputSlots` | Patches `ExchangeStationComponent.UpdateOutputSlots` | Prefix added — multi-calls UpdateOutputSlots N times | ✅ **Ported** |
| **FactoryMultiplier → FactoryOverclock** | Station `UpdateCollection` collector safety limits | Detailed per-slot `maxCollectionPerTick` check with `limitRate` clamping | Improved — per-slot analysis with limitRate clamping and 1M cap | ✅ **Ported** |
| **FactoryMultiplier → FactoryOverclock** | Station reflection-based `InternalTickLocal/Remote` | Uses `method.Invoke()` via reflection as a **postfix** (runs N-1 extra times) | Uses **prefix** pattern — calls `__instance.InternalTickLocal()` directly N times, returning false to skip original | ✅ **Improved** — direct calls are faster than reflection |
| **FactoryMultiplier → FactoryOverclock** | Config: separate `Util/PluginConfig.cs` and `Util/ItemUtil.cs` | Separate files in `Util/` folder | Inlined into single `FactoryOverclock.cs` file | ✅ Consolidated |
| **BottleneckUI** | Scan logic | Direct scan in plugin class | Separated into `BottleneckScanner` + `BottleneckWindow` | ✅ Refactored |
| **CloserStations** | Transpiler logic | Separate transpilers per method | Shared `CommonTranspiler` method | ✅ Refactored |
| **DistributeWarpers** | Distribution trigger | Patches `InternalTickLocal` on `station.id == 1` | Uses `TickManager.OnSlowTick` | ✅ Refactored |
| **FarZoom** | FOV input handling | Raw `Input.GetAxis("Mouse ScrollWheel")` + `VFInput.shift` | Uses `VFInput` for input, similar logic | ✅ Equivalent |
| **HydrogenDissolution** | Recipe registration | Inline in `Awake()` | Separate `RecipeDefinitions.Register()` | ✅ Refactored |
| **InfinityTechnologies** | Tech registration & modifiers | All inline in plugin | Separated into `ModifierManager` + `TechDefinitions` | ✅ Refactored |
| **LessVesselPower → LessShipPower** | Core logic | Static field `_vesselEnergyScale` | `ConfigEntry<float>` | ✅ Improved |
| **MaxLVLIncrease** | Tech state reset logic | Same logic for `hashUploaded`/`unlocked` | Identical logic | ✅ Equivalent |
| **PilerMax** | Cooldown value | Hardcoded `2` | `ConfigEntry<int> PilerCooldown` (configurable) | ✅ **New feature** added |
| **PinnedNamesEverywhere** | Performance cache | No caching — checks pinned state every frame | `ConditionalWeakTable` cache, checks every 60 frames | ✅ **New feature** — performance optimized |
| **PlanetMinerFast** | Tick mechanism | `FactorySystem.GameTick` postfix with `time % SCAN_INTERVAL` | `TickManager.OnSlowTick` iterating all factories | ✅ Refactored |
| **PlanetMinerFast** | `SCAN_INTERVAL` constant | `60` (explicit constant) | Implicit via `TickManager` (1s slow tick) | ✅ Equivalent |
| **PlanetMinerFast** | Dependency flag | `SoftDependency` on `com.Valoneu.Shared` | `HardDependency` on `com.Valoneu.Shared` | ✅ Changed |
| **PlanetMinerFast** | `OnDestroy` cleanup | Not present | `OnDestroy` unsubscribes tick + unpatches | ✅ **New feature** — proper cleanup |
| **SortByStorage** | Transpiler approach | Manual `List<CodeInstruction>` manipulation | Uses `CodeMatcher` for cleaner transpiler | ✅ **Improved** — more robust |
| **SortByStorage** | Sort constants location | Inside nested `SortByStoragePatch` class | Moved to plugin class level | ✅ Refactored |
| **SpaceTargetEverything** | Tick mechanism | Patches `SpaceSector.GameTick` postfix, runs every tick with `_logTimer >= 600` | Uses `TickManager.OnSlowTick` + `OnLazyTick` for aggregated logging | ✅ Refactored |
| **SpaciousStations** | Proto data application | Uses `LDBTool.EditDataAction += OnEditData` | Uses `VFPreload_InvokeOnLoadWorkEnded` postfix only | ⚠️ **Changed** — `LDBTool.EditDataAction` hook removed; relies on postfix timing |
| **SpaciousStations** | Multiplier access | Direct `ConfigEntry.Value` access | Via `MultiplierService.GetMultiplier()` | ✅ Refactored |
| **SpaciousStations** | `xiaoye97` import | `using xiaoye97;` (for LDBTool) | Removed | ✅ Dependency removed |
| **StacksizeMultiplier** | Window implementation | Inline `OnGUI`/`WindowFunc` with resize, scroll, drag | Separated into `StacksizeMultiplierWindow : WindowBase` | ✅ Refactored |
| **StacksizeMultiplier** | ScrollView in item list | `GUILayout.BeginScrollView(_scrollPos, GUI.skin.box)` wrapping item loops | Handled by `WindowBase.DrawWindowInternal` — wraps `DrawWindowContent` in ScrollView | ✅ **Present via base class** |
| **TechHashReduce** | `techStates` key check | Direct access `__instance.techStates[tech.ID]` without key check | Added `ContainsKey` guard before access | ✅ **Improved** — prevents KeyNotFoundException |
| **VesselTrails** | Architecture | Monolithic `VesselTrailRenderer` MonoBehaviour | Split into `VesselRouteManager` (data), `VesselTrailRenderer` (rendering), `VesselTrailsWindow : WindowBase` (UI) | ✅ Refactored |
| **VesselTrails** | Data update timing | `LateUpdate()` every frame | `TickManager.OnSlowTick` (every 1s) | ✅ **Improved** — less CPU usage |
| **VesselTrails** | Resize handling | Manual resize logic in `Update()` with `_isResizing` state | Handled by `WindowBase` | ✅ Refactored |

---

## Summary

All previously missing features from **FactoryMultiplier → FactoryOverclock** have been ported:

1. ✅ **Fractionator 3-part patching** (prefix + transpiler + postfix) — full belt input/output/limit scaling
2. ✅ **Station `UpdateInputSlots`** — multi-call postfix for input throughput
3. ✅ **Station slot counter transpiler** — patches counter writes in both UpdateOutputSlots/UpdateInputSlots
4. ✅ **`UIStationStorage.RefreshValues` transpiler** — UI slider max adjustment for extended storage
5. ✅ **Inserter bidirectional transpiler** — local variable init patching for InternalUpdate_Bidirectional
6. ✅ **MoreMegaStructure full compatibility** — InternalTickRemote, UpdateOutputSlots, UpdateInputSlots prefixes + UpdateSlots transpiler
7. ✅ **Collector safety limits** — per-slot maxCollectionPerTick check with limitRate clamping
8. ✅ **StacksizeMultiplier ScrollView** — already handled by WindowBase.DrawWindowInternal

The only remaining ⚠️ item is **SpaciousStations** timing change (`LDBTool.EditDataAction` → postfix), which is a deliberate design decision to reduce dependencies.
