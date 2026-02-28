# StacksizeMultiplier

Provides a massive, globally customizable stack size multiplier to all game items, saving massive amounts of inventory and storage space. Includes a dedicated UI panel for live fine-tuning.

---

## Technical Information

### Mechanics and Configuration
- `Hotkey`: Default `LeftAlt + S` to open the Stack Size adjustment UI panel in-game.
- `GlobalMultiplier`: Multiplies all items globally on load.
- Contains unique save states for individual custom item overrides.

### Deep Technical Details
Implements a custom `StacksizeMultiplierWindow` completely inheriting from native `WindowBase` to ensure seamless scrolling/interaction with the native Canvas DSP pipeline.

### Dependencies
* **BepInEx, CommonAPI**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
