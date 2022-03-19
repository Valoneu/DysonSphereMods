using HarmonyLib;
using System;
using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace DSPFactorySpaceStations
{
	[HarmonyPatch]
	public static class UIRecipePickerPatch
	{
		public static HashSet<ERecipeType> factorySpaceStationRecipeTypes = new HashSet<ERecipeType>(new []{
			ERecipeType.Assemble,
			ERecipeType.Chemical,
			ERecipeType.Particle,
			ERecipeType.Refine,
			//ERecipeType.Research,
			ERecipeType.Smelt,
		});

		// Filter recipes based on multiple allowed recipe types
		[HarmonyPostfix]
        [HarmonyPatch(typeof(UIRecipePicker), "RefreshIcons")]
        public static void RefreshIconsPostfix(UIRecipePicker __instance)
		{
			var stationWindow = UIRoot.instance.uiGame.stationWindow;
			if (stationWindow == null || !stationWindow.active)
			{
				return;
			}
			var stationId = stationWindow.stationId;
			var station = stationWindow.factory.transport.stationPool[stationId];
			if (station == null)
			{
				return;
			}
			var entity = stationWindow.factory.entityPool[station.entityId];
			if (LDB.items.Select(entity.protoId).ID != DSPFactorySpaceStationsPlugin.factorySpaceStationItem.ID)
            {
				return;
            }

			Array.Clear(__instance.indexArray, 0, __instance.indexArray.Length);
			Array.Clear(__instance.protoArray, 0, __instance.protoArray.Length);
			GameHistoryData history = GameMain.history;
			RecipeProto[] dataArray = LDB.recipes.dataArray;
			IconSet iconSet = GameMain.iconSet;
			for (int i = 0; i < dataArray.Length; i++)
			{
				if (dataArray[i].GridIndex >= 1101 && history.RecipeUnlocked(dataArray[i].ID) && factorySpaceStationRecipeTypes.Contains(dataArray[i].Type))
				{
					int num = dataArray[i].GridIndex / 1000;
					int num2 = (dataArray[i].GridIndex - num * 1000) / 100 - 1;
					int num3 = dataArray[i].GridIndex % 100 - 1;
					if (num2 >= 0 && num3 >= 0 && num2 < 7 && num3 < 12)
					{
						int num4 = num2 * 12 + num3;
						if (num4 >= 0 && num4 < __instance.indexArray.Length && num == __instance.currentType)
						{
							__instance.indexArray[num4] = iconSet.recipeIconIndex[dataArray[i].ID];
							__instance.protoArray[num4] = dataArray[i];
						}
					}
				}
			}
        }
    }
}
