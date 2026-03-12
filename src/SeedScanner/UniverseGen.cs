using System;
using System.Collections.Generic;
using UnityEngine;
#nullable disable
public static class UniverseGen
{
  public static int algoVersion = 20200403;
  private static int[] tmp_state = (int[]) null;
  public static void Start() => PlanetModelingManager.Start();
  public static void End() => PlanetModelingManager.End();
  public static void Update() => PlanetModelingManager.Update();
  public static GalaxyData CreateGalaxy(GameDesc gameDesc)
  {
    int galaxyAlgo = gameDesc.galaxyAlgo;
    int galaxySeed = gameDesc.galaxySeed;
    int starCount = gameDesc.starCount;
    if (galaxyAlgo < 20200101 || galaxyAlgo > 20591231)
      throw new Exception("Wrong version of unigen algorithm!");
    PlanetGen.gasCoef = gameDesc.isRareResource ? 0.8f : 1f;
    DotNet35Random dotNet35Random = new DotNet35Random(galaxySeed);
    List<VectorLF3> tmp_poses = new List<VectorLF3>();
    List<VectorLF3> tmp_drunk = new List<VectorLF3>();
    int tempPoses = UniverseGen.GenerateTempPoses(dotNet35Random.Next(), starCount, 4, 2.0, 2.3, 3.5, 0.18, tmp_poses, tmp_drunk);
    GalaxyData galaxy = new GalaxyData();
    galaxy.seed = galaxySeed;
    galaxy.starCount = tempPoses;
    galaxy.stars = new StarData[tempPoses];
    Assert.Positive(tempPoses);
    if (tempPoses <= 0)
      return galaxy;
    float num1 = (float) dotNet35Random.NextDouble();
    float num2 = (float) dotNet35Random.NextDouble();
    float num3 = (float) dotNet35Random.NextDouble();
    float num4 = (float) dotNet35Random.NextDouble();
    int num5 = Mathf.CeilToInt((float) (0.0099999997764825821 * (double) tempPoses + (double) num1 * 0.30000001192092896));
    int num6 = Mathf.CeilToInt((float) (0.0099999997764825821 * (double) tempPoses + (double) num2 * 0.30000001192092896));
    int num7 = Mathf.CeilToInt((float) (0.016000000759959221 * (double) tempPoses + (double) num3 * 0.40000000596046448));
    int num8 = Mathf.CeilToInt((float) (0.013000000268220901 * (double) tempPoses + (double) num4 * 1.3999999761581421));
    int num9 = tempPoses - num5;
    int num10 = num9 - num6;
    int num11 = num10 - num7;
    int num12 = (num11 - 1) / num8;
    int num13 = num12 / 2;
    for (int index = 0; index < tempPoses; ++index)
    {
      int seed = dotNet35Random.Next();
      if (index == 0)
      {
        galaxy.stars[index] = StarGen.CreateBirthStar(galaxy, gameDesc, seed);
      }
      else
      {
        ESpectrType needSpectr = ESpectrType.X;
        if (index == 3)
          needSpectr = ESpectrType.M;
        else if (index == num11 - 1)
          needSpectr = ESpectrType.O;
        EStarType needtype = EStarType.MainSeqStar;
        if (index % num12 == num13)
          needtype = EStarType.GiantStar;
        if (index >= num9)
          needtype = EStarType.BlackHole;
        else if (index >= num10)
          needtype = EStarType.NeutronStar;
        else if (index >= num11)
          needtype = EStarType.WhiteDwarf;
        galaxy.stars[index] = StarGen.CreateStar(galaxy, tmp_poses[index], gameDesc, index + 1, seed, needtype, needSpectr);
      }
    }
    AstroData[] astrosData = galaxy.astrosData;
    StarData[] stars = galaxy.stars;
    for (int index = 0; index < galaxy.astrosData.Length; ++index)
    {
      astrosData[index].uRot.w = 1f;
      astrosData[index].uRotNext.w = 1f;
    }
    for (int index = 0; index < tempPoses; ++index)
    {
      StarGen.CreateStarPlanets(galaxy, stars[index], gameDesc);
      int astroId = stars[index].astroId;
      astrosData[astroId].id = astroId;
      astrosData[astroId].type = EAstroType.Star;
      astrosData[astroId].uPos = astrosData[astroId].uPosNext = stars[index].uPosition;
      astrosData[astroId].uRot = astrosData[astroId].uRotNext = Quaternion.identity;
      astrosData[astroId].uRadius = stars[index].physicsRadius;
    }
    galaxy.UpdatePoses(0.0);
    galaxy.birthPlanetId = 0;
    if (tempPoses > 0)
    {
      StarData starData = stars[0];
      for (int index = 0; index < starData.planetCount; ++index)
      {
        PlanetData planet = starData.planets[index];
        ThemeProto themeProto = LDB.themes.Select(planet.theme);
        if (themeProto != null && themeProto.Distribute == EThemeDistribute.Birth)
        {
          galaxy.birthPlanetId = planet.id;
          galaxy.birthStarId = starData.id;
          break;
        }
      }
    }
    Assert.Positive(galaxy.birthPlanetId);
    UniverseGen.CreateGalaxyStarGraph(galaxy);
    PlanetGen.gasCoef = 1f;
    return galaxy;
  }
  private static int GenerateTempPoses(
    int seed,
    int targetCount,
    int iterCount,
    double minDist,
    double minStepLen,
    double maxStepLen,
    double flatten,
    List<VectorLF3> tmp_poses,
    List<VectorLF3> tmp_drunk)
  {
    tmp_poses.Clear();
    tmp_drunk.Clear();
    if (iterCount < 1)
      iterCount = 1;
    else if (iterCount > 16)
      iterCount = 16;
    UniverseGen.RandomPoses(seed, targetCount * iterCount, minDist, minStepLen, maxStepLen, flatten, tmp_poses, tmp_drunk);
    for (int index = tmp_poses.Count - 1; index >= 0; --index)
    {
      if (index % iterCount != 0)
        tmp_poses.RemoveAt(index);
      if (tmp_poses.Count <= targetCount)
        break;
    }
    return tmp_poses.Count;
  }
  private static void RandomPoses(
    int seed,
    int maxCount,
    double minDist,
    double minStepLen,
    double maxStepLen,
    double flatten,
    List<VectorLF3> tmp_poses,
    List<VectorLF3> tmp_drunk)
  {
    DotNet35Random dotNet35Random = new DotNet35Random(seed);
    double num1 = dotNet35Random.NextDouble();
    tmp_poses.Add(VectorLF3.zero);
    int num2 = 6;
    int num3 = 8;
    if (num2 < 1)
      num2 = 1;
    if (num3 < 1)
      num3 = 1;
    double num4 = (double) (num3 - num2);
    int num5 = (int) (num1 * num4 + (double) num2);
    for (int index = 0; index < num5; ++index)
    {
      int num6 = 0;
      while (num6++ < 256)
      {
        double num7 = dotNet35Random.NextDouble() * 2.0 - 1.0;
        double num8 = (dotNet35Random.NextDouble() * 2.0 - 1.0) * flatten;
        double num9 = dotNet35Random.NextDouble() * 2.0 - 1.0;
        double num10 = dotNet35Random.NextDouble();
        double d = num7 * num7 + num8 * num8 + num9 * num9;
        if (d <= 1.0 && d >= 1E-08)
        {
          double num11 = Math.Sqrt(d);
          double num12 = (num10 * (maxStepLen - minStepLen) + minDist) / num11;
          VectorLF3 pt = new VectorLF3(num7 * num12, num8 * num12, num9 * num12);
          if (!UniverseGen.CheckCollision(tmp_poses, pt, minDist))
          {
            tmp_drunk.Add(pt);
            tmp_poses.Add(pt);
            if (tmp_poses.Count >= maxCount)
              return;
            break;
          }
        }
      }
    }
    int num13 = 0;
    while (num13++ < 256)
    {
      for (int index = 0; index < tmp_drunk.Count; ++index)
      {
        if (dotNet35Random.NextDouble() <= 0.7)
        {
          int num14 = 0;
          while (num14++ < 256)
          {
            double num15 = dotNet35Random.NextDouble() * 2.0 - 1.0;
            double num16 = (dotNet35Random.NextDouble() * 2.0 - 1.0) * flatten;
            double num17 = dotNet35Random.NextDouble() * 2.0 - 1.0;
            double num18 = dotNet35Random.NextDouble();
            double d = num15 * num15 + num16 * num16 + num17 * num17;
            if (d <= 1.0 && d >= 1E-08)
            {
              double num19 = Math.Sqrt(d);
              double num20 = (num18 * (maxStepLen - minStepLen) + minDist) / num19;
              VectorLF3 pt = new VectorLF3(tmp_drunk[index].x + num15 * num20, tmp_drunk[index].y + num16 * num20, tmp_drunk[index].z + num17 * num20);
              if (!UniverseGen.CheckCollision(tmp_poses, pt, minDist))
              {
                tmp_drunk[index] = pt;
                tmp_poses.Add(pt);
                if (tmp_poses.Count >= maxCount)
                  return;
                break;
              }
            }
          }
        }
      }
    }
  }
  private static bool CheckCollision(List<VectorLF3> pts, VectorLF3 pt, double min_dist)
  {
    double num1 = min_dist * min_dist;
    foreach (VectorLF3 pt1 in pts)
    {
      double num2 = pt.x - pt1.x;
      double num3 = pt.y - pt1.y;
      double num4 = pt.z - pt1.z;
      if (num2 * num2 + num3 * num3 + num4 * num4 < num1)
        return true;
    }
    return false;
  }
  public static void CreateGalaxyStarGraph(GalaxyData galaxy)
  {
    galaxy.graphNodes = new StarGraphNode[galaxy.starCount];
    for (int index1 = 0; index1 < galaxy.starCount; ++index1)
    {
      galaxy.graphNodes[index1] = new StarGraphNode(galaxy.stars[index1]);
      StarGraphNode graphNode1 = galaxy.graphNodes[index1];
      for (int index2 = 0; index2 < index1; ++index2)
      {
        StarGraphNode graphNode2 = galaxy.graphNodes[index2];
        if ((graphNode1.pos - graphNode2.pos).sqrMagnitude < 64.0)
        {
          UniverseGen.list_sorted_add(graphNode1.conns, graphNode2);
          UniverseGen.list_sorted_add(graphNode2.conns, graphNode1);
        }
      }
      UniverseGen.line_arragement_for_add_node(graphNode1);
    }
  }
  private static void list_sorted_add(List<StarGraphNode> l, StarGraphNode n)
  {
    int count = l.Count;
    bool flag = false;
    for (int index = 0; index < count; ++index)
    {
      if (l[index].index == n.index)
      {
        flag = true;
        break;
      }
      if (l[index].index > n.index)
      {
        l.Insert(index, n);
        flag = true;
        break;
      }
    }
    if (flag)
      return;
    l.Add(n);
  }
  private static void line_arragement_for_add_node(StarGraphNode node)
  {
    if (UniverseGen.tmp_state == null)
      UniverseGen.tmp_state = new int[128];
    Array.Clear((Array) UniverseGen.tmp_state, 0, UniverseGen.tmp_state.Length);
    Vector3 pos1 = (Vector3) node.pos;
    for (int index1 = 0; index1 < node.conns.Count; ++index1)
    {
      StarGraphNode conn1 = node.conns[index1];
      Vector3 pos2 = (Vector3) conn1.pos;
      for (int index2 = index1 + 1; index2 < node.conns.Count; ++index2)
      {
        StarGraphNode conn2 = node.conns[index2];
        Vector3 pos3 = (Vector3) conn2.pos;
        bool flag = false;
        for (int index3 = 0; index3 < conn1.conns.Count; ++index3)
        {
          if (conn1.conns[index3] == conn2)
          {
            flag = true;
            break;
          }
        }
        if (flag)
        {
          float num1 = (float) (((double) pos2.x - (double) pos1.x) * ((double) pos2.x - (double) pos1.x) + ((double) pos2.y - (double) pos1.y) * ((double) pos2.y - (double) pos1.y) + ((double) pos2.z - (double) pos1.z) * ((double) pos2.z - (double) pos1.z));
          float num2 = (float) (((double) pos3.x - (double) pos1.x) * ((double) pos3.x - (double) pos1.x) + ((double) pos3.y - (double) pos1.y) * ((double) pos3.y - (double) pos1.y) + ((double) pos3.z - (double) pos1.z) * ((double) pos3.z - (double) pos1.z));
          float num3 = (float) (((double) pos2.x - (double) pos3.x) * ((double) pos2.x - (double) pos3.x) + ((double) pos2.y - (double) pos3.y) * ((double) pos2.y - (double) pos3.y) + ((double) pos2.z - (double) pos3.z) * ((double) pos2.z - (double) pos3.z));
          float num4 = (double) num1 > (double) num2 ? ((double) num1 > (double) num3 ? num1 : num3) : ((double) num2 > (double) num3 ? num2 : num3);
          float num5 = (double) num1 < (double) num2 ? ((double) num1 < (double) num3 ? num1 : num3) : ((double) num2 < (double) num3 ? num2 : num3);
          float num6 = (float) (((double) num1 + (double) num2 + (double) num3 - (double) num4 - (double) num5) * 1.0010000467300415);
          float num7 = num5 * 1.01f;
          if ((double) num1 <= (double) num6 || (double) num1 <= (double) num7)
          {
            if (UniverseGen.tmp_state[index1] == 0)
            {
              UniverseGen.list_sorted_add(node.lines, conn1);
              UniverseGen.list_sorted_add(conn1.lines, node);
              UniverseGen.tmp_state[index1] = 1;
            }
          }
          else
          {
            UniverseGen.tmp_state[index1] = -1;
            node.lines.Remove(conn1);
            conn1.lines.Remove(node);
          }
          if ((double) num2 <= (double) num6 || (double) num2 <= (double) num7)
          {
            if (UniverseGen.tmp_state[index2] == 0)
            {
              UniverseGen.list_sorted_add(node.lines, conn2);
              UniverseGen.list_sorted_add(conn2.lines, node);
              UniverseGen.tmp_state[index2] = 1;
            }
          }
          else
          {
            UniverseGen.tmp_state[index2] = -1;
            node.lines.Remove(conn2);
            conn2.lines.Remove(node);
          }
          if ((double) num3 > (double) num6 && (double) num3 > (double) num7)
          {
            conn1.lines.Remove(conn2);
            conn2.lines.Remove(conn1);
          }
        }
      }
      if (UniverseGen.tmp_state[index1] == 0)
      {
        UniverseGen.list_sorted_add(node.lines, conn1);
        UniverseGen.list_sorted_add(conn1.lines, node);
        UniverseGen.tmp_state[index1] = 1;
      }
    }
    Array.Clear((Array) UniverseGen.tmp_state, 0, UniverseGen.tmp_state.Length);
  }
}
