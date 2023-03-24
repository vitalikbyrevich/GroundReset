using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using static GroundReset.Plugin;

namespace GroundReset
{
    internal static class Reseter
    {
        static int key = "TCData".GetStableHashCode();

        internal static void ResetAllTerrains()
        {
            TerrainComp.m_instances.ForEach(terrainComp => { ResetTerrainComp(terrainComp); });
            TerrainComp.UpgradeTerrain();
        }
        internal static void ResetTerrainComp(TerrainComp terrainComp)
        {
            // var zdo = terrainComp.m_nview.GetZDO();
            // for (int i = 0; i < terrainComp.m_modifiedHeight.Length; i++) terrainComp.m_modifiedHeight[i] = false;
            // for (int i = 0; i < terrainComp.m_levelDelta.Length; i++) terrainComp.m_levelDelta[i] = 0;
            // for (int i = 0; i < terrainComp.m_smoothDelta.Length; i++) terrainComp.m_smoothDelta[i] = 0;
            // for (int i = 0; i < terrainComp.m_modifiedPaint.Length; i++) terrainComp.m_modifiedPaint[i] = false;
            // for (int i = 0; i < terrainComp.m_paintMask.Length; i++) terrainComp.m_paintMask[i] = Color.clear;
            // terrainComp.Save();
            // terrainComp.m_hmap.Poke(true);
            // ClutterSystem.instance.ResetGrass(terrainComp.transform.position, 45);
            // foreach (TerrainModifier terrainModifier in TerrainModifier.GetAllInstances())
            // {
            //     Vector3 position = terrainModifier.transform.position;
            //     ZNetView nview = terrainModifier.GetComponent<ZNetView>();
            //     if (nview != null && nview.IsValid() && nview.IsOwner())
            //     {
            //         if (terrainComp.m_hmap.TerrainVSModifier(terrainModifier)) nview.Destroy();
            //     }
            // }

            int resets = 0;
            List<TerrainModifier> allInstances = TerrainModifier.GetAllInstances();
            foreach (TerrainModifier terrainModifier in allInstances)
            {
                Vector3 position = terrainModifier.transform.position;
                ZNetView nview = terrainModifier.GetComponent<ZNetView>();
                if (nview != null && nview.IsValid())
                {
                    Debug($"TerrainModifier {position}");
                    resets++;
                    if (terrainComp.m_hmap.TerrainVSModifier(terrainModifier))
                        terrainComp.m_hmap.Poke(true);
                    nview.Destroy();
                }
            }

            Debug($"Reset {resets} mod edits");

            Traverse traverse = Traverse.Create(terrainComp);

            if (!traverse.Field("m_initialized").GetValue<bool>())
                return;

            terrainComp.m_hmap.WorldToVertex(terrainComp.m_hmap.GetCenter(), out int x, out int y);

            bool[] m_modifiedHeight = traverse.Field("m_modifiedHeight").GetValue<bool[]>();
            float[] m_levelDelta = traverse.Field("m_levelDelta").GetValue<float[]>();
            float[] m_smoothDelta = traverse.Field("m_smoothDelta").GetValue<float[]>();
            bool[] m_modifiedPaint = traverse.Field("m_modifiedPaint").GetValue<bool[]>();
            Color[] m_paintMask = traverse.Field("m_paintMask").GetValue<Color[]>();

            int m_width = traverse.Field("m_width").GetValue<int>();

            Debug($"Checking heightmap at {terrainComp.transform.position}");
            int thisResets = 0;
            bool thisReset = false;
            int num = m_width + 1;
            for (int h = 0; h < num; h++)
            {
                for (int w = 0; w < num; w++)
                {
                    int idx = h * num + w;

                    if (!m_modifiedHeight[idx])
                        continue;

                    var inWard = PrivateArea.InsideFactionArea(VertexToWorld(terrainComp.m_hmap, w, h),
                        Character.Faction.Players);
                    Debug(
                        $"Player coord {x},{y} coord {w},{h}, distance {CoordDistance(x, y, w, h)} has edits, inWard {inWard}");

                    if (inWard)
                        continue;

                    Debug("In range, resetting");

                    resets++;
                    thisResets++;
                    thisReset = true;

                    m_modifiedHeight[idx] = false;
                    m_levelDelta[idx] -= 0.5f;
                    m_smoothDelta[idx] -= 0.5f;
                }
            }

            num = m_width;
            for (int h = 0; h < num; h++)
            {
                for (int w = 0; w < num; w++)
                {
                    int idx = h * num + w;

                    if (!m_modifiedPaint[idx])
                        continue;

                    if (PrivateArea.InsideFactionArea(VertexToWorld(terrainComp.m_hmap, w, h),
                            Character.Faction.Players))
                        continue;

                    thisReset = true;
                    m_modifiedPaint[idx] = false;
                    m_paintMask[idx] = Color.clear;
                }
            }

            if (thisReset)
            {
                traverse.Field("m_modifiedHeight").SetValue(m_modifiedHeight);
                traverse.Field("m_levelDelta").SetValue(m_levelDelta);
                traverse.Field("m_smoothDelta").SetValue(m_smoothDelta);
                traverse.Field("m_modifiedPaint").SetValue(m_modifiedPaint);
                traverse.Field("m_paintMask").SetValue(m_paintMask);

                traverse.Method("Save").GetValue();
                terrainComp.m_hmap.Poke(true);
            }

            if (resets > 0 && ClutterSystem.instance)
                ClutterSystem.instance.ResetGrass(terrainComp.transform.position, 45);

            return;
        }

