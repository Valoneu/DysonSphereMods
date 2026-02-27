# TechHashReduce

Injects a dynamic scaling mechanism for technology research costs (Universal Matrix Hashes), drastically reducing the astronomical matrix requirements typically found deep in very late-game repeating technologies.

---

## Technical Information

### Mechanics and Configuration
- `HashScale`: Float multiplier adjusting and reducing global tech hash costs universally.

### Deep Technical Details
Patches the technology prototype database at runtime immediately upon load, modifying `hashNeeded` mathematically against predefined tier limits without breaking the tech-tree UI rendering formats.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
