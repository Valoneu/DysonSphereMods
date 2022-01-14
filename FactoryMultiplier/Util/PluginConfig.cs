using BepInEx.Configuration;
using static FactoryMultiplier.Util.Log;

namespace FactoryMultiplier.Util
{
    public static class PluginConfig
    {
        public static ConfigEntry<int> walkspeedMultiplier;
        public static ConfigEntry<int> smeltMultiplier;
        public static ConfigEntry<int> chemicalMultiplier;
        public static ConfigEntry<int> refineMultiplier;
        public static ConfigEntry<int> assembleMultiplier;
        public static ConfigEntry<int> particleMultiplier;
        private static ConfigEntry<int> _labMultiplier;
        private static ConfigEntry<int> _fractionatorMultiplier;
        private static ConfigEntry<int> _ejectorMultiplier;
        private static ConfigEntry<int> _siloMultiplier;
        public static ConfigEntry<int> gammaMultiplier;
        public static ConfigEntry<int> inserterMultiplier;
        public static ConfigEntry<bool> keyTestMode;
        public static ConfigEntry<bool> multiplierEnabled;
        public static ConfigEntry<bool> enableAssemblerPopupLogMessage;

        public static int siloMultiplier => multiplierEnabled.Value ? _siloMultiplier.Value : 1;
        public static int ejectorMultiplier => multiplierEnabled.Value ? _ejectorMultiplier.Value : 1;

        public static int fractionatorMultiplier => multiplierEnabled.Value ? _fractionatorMultiplier.Value : 1;
        public static int labMultiplier => multiplierEnabled.Value ? _labMultiplier.Value : 1;

        public static void InitConfig(ConfigFile confFile)
        {
            walkspeedMultiplier = confFile.Bind("mecha", "WalkSpeedMultiplier", 1, new ConfigDescription( "How fast do you want to go", new AcceptableValueRange<int>(1, 20)));
            smeltMultiplier = confFile.Bind("config", "smeltMultiplier", 1, new ConfigDescription( "Multiplies speed of smelters", new AcceptableValueRange<int>(1, 20)));
            chemicalMultiplier = confFile.Bind("config", "chemicalMultiplier", 1, new ConfigDescription( "Multiplies speed of chemical plants", new AcceptableValueRange<int>(1, 20)));
            refineMultiplier = confFile.Bind("config", "refineMultiplier", 1, new ConfigDescription( "Multiplies speed of refineries", new AcceptableValueRange<int>(1, 20)));
            assembleMultiplier = confFile.Bind("config", "assembleMultiplier", 1, new ConfigDescription( "Multiplies speed of assemblers", new AcceptableValueRange<int>(1, 20)));
            particleMultiplier = confFile.Bind("config", "particleMultiplier", 1, new ConfigDescription( "Multiplies speed of particle colliders", new AcceptableValueRange<int>(1, 20)));
            _labMultiplier = confFile.Bind("config", "labMultiplier", 1, new ConfigDescription( "Multiplies speed of laboratories", new AcceptableValueRange<int>(1, 20)));
            _fractionatorMultiplier = confFile.Bind("config", "fractionateMultiplier", 1, new ConfigDescription( "Multiplies % of fractionators", new AcceptableValueRange<int>(1, 20)));
            _ejectorMultiplier = confFile.Bind("config", "ejectorMultiplier", 1, new ConfigDescription( "Multiplies speed of EM rail ejectors", new AcceptableValueRange<int>(1, 20)));
            _siloMultiplier = confFile.Bind("config", "siloMultiplier", 1, new ConfigDescription( "Multiplies speed of silos", new AcceptableValueRange<int>(1, 20)));
            gammaMultiplier = confFile.Bind("config", "gammaMultiplier", 1, new ConfigDescription( "Multiplies speed of ray recievers", new AcceptableValueRange<int>(1, 20)));
            inserterMultiplier = confFile.Bind("config", "sorterMultiplier", 1, new ConfigDescription("Multiplies speed of sorter", new AcceptableValueList<int>(1, 2, 4)));
            keyTestMode = confFile.Bind("config", "keyTestMode", false, "Uses alt+1 as keybind for scriptengine support");
            multiplierEnabled = confFile.Bind("config", "multiplierEnabled", true, "Determine whether we are currently multiplying values");
            enableAssemblerPopupLogMessage = confFile.Bind("config", "enableAssemblerPopupLogMessage", false, "Ignore - For debugging, log message when UI window is opened");
        }

        public static int GetMultiplierFromPrefabDesc(PrefabDesc desc, int defaultToUse = 1)
        {
            if (multiplierEnabled.Value == false)
                return 1;
            if (desc.isSilo)
            {
                return siloMultiplier;
            }

            if (desc.isFractionate)
            {
                return fractionatorMultiplier;
            }

            if (desc.isEjector)
                return ejectorMultiplier;
            if (desc.isLab)
                return labMultiplier;

            return defaultToUse;
        }

        public static int GetMultiplierByRecipe(ERecipeType eRecipeType)
        {
            if (multiplierEnabled.Value == false)
                return 1;
            switch (eRecipeType)
            {
                case ERecipeType.Assemble:
                    return assembleMultiplier.Value;
                case ERecipeType.Chemical:
                    return chemicalMultiplier.Value;
                case ERecipeType.Exchange:
                    return 1;
                case ERecipeType.Fractionate:
                    return fractionatorMultiplier;
                case ERecipeType.Particle:
                    return particleMultiplier.Value;
                case ERecipeType.Refine:
                    return refineMultiplier.Value;
                case ERecipeType.Research:
                    return labMultiplier;
                case ERecipeType.Smelt:
                    return smeltMultiplier.Value;
                case ERecipeType.PhotonStore:
                    return gammaMultiplier.Value;
                default:
                    return 1;
            }
        }
    }
}