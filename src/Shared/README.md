# Shared Library

Common utilities and frameworks used across the Dyson Sphere Program mod collection. This library is designed to provide shared state management, throttled event systems, and a consistent UI framework.

## Technical Implementation

### Multiplier Registry (`MultiplierService`)
*   **Implementation**: Uses a `Dictionary<string, float>` to store multipliers by key.
*   **Dirty State**: Implements an `_isDirty` flag that is set whenever a multiplier changes.
*   **Batching**: `CommitChanges()` triggers the `OnMultipliersChanged` event, allowing dependent mods (like `FactoryOverclock`) to refresh all systems (e.g., generators, assemblers) in a single pass rather than per-configuration-change.

### Throttled Update System (`TickManager`)
*   **Implementation**: Patches `GameLogic.LogicFrame` via Harmony.
*   **Logic**: Tracks `GameMain.gameTick`.
    *   `OnSlowTick`: Invoked every 60 ticks (~1 second at 60 FPS).
    *   `OnLazyTick`: Invoked every 600 ticks (~10 seconds at 60 FPS).
*   **Performance**: Prevents O(n) operations (like scanning all ships or stations) from running every frame, significantly reducing the CPU overhead of background logic.

### UI Framework (`WindowBase`)
*   **Implementation**: Abstract base class for Unity IMGUI windows.
*   **Features**:
    *   Automatic screen clamping to prevent windows from being lost off-screen.
    *   Integrated `GUILayout.BeginScrollView` for scrollable content.
    *   Standardized Header, Content, and Footer hooks for consistent layout.
    *   `Toggle()` method for easy visibility management.

### Logging System (`Log`)
*   **Implementation**: Static wrapper for BepInEx `ManualLogSource`.
*   **Spam Prevention**: `LogOnce` uses a boolean flag and `JsonUtility` to log unique objects/messages exactly once, preventing log bloating during rapid game loops.
