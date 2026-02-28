# TechHashReduce

Allows scaling the hash requirement (cost) for research. Speed up your progression or add a challenge by adjusting the universal research investment required for all technologies.

---

## Technical Information

### Mechanics and Configuration
- `HashrateScale`: Multiplier for technology hash requirements. Below 1.0 is cheaper; above 1.0 is more expensive.

### Deep Technical Details
Patches `TechProto.GetHashNeeded` to apply the scale and `GameHistoryData.Import` to ensure save games remain consistent across requirement changes.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
