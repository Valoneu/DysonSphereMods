# InfinityTechnologies

Allows for more granular, balanced control over the infinite research levels by recalculating tech requirements, ensuring late-game research paths scale infinitely efficiently.

---

## Technical Information

### Mechanics and Configuration
- Customizable technology multipliers and toggle overrides via BepInEx config manager.

### Deep Technical Details
Utilizes fast MSIL Delegates (`AccessTools.FieldRefAccess`) to bypass standard reflection overhead during Dyson Sphere power updates and technology node recalculations to ensure high UPS.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
