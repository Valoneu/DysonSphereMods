# DysonSphereMods
## Lisf of my mods:
1. LessShipPower - reduce power needed by vessels
2. TechHashReduce - reduce hashes of techs
3. FactoryMultiplier mod support - WIP, need to do somehow this 
Kremnev8
1/4/2022 20:07:24 today
LDBTool class has a Action variable called PostAddDataAction. In your awake function do this:
LDBTool.PostAddDataAction += PostAdd;

Create a method called PostAdd in the same class and do your stuff there