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
        public static ConfigEntry<int> ejectorMultiplier;
        private static ConfigEntry<int> _siloMultiplier;
        public static ConfigEntry<int> gammaMultiplier;
        public static ConfigEntry<bool> keyTestMode;
        public static ConfigEntry<bool> multiplierEnabled;
        public static ConfigEntry<bool> enableAssemblerPopupLogMessage;

        public static int siloMultiplier => multiplierEnabled.Value ? _siloMultiplier.Value : 1;

        public static int fractionatorMultiplier => multiplierEnabled.Value ? _fractionatorMultiplier.Value : 1;
        public static int labMultiplier => multiplierEnabled.Value ? _labMultiplier.Value : 1;

        public static void InitConfig(ConfigFile confFile)
        {
            walkspeedMultiplier = confFile.Bind("mecha", "WalkSpeedMultiplier", 1, new ConfigDescription(
                "How fast do you want to go",
                new AcceptableValueRange<int>(1, 10)));

            smeltMultiplier = confFile.Bind("config", "smeltMultiplier", 1, "Multiplies speed of smelters");
            chemicalMultiplier = confFile.Bind("config", "chemicalMultiplier", 1, "Multiplies speed of chemical plants");
            refineMultiplier = confFile.Bind("config", "refineMultiplier", 1, "Multiplies speed of refineries");
            assembleMultiplier = confFile.Bind("config", "assembleMultiplier", 1, "Multiplies speed of assemblers");
            particleMultiplier = confFile.Bind("config", "particleMultiplier", 1, "Multiplies speed of particle colliders");
            _labMultiplier = confFile.Bind("config", "labMultiplier", 1, "Multiplies speed of laboratories");
            _fractionatorMultiplier = confFile.Bind("config", "fractionateMultiplier", 1, "Multiplies % of fractionators");
            ejectorMultiplier = confFile.Bind("config", "ejectorMultiplier", 1, "Multiplies speed of EM rail ejectors");
            _siloMultiplier = confFile.Bind("config", "siloMultiplier", 1, "Multiplies speed of silos");
            gammaMultiplier = confFile.Bind("config", "gammaMultiplier", 1, "Multiplies speed of ray recievers");
            keyTestMode = confFile.Bind("config", "keyTestMode", false, "Uses alt+1 as keybind for scriptengine support");
            multiplierEnabled = confFile.Bind("config", "multiplierEnabled", true, "Determine whether we are currently multiplying values");
            enableAssemblerPopupLogMessage = confFile.Bind("config", "enableAssemblerPopupLogMessage", false, "For debugging, log message when UI window is opened");
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
                return ejectorMultiplier.Value;
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