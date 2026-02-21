using BepInEx;
using BepInEx.Configuration;
using CommonAPI;
using CommonAPI.Systems;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using DysonSphereMods.Shared;

namespace BottleneckUI
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("DSPGAME.exe")]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(CustomKeyBindSystem))]
    public class BottleneckUIPlugin : BaseUnityPlugin
    {
        private Harmony _harmony;
        public static ConfigEntry<KeyboardShortcut> toggleKey;
        private bool _showUI = false;
        private Rect _windowRect = new Rect(200, 200, 500, 600);
        private Vector2 _scrollPos;
        private List<BottleneckInfo> _bottlenecks = new List<BottleneckInfo>();
        private float _lastScanTime = 0f;
        private const float ScanInterval = 1f;
        private int _totalScanned = 0;

        private void Awake()
        {
            Log.Init(Logger);
            toggleKey = Config.Bind("General", "ToggleKey", new KeyboardShortcut(KeyCode.Keypad4, KeyCode.LeftAlt), "Key to toggle the Bottleneck UI");
            
            InitKeyBinds();
            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded!");
        }

        private void InitKeyBinds()
        {
            if (!CustomKeyBindSystem.HasKeyBind("ToggleBottleneckUI"))
            {
                // Modifier 4 = Alt
                CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
                {
                    id = 1215,
                    key = new CombineKey((int)toggleKey.Value.MainKey, 4, ECombineKeyAction.OnceClick, false),
                    conflictGroup = 2052,
                    name = "ToggleBottleneckUI",
                    canOverride = true
                });
            }
#pragma warning disable CS0618
            ProtoRegistry.RegisterString("KEYToggleBottleneckUI", "Toggle Bottleneck UI");
#pragma warning restore CS0618
        }

        private void Update()
        {
            if (CustomKeyBindSystem.GetKeyBind("ToggleBottleneckUI").keyValue)
            {
                _showUI = !_showUI;
                if (_showUI)
                {
                    ScanBottlenecks();
                }
            }

            if (_showUI && Time.time - _lastScanTime > ScanInterval)
            {
                ScanBottlenecks();
            }
        }

        private string GetMachineName(PlanetFactory factory, int entityId)
        {
            if (entityId <= 0 || entityId >= factory.entityCursor) return "Unknown";
            int protoId = factory.entityPool[entityId].protoId;
            var item = LDB.items.Select(protoId);
            return item != null ? item.Name.Translate() : "Unknown Machine";
        }

        private string GetPowerStatus(int pcId, PlanetFactory factory)
        {
            if (pcId <= 0) return "No Power Connection";
            if (pcId >= factory.powerSystem.consumerCursor) return "Power Error";
            
            var consumer = factory.powerSystem.consumerPool[pcId];
            if (consumer.requiredEnergy <= 0) return null; // No energy needed
            
            float serveRatio = 1f;
            if (consumer.networkId > 0 && consumer.networkId < factory.powerSystem.networkServes.Length)
                serveRatio = factory.powerSystem.networkServes[consumer.networkId];
            
            if (serveRatio <= 0.001f) return "No Power";
            if (serveRatio < 0.98f) // 2% tolerance
            {
                return $"Low Power ({serveRatio:P0})";
            }
            return null;
        }

        private void ScanBottlenecks()
        {
            _lastScanTime = Time.time;
            _bottlenecks.Clear();
            _totalScanned = 0;

            if (GameMain.data == null || GameMain.data.factories == null) return;

            foreach (var factory in GameMain.data.factories)
            {
                if (factory == null) continue;
                string planetName = factory.planet.displayName;
                var factorySystem = factory.factorySystem;

                // Assemblers, Smelters, Chem Plants, Refineries, Colliders
                for (int i = 1; i < factorySystem.assemblerCursor; i++)
                {
                    ref var assembler = ref factorySystem.assemblerPool[i];
                    if (assembler.id != i || assembler.entityId == 0) continue;
                    _totalScanned++;

                    if (!assembler.replicating)
                    {
                        string status = GetAssemblerStatus(ref assembler, factory);
                        if (status != "Working")
                        {
                            _bottlenecks.Add(new BottleneckInfo
                            {
                                planetName = planetName,
                                machineName = GetMachineName(factory, assembler.entityId),
                                status = status
                            });
                        }
                    }
                }

                // Labs
                for (int i = 1; i < factorySystem.labCursor; i++)
                {
                    ref var lab = ref factorySystem.labPool[i];
                    if (lab.id != i || lab.entityId == 0) continue;
                    if (!lab.researchMode && lab.recipeId == 0) continue;
                    _totalScanned++;

                    if (!lab.replicating)
                    {
                        string status = GetLabStatus(ref lab, factory);
                        if (status != "Working")
                        {
                            _bottlenecks.Add(new BottleneckInfo
                            {
                                planetName = planetName,
                                machineName = GetMachineName(factory, lab.entityId),
                                status = status
                            });
                        }
                    }
                }
                
                // Fractionators
                for (int i = 1; i < factorySystem.fractionatorCursor; i++)
                {
                    ref var frac = ref factorySystem.fractionatorPool[i];
                    if (frac.id != i || frac.entityId == 0 || frac.fluidId == 0) continue;
                    _totalScanned++;
                    
                    if (!frac.isWorking)
                    {
                        string status = GetFracStatus(ref frac, factory);
                        if (status != "Working")
                        {
                            _bottlenecks.Add(new BottleneckInfo
                            {
                                planetName = planetName,
                                machineName = GetMachineName(factory, frac.entityId),
                                status = status
                            });
                        }
                    }
                }

                // Miners
                for (int i = 1; i < factorySystem.minerCursor; i++)
                {
                    ref var miner = ref factorySystem.minerPool[i];
                    if (miner.id != i || miner.entityId == 0) continue;
                    _totalScanned++;

                    string status = GetMinerStatus(ref miner, factory);
                    if (status != "Working")
                    {
                        _bottlenecks.Add(new BottleneckInfo
                        {
                            planetName = planetName,
                            machineName = GetMachineName(factory, miner.entityId),
                            status = status
                        });
                    }
                }

                // Ejectors
                for (int i = 1; i < factorySystem.ejectorCursor; i++)
                {
                    ref var ejector = ref factorySystem.ejectorPool[i];
                    if (ejector.id != i || ejector.entityId == 0) continue;
                    _totalScanned++;

                    string status = GetEjectorStatus(ref ejector, factory);
                    if (status != "Working")
                    {
                        _bottlenecks.Add(new BottleneckInfo
                        {
                            planetName = planetName,
                            machineName = GetMachineName(factory, ejector.entityId),
                            status = status
                        });
                    }
                }

                // Silos
                for (int i = 1; i < factorySystem.siloCursor; i++)
                {
                    ref var silo = ref factorySystem.siloPool[i];
                    if (silo.id != i || silo.entityId == 0) continue;
                    _totalScanned++;

                    string status = GetSiloStatus(ref silo, factory);
                    if (status != "Working")
                    {
                        _bottlenecks.Add(new BottleneckInfo
                        {
                            planetName = planetName,
                            machineName = GetMachineName(factory, silo.entityId),
                            status = status
                        });
                    }
                }
            }
        }

        private string GetAssemblerStatus(ref AssemblerComponent assembler, PlanetFactory factory)
        {
            if (assembler.recipeId == 0) return "No Recipe";

            string power = GetPowerStatus(assembler.pcId, factory);
            if (power != null) return power;

            var recipe = LDB.recipes.Select(assembler.recipeId);
            if (recipe != null)
            {
                for (int j = 0; j < recipe.Items.Length; j++)
                {
                    int reqId = recipe.Items[j];
                    if (reqId > 0 && assembler.served[j] < recipe.ItemCounts[j])
                        return "Missing " + LDB.items.Select(reqId).Name.Translate();
                }

                for (int j = 0; j < recipe.Results.Length; j++)
                {
                    int prodId = recipe.Results[j];
                    if (prodId <= 0) continue;

                    int limit = 0;
                    switch (assembler.recipeType)
                    {
                        case ERecipeType.Smelt: limit = 100; break;
                        case ERecipeType.Assemble: limit = recipe.ResultCounts[j] * 9; break;
                        default: limit = recipe.ResultCounts[j] * 19; break;
                    }
                    if (assembler.produced[j] >= limit)
                        return "Output Full: " + LDB.items.Select(prodId).Name.Translate();
                }
            }

            return "Idle";
        }

        private string GetLabStatus(ref LabComponent lab, PlanetFactory factory)
        {
            string power = GetPowerStatus(lab.pcId, factory);
            if (power != null) return power;

            if (lab.researchMode)
            {
                for (int j = 0; j < 6; j++)
                {
                    int matrixId = LabComponent.matrixIds[j];
                    if (lab.matrixPoints[j] > 0 && lab.matrixServed[j] < lab.matrixPoints[j])
                    {
                        return "Missing " + LDB.items.Select(matrixId).Name.Translate();
                    }
                }
            }
            else
            {
                if (lab.recipeId == 0) return "No Recipe";

                var recipe = LDB.recipes.Select(lab.recipeId);
                if (recipe != null)
                {
                    for (int j = 0; j < recipe.Items.Length; j++)
                    {
                        int reqId = recipe.Items[j];
                        if (reqId > 0 && lab.served[j] < recipe.ItemCounts[j])
                            return "Missing " + LDB.items.Select(reqId).Name.Translate();
                    }

                    for (int j = 0; j < recipe.Results.Length; j++)
                    {
                        int prodId = recipe.Results[j];
                        if (prodId <= 0) continue;

                        int limit = 10 * ((lab.speedOverride + 9999) / 10000);
                        if (lab.produced[j] >= limit)
                            return "Output Full: " + LDB.items.Select(prodId).Name.Translate();
                    }
                }
            }

            return "Idle";
        }

        private string GetFracStatus(ref FractionatorComponent frac, PlanetFactory factory)
        {
            string power = GetPowerStatus(frac.pcId, factory);
            if (power != null) return power;

            if (frac.fluidInputCount <= 0)
                return "Missing Input";
            
            if (frac.productOutputCount >= frac.productOutputMax)
                return "Output Full (Product)";
            
            if (frac.fluidOutputCount >= frac.fluidOutputMax)
                return "Output Full (Fluid)";

            return "Idle";
        }

        private string GetMinerStatus(ref MinerComponent miner, PlanetFactory factory)
        {
            string power = GetPowerStatus(miner.pcId, factory);
            if (power != null) return power;

            if (miner.type != EMinerType.Water && miner.veinCount == 0)
                return "No Veins";

            if (miner.productCount >= 50)
                return "Output Full";

            return "Working"; 
        }

        private string GetEjectorStatus(ref EjectorComponent ejector, PlanetFactory factory)
        {
            string power = GetPowerStatus(ejector.pcId, factory);
            if (power != null) return power;

            if (ejector.bulletCount <= 0)
                return "Missing Solar Sails";
            
            if (ejector.orbitId == 0)
                return "No Orbit Set";

            return "Working";
        }

        private string GetSiloStatus(ref SiloComponent silo, PlanetFactory factory)
        {
            string power = GetPowerStatus(silo.pcId, factory);
            if (power != null) return power;

            if (silo.bulletCount <= 0)
                return "Missing Rockets";
            
            if (!silo.hasNode)
                return "No Node Available";

            return "Working";
        }

        private void OnGUI()
        {
            if (!_showUI) return;

            _windowRect = GUILayout.Window(1215, _windowRect, WindowFunction, "Bottleneck UI (Galactic)");
        }

        private void WindowFunction(int windowID)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Manual Scan")) ScanBottlenecks();
            if (GUILayout.Button("Close")) _showUI = false;
            GUILayout.EndHorizontal();

            GUILayout.Label($"Total Machines (All Planets): {_totalScanned} | Bottlenecks: {_bottlenecks.Count}");

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            
            if (_bottlenecks.Count == 0)
            {
                GUILayout.Label("All machines across all planets are working normally!");
            }
            else
            {
                string currentPlanet = "";
                foreach (var info in _bottlenecks)
                {
                    if (currentPlanet != info.planetName)
                    {
                        currentPlanet = info.planetName;
                        GUILayout.Space(10);
                        GUILayout.Label($"--- {currentPlanet} ---", GUI.skin.box);
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(info.machineName, GUILayout.Width(180));
                    
                    Color originalColor = GUI.contentColor;
                    if (info.status.Contains("No Power") || info.status.Contains("Missing") || info.status.Contains("No Veins"))
                        GUI.contentColor = Color.red;
                    else if (info.status.Contains("Low Power") || info.status.Contains("No Recipe") || info.status.Contains("No Orbit") || info.status.Contains("No Node"))
                        GUI.contentColor = Color.yellow;
                    else if (info.status.Contains("Output Full"))
                        GUI.contentColor = Color.cyan;

                    GUILayout.Label(info.status);
                    GUI.contentColor = originalColor;
                    
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        internal void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }

    public struct BottleneckInfo
    {
        public string planetName;
        public string machineName;
        public string status;
    }
}
