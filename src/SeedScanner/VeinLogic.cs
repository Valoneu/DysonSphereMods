using System;
using System.Collections.Generic;
using UnityEngine;
public static class VeinLogic
{
    public static void GenerateVeins(PlanetData planet)
    {
        ThemeProto themeProto = LDB.themes.Select(planet.theme);
        if (themeProto == null) return;
        DotNet35Random rand1 = new DotNet35Random(planet.seed);
        rand1.Next(); rand1.Next(); rand1.Next(); rand1.Next();
        int birthSeed = rand1.Next();
        DotNet35Random rand2 = new DotNet35Random(rand1.Next());
        int[] veinSpots = new int[20];
        float[] veinCounts = new float[20];
        float[] veinOpacities = new float[20];
        if (themeProto.VeinSpot != null)
            Array.Copy(themeProto.VeinSpot, 0, veinSpots, 1, Math.Min(themeProto.VeinSpot.Length, 19));
        if (themeProto.VeinCount != null)
            Array.Copy(themeProto.VeinCount, 0, veinCounts, 1, Math.Min(themeProto.VeinCount.Length, 19));
        if (themeProto.VeinOpacity != null)
            Array.Copy(themeProto.VeinOpacity, 0, veinOpacities, 1, Math.Min(themeProto.VeinOpacity.Length, 19));
        float p = 1f;
        switch (planet.star.type)
        {
            case EStarType.MainSeqStar:
                switch (planet.star.spectr)
                {
                    case ESpectrType.M: p = 2.5f; break;
                    case ESpectrType.K: p = 1f; break;
                    case ESpectrType.G: p = 0.7f; break;
                    case ESpectrType.F: p = 0.6f; break;
                    case ESpectrType.A: p = 1f; break;
                    case ESpectrType.B: p = 0.4f; break;
                    case ESpectrType.O: p = 1.6f; break;
                }
                break;
            case EStarType.GiantStar: p = 2.5f; break;
            case EStarType.WhiteDwarf: p = 3.5f; break;
            case EStarType.NeutronStar: p = 4.5f; break;
            case EStarType.BlackHole: p = 5f; break;
        }
        if (planet.star.type == EStarType.WhiteDwarf) {
            veinSpots[9]++; veinSpots[9]++;
            for (int i = 1; i < 12 && rand1.NextDouble() < 0.45; ++i) veinSpots[9]++;
            veinCounts[9] = 0.7f; veinOpacities[9] = 1f;
            veinSpots[10]++; veinSpots[10]++;
            for (int i = 1; i < 12 && rand1.NextDouble() < 0.45; ++i) veinSpots[10]++;
            veinCounts[10] = 0.7f; veinOpacities[10] = 1f;
            veinSpots[12]++;
            for (int i = 1; i < 12 && rand1.NextDouble() < 0.5; ++i) veinSpots[12]++;
            veinCounts[12] = 0.7f; veinOpacities[12] = 0.3f;
        } else if (planet.star.type == EStarType.NeutronStar || planet.star.type == EStarType.BlackHole) {
            veinSpots[14]++;
            for (int i = 1; i < 12 && rand1.NextDouble() < 0.65; ++i) veinSpots[14]++;
            veinCounts[14] = 0.7f; veinOpacities[14] = 0.3f;
        }
        if (themeProto.RareVeins != null && themeProto.RareSettings != null) {
            for (int i = 0; i < themeProto.RareVeins.Length; ++i)
            {
                int rareVein = themeProto.RareVeins[i];
                float prob = planet.star.index == 0 ? themeProto.RareSettings[i * 4] : themeProto.RareSettings[i * 4 + 1];
                float richness = themeProto.RareSettings[i * 4 + 3];
                float num4 = 1f - Mathf.Pow(1f - prob, p);
                if (rand1.NextDouble() < num4)
                {
                    veinSpots[rareVein]++;
                    veinCounts[rareVein] = richness;
                    veinOpacities[rareVein] = richness;
                    for (int j = 1; j < 12 && rand1.NextDouble() < themeProto.RareSettings[i * 4 + 2]; ++j)
                        veinSpots[rareVein]++;
                }
            }
        }
        float resourceCoef = planet.star.resourceCoef;
        if (planet.galaxy.birthPlanetId == planet.id) resourceCoef *= 0.6666667f;
        for (int i = 1; i < 15; i++)
        {
            int spots = veinSpots[i];
            if (spots == 0) continue;
            float opacity = veinOpacities[i];
            float countMult = veinCounts[i];
            long totalAmount = (long)(spots * 22.5f * opacity * 100000f * resourceCoef * countMult);
            if (totalAmount > 0)
                planet.VeinAmounts[(EVeinType)i] = totalAmount;
        }
    }
}