        // private static void ResetTerrainOLD(Heightmap heightmap)
        // {
        //     RecordDataInWards();
        //
        //     TerrainComp terrainComp = TerrainComp.FindTerrainCompiler(heightmap.transform.position);
        //     if (!terrainComp) return;
        //     ZDO zDO = terrainComp?.m_nview?.GetZDO();
        //     if (zDO == null || zDO.m_byteArrays == null || !zDO.m_byteArrays.ContainsKey(key)) return;
        //     zDO.m_byteArrays.Remove(key);
        //
        //     if (ClutterSystem.instance)
        //     {
        //         ClutterSystem.instance.ResetGrass(heightmap.GetCenter(), 45);
        //     }
        //
        //     heightmap.Poke(false);
        // }

        //private static bool ChechWard(Vector3 center)
        //{
        //    float radius = 60;
        //    return PrivateArea.m_allAreas.Any(x => x.m_ownerFaction == Character.Faction.Players && Utils.DistanceXZ(center, x.transform.position) <= radius);
        //}

        // public static void RecordDataInWards()
        // {
        //     foreach (PrivateArea ward in PrivateArea.m_allAreas)
        //     {
        //         if (!ward) continue;
        //         List<Heightmap> heightmaps = new();
        //         Heightmap.FindHeightmap(ward.transform.position, ward.m_radius + 45, heightmaps);
        //         //TerrainComp terrainComp = TerrainComp.FindTerrainCompiler(ward.transform.position);
        //         foreach (Heightmap heightmap in heightmaps)
        //         {
        //             TerrainComp terrainComp = TerrainComp.FindTerrainCompiler(heightmap.transform.position);
        //             if (!terrainComp) continue;
        //
        //             byte[] byteArray = terrainComp.m_nview.GetZDO().GetByteArray("TCData");
        //             Debug($"RecordDataInWards 1, byteArray = '{byteArray}'");
        //             if (byteArray == null) continue;
        //             var zdo = ward.m_nview.GetZDO();
        //             zdo.Set($"TCData_{terrainComp.transform.position}", byteArray);
        //             zdo.Set("NeedToReturn", true);
        //             zdo.m_byteArrays.Remove(key);
        //         }
        //     }
        // }

        // public static void LoadAllWards()
        // {
        //     foreach (PrivateArea ward in PrivateArea.m_allAreas)
        //     {
        //         LoadAreaOfWard(ward);
        //     }
        // }

        // internal static void LoadAreaOfWard(PrivateArea ward)
        // {
        //     TerrainOp.Settings modifier = new()
        //     {
        //         m_smooth = true,
        //         m_smoothPower = 999,
        //         m_smoothRadius = 3
        //     };
        //     bool flag = ward.m_nview.GetZDO().GetBool("NeedToReturn", false);
        //     if (flag)
        //     {
        //         ward.m_areaMarker.CreateSegments();
        //         foreach (var segment in ward.m_areaMarker.m_segments)
        //         {
        //             var terrainComp = TerrainComp.FindTerrainCompiler(segment.transform.position);
        //             if (!terrainComp) continue;
        //             byte[] byteArray = ward.m_nview.GetZDO().GetByteArray($"TCData_{terrainComp.transform.position}");
        //             if (byteArray == null) continue;
        //             terrainComp.m_nview.GetZDO().Set($"TCData", byteArray);
        //             terrainComp.Save();
        //             terrainComp.m_hmap.Poke(true);
        //             terrainComp.DoOperation(segment.transform.position, modifier);
        //             terrainComp.m_nview.GetZDO().m_dataRevision--;
        //         }
        //
        //         ward.m_nview.GetZDO().Set("NeedToReturn", false);
        //         Chat.instance.SetNpcText(ward.gameObject, Vector3.up * 1.5f, 20f, 2.5f, "", "I given ur terrain back",
        //             false);
        //     }
        // }

        public static IEnumerator WateForWards(TerrainComp terrainComp)
        {
            yield return new WaitForSeconds(15f);

            ResetTerrainComp(terrainComp);
            Debug($"Terrain Reseted");
        }
        public static Vector3 VertexToWorld(Heightmap heightmap, int x, int y)
        {
            float xPos = ((float)x - heightmap.m_width / 2) * heightmap.m_scale;
            float zPos = ((float)y - heightmap.m_width / 2) * heightmap.m_scale;
            return heightmap.transform.position + new Vector3(xPos, 0f, zPos);
        }
        private static float CoordDistance(float x, float y, float rx, float ry)
        {
            float num = x - rx;
            float num2 = y - ry;
            return Mathf.Sqrt(num * num + num2 * num2);
        }
    }
}
