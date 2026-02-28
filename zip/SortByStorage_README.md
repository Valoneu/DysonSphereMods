# SortByStorage

Adds 'Stored Descending' and 'Stored Ascending' sorting options natively into the Production Statistics UI panel, letting you sort items by their total stored quantity across the current scope.

---

## Technical Information

### Mechanics and Configuration
Select the new options in the sorting dropdown within the Production Statistics window.

### Deep Technical Details
Hooks into `UIStatisticsWindow` routines to refresh item storage counts and apply a custom quicksort algorithm to the UI list elements.

### Dependencies
* **BepInEx**

---
**Game Version:** V0.10.34.28455
**Developer:** Valoneu
