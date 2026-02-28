# StacksizeMultiplier

Globally customizable stack size multiplier for all items. Includes a dedicated in-game UI for fine-tuning individual item stacks and saving separate overrides.

---

## Technical Information

### Mechanics and Configuration
- `Hotkey`: Default `NumPad 2` to toggle the adjustment UI.
- Separate global multipliers for Items, Buildings, and Drones/Vessels.

### Deep Technical Details
Implements a custom `StacksizeMultiplierWindow` inheriting from native `WindowBase`. Directly modifies `ItemProto` stack sizes and updates the player's package in real-time.

### Dependencies
* **BepInEx, CommonAPI**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
