using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GroundReset.Plugin;

namespace GroundReset
{
    internal static class Reseter
    {
        internal static int ResetTerrain(Vector3 center, float radius)
        {
            if(ChechWard(center, radius)) return 0;
            int resets = 0;
            List<Heightmap> list = new();


            Heightmap.FindHeightmap(center, radius + 100, list);


            List<TerrainModifier> allInstances = TerrainModifier.GetAllInstances();
            foreach(TerrainModifier terrainModifier in allInstances)
            {
                Vector3 position = terrainModifier.transform.position;
                ZNetView nview = terrainModifier.GetComponent<ZNetView>();
                if(nview != null && nview.IsValid() && nview.IsOwner() && Utils.DistanceXZ(position, center) <= radius)
                {
                    //Debug($"TerrainModifier {position}, player {playerPos}, distance: {Utils.DistanceXZ(position, playerPos)}");
                    resets++;
                    foreach(Heightmap heightmap in list)
                    {
                        if(heightmap.TerrainVSModifier(terrainModifier))
                            heightmap.Poke(true);
                    }
                    nview.Destroy();
                }
            }
            //Debug($"Reset {resets} mod edits");

            using(List<Heightmap>.Enumerator enumerator = list.GetEnumerator())
            {
                while(enumerator.MoveNext())
                {
                    TerrainComp terrainComp = TerrainComp.FindTerrainCompiler(enumerator.Current.transform.position);
                    if(!terrainComp)
                        continue;




                    Traverse traverse = Traverse.Create(terrainComp);

                    if(!traverse.Field("m_initialized").GetValue<bool>())
                        continue;

                    enumerator.Current.WorldToVertex(center, out int x, out int y);

                    bool[] m_modifiedHeight = traverse.Field("m_modifiedHeight").GetValue<bool[]>();
                    float[] m_levelDelta = traverse.Field("m_levelDelta").GetValue<float[]>();
                    float[] m_smoothDelta = traverse.Field("m_smoothDelta").GetValue<float[]>();
                    bool[] m_modifiedPaint = traverse.Field("m_modifiedPaint").GetValue<bool[]>();
                    Color[] m_paintMask = traverse.Field("m_paintMask").GetValue<Color[]>();

                    int m_width = traverse.Field("m_width").GetValue<int>();

                    //Debug($"Checking heightmap at {terrainComp.transform.position}");
                    int thisResets = 0;
                    bool thisReset = false;
                    int num = m_width + 1;
                    for(int h = 0; h < num; h++)
                    {
                        for(int w = 0; w < num; w++)
                        {

                            int idx = h * num + w;

                            if(!m_modifiedHeight[idx])
                                continue;

                            //Debug($"Player coord {x},{y} coord {w},{h}, distance {CoordDistance(x, y, w, h)} has edits. ");

                            if(CoordDistance(x, y, w, h) > radius)
                                continue;

                            //Debug("In range, resetting");

                            resets++;
                            thisResets++;
                            thisReset = true;

                            m_modifiedHeight[idx] = false;
                            m_levelDelta[idx] = 0;
                            m_smoothDelta[idx] = 0;
                        }
                    }

                    num = m_width;
                    for(int h = 0; h < num; h++)
                    {
                        for(int w = 0; w < num; w++)
                        {

                            int idx = h * num + w;

                            if(!m_modifiedPaint[idx])
                                continue;

                            if(CoordDistance(x, y, w, h) > radius)
                                continue;

                            thisReset = true;
                            m_modifiedPaint[idx] = false;
                            m_paintMask[idx] = Color.clear;
                        }
                    }

                    if(thisReset)
                    {
                        //Debug($"\tReset {thisResets} comp edits");

                        traverse.Field("m_modifiedHeight").SetValue(m_modifiedHeight);
                        traverse.Field("m_levelDelta").SetValue(m_levelDelta);
                        traverse.Field("m_smoothDelta").SetValue(m_smoothDelta);
                        traverse.Field("m_modifiedPaint").SetValue(m_modifiedPaint);
                        traverse.Field("m_paintMask").SetValue(m_paintMask);

                        traverse.Method("Save").GetValue();
                        enumerator.Current.Poke(true);
                    }

                }
            }

            if(resets > 0 && ClutterSystem.instance)
                ClutterSystem.instance.ResetGrass(center, radius);

            return resets;
        }

        private static bool ChechWard(Vector3 center, float radius)
        {
            return PrivateArea.m_allAreas.Any(x => x.m_ownerFaction == Character.Faction.Players && x.IsInside(center, radius));
        }

        private static float CoordDistance(float x, float y, float rx, float ry)
        {
            float num = x - rx;
            float num2 = y - ry;
            return Mathf.Sqrt(num * num + num2 * num2);
        }

        public static IEnumerator WateForReset(Vector3 center, float radius)
        {
            yield return new WaitForSeconds(15f);

            int v = ResetTerrain(center, radius);
            Debug($"{v} Terrains Reseted");
        }
    }
}
