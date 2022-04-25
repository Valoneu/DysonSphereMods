using BepInEx.Configuration;

namespace FactoryMultiplier.Util
{
    public static class PluginConfig
    {
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
        public static ConfigEntry<int> _inserterMultiplier;
        public static ConfigEntry<double> drawMultiplier;

        private static ConfigEntry<int> _genWindMultiplier;
        private static ConfigEntry<int> _genSolarMultiplier;
        private static ConfigEntry<int> _genGeoMultiplier;
        private static ConfigEntry<int> _genThermalMultiplier;
        private static ConfigEntry<int> _genFusionMultiplier;
        private static ConfigEntry<int> _genStarMultiplier;
        private static ConfigEntry<int> _genExchMultiplier;

        public static int genWindMultiplier => multiplierEnabled.Value ? _genWindMultiplier.Value : 1;
        public static int genSolarMultiplier => multiplierEnabled.Value ? _genSolarMultiplier.Value : 1;
        public static int genGeoMultiplier => multiplierEnabled.Value ? _genGeoMultiplier.Value : 1;
        public static int genThermalMultiplier => multiplierEnabled.Value ? _genThermalMultiplier.Value : 1;
        public static int genFusionMultiplier => multiplierEnabled.Value ? _genFusionMultiplier.Value : 1;
        public static int genStarMultiplier => multiplierEnabled.Value ? _genStarMultiplier.Value : 1;
        public static int genExchMultiplier => multiplierEnabled.Value ? _genExchMultiplier.Value : 1;

        public static ConfigEntry<bool> keyTestMode;
        public static ConfigEntry<bool> multiplierEnabled;
        public static ConfigEntry<bool> enableAssemblerPopupLogMessage;

        public static int siloMultiplier => multiplierEnabled.Value ? _siloMultiplier.Value : 1;
        public static int inserterMultiplier => multiplierEnabled.Value ? _inserterMultiplier.Value : 1;
        public static int ejectorMultiplier => multiplierEnabled.Value ? _ejectorMultiplier.Value : 1;
        public static int fractionatorMultiplier => multiplierEnabled.Value ? _fractionatorMultiplier.Value : 1;
        public static int labMultiplier => multiplierEnabled.Value ? _labMultiplier.Value : 1;

        public static void InitConfig(ConfigFile confFile)
        {
            smeltMultiplier = confFile.Bind("1. Factory", "smeltMultiplier", 1, new ConfigDescription("Multiplies speed of smelters", new AcceptableValueRange<int>(1, 20)));
            chemicalMultiplier = confFile.Bind("1. Factory", "chemicalMultiplier", 1, new ConfigDescription("Multiplies speed of chemical plants", new AcceptableValueRange<int>(1, 20)));
            refineMultiplier = confFile.Bind("1. Factory", "refineMultiplier", 1, new ConfigDescription("Multiplies speed of refineries", new AcceptableValueRange<int>(1, 20)));
            assembleMultiplier = confFile.Bind("1. Factory", "assembleMultiplier", 1, new ConfigDescription("Multiplies speed of assemblers", new AcceptableValueRange<int>(1, 20)));
            particleMultiplier = confFile.Bind("1. Factory", "particleMultiplier", 1, new ConfigDescription("Multiplies speed of particle colliders", new AcceptableValueRange<int>(1, 20)));
            _labMultiplier = confFile.Bind("1. Factory", "labMultiplier", 1, new ConfigDescription("Multiplies speed of laboratories", new AcceptableValueRange<int>(1, 20)));
            _fractionatorMultiplier = confFile.Bind("1. Factory", "fractionateMultiplier", 1, new ConfigDescription("Multiplies % of fractionators", new AcceptableValueRange<int>(1, 20)));
            _ejectorMultiplier = confFile.Bind("1. Factory", "ejectorMultiplier", 1, new ConfigDescription("Multiplies speed of EM rail ejectors", new AcceptableValueRange<int>(1, 100)));
            _siloMultiplier = confFile.Bind("1. Factory", "siloMultiplier", 1, new ConfigDescription("Multiplies speed of silos", new AcceptableValueRange<int>(1, 100)));
            gammaMultiplier = confFile.Bind("1. Factory", "gammaMultiplier", 1, new ConfigDescription("Multiplies speed of ray recievers", new AcceptableValueRange<int>(1, 1000)));
            _inserterMultiplier = confFile.Bind("1. Factory", "sorterMultiplier", 1, new ConfigDescription("Multiplies speed of sorter", new AcceptableValueList<int>(1, 2, 4, 8)));

            drawMultiplier = confFile.Bind("1. Factory", "drawMultipler", 1.0, new ConfigDescription("Multiplies how much your factory will draw on top of your normal overclock", new AcceptableValueRange<double>(0.1, 10)));

            _genWindMultiplier = confFile.Bind("2. Generator", "generatorWindMultiplier", 1, new ConfigDescription("Multiplies speed of wind turbines", new AcceptableValueRange<int>(1, 100)));
            _genSolarMultiplier = confFile.Bind("2. Generator", "generatorSolarMultiplier", 1, new ConfigDescription("Multiplies speed of solar panels", new AcceptableValueRange<int>(1, 100)));
            _genGeoMultiplier = confFile.Bind("2. Generator", "generatorGeothermalMultiplier", 1, new ConfigDescription("Multiplies speed of geothermal plants", new AcceptableValueRange<int>(1, 100)));
            _genThermalMultiplier = confFile.Bind("2. Generator", "generatorThermalMultiplier", 1, new ConfigDescription("Multiplies speed of thermal plants", new AcceptableValueRange<int>(1, 100)));
            _genFusionMultiplier = confFile.Bind("2. Generator", "generatorFusionMultiplier", 1, new ConfigDescription("Multiplies speed of fusion power plants", new AcceptableValueRange<int>(1, 100)));
            _genStarMultiplier = confFile.Bind("2. Generator", "generatorArtificialStarMultiplier", 1, new ConfigDescription("Multiplies speed of artificial stars", new AcceptableValueRange<int>(1, 100)));
            _genExchMultiplier = confFile.Bind("2. Generator", "generatorExchangerMultiplier", 1, new ConfigDescription("Multiplies speed of energy exchangers", new AcceptableValueRange<int>(1, 100)));

            keyTestMode = confFile.Bind("3. Advanced", "keyTestMode", false, "Uses alt+1 as keybind for scriptengine support");
            multiplierEnabled = confFile.Bind("3. Advanced", "multiplierEnabled", true, "Determine whether we are currently multiplying values");
            enableAssemblerPopupLogMessage = confFile.Bind("3. Advanced", "enableAssemblerPopupLogMessage", false, "Ignore - For debugging, log message when UI window is opened");
        }

        public static int GetMultiplierFromPrefabDesc(PrefabDesc desc, int defaultToUse = 1)
        {
            if (multiplierEnabled.Value == false)
                return 1;
            if (desc.isSilo)
            {
                return siloMultiplier;
            }

            if (desc.isFractionator)
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