using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Market_API;
using UnityEngine;
using static GroundReset.Plugin;

namespace GroundReset
{
    internal static class Reseter
    {
        static int key = "TCData".GetStableHashCode();

        internal static void ResetAllTerrains(bool checkIfNeed = false, bool checkWards = true, bool checkZones = true)
        {
            Task.Run(() => TerrainComp.m_instances.ForEach(terrainComp =>
            {
                var flag = true;
                if (checkIfNeed) flag = IsNeedToReset(terrainComp);
                if (flag)
                {
                    ResetTerrainComp(terrainComp, checkWards, checkZones);
                    terrainComp.m_nview.GetZDO().Set($"{ModName} time", lastReset.ToString());
                }
            }));
        }

        internal static bool IsNeedToReset(TerrainComp terrainComp)
        {
            ZDO zdo = terrainComp.m_nview.GetZDO();
            string json = zdo.GetString($"{ModName} time", "");
            if (string.IsNullOrEmpty(json))
            {
                zdo.Set($"{ModName} time", DateTime.MinValue.ToString());
                return false;
            }

            var flag = json == lastReset.ToString();
            if (flag) return false;

            return true;
        }

        internal static void ResetTerrainComp(TerrainComp terrainComp, bool checkWards = true, bool checkZones = true)
        {
            int resets = 0;
            List<TerrainModifier> allInstances = TerrainModifier.GetAllInstances();
            foreach (TerrainModifier terrainModifier in allInstances)
            {
                Vector3 position = terrainModifier.transform.position;
                ZNetView nview = terrainModifier.GetComponent<ZNetView>();
                if (nview && nview.IsValid())
                {
                    resets++;
                    if (terrainComp.m_hmap.TerrainVSModifier(terrainModifier))
                        terrainComp.m_hmap.Poke(true);
                    nview.Destroy();
                }
            }


            if (!terrainComp.m_initialized)
                return;

            terrainComp.m_hmap.WorldToVertex(terrainComp.m_hmap.GetCenter(), out int x, out int y);

            bool[] m_modifiedHeight = terrainComp.m_modifiedHeight;
            float[] m_levelDelta = terrainComp.m_levelDelta;
            float[] m_smoothDelta = terrainComp.m_smoothDelta;
            bool[] m_modifiedPaint = terrainComp.m_modifiedPaint;
            Color[] m_paintMask = terrainComp.m_paintMask;

            int m_width = terrainComp.m_width;

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

                    var vertexToWorld = VertexToWorld(terrainComp.m_hmap, w, h);
                    if (Utils.DistanceXZ(Player.m_localPlayer.transform.position, vertexToWorld) >= fuckingBugDistance)
                        continue;
                    
                    var inWard = PrivateArea.InsideFactionArea(vertexToWorld,
                        Character.Faction.Players);
                    if (inWard && checkWards)
                        continue;
                    var inZone = Market_API.Marketplace_API.IsPointInsideTerritoryWithFlag(vertexToWorld,
                        Marketplace_API.TerritoryFlags.NoPickaxe, out string name,
                        out Marketplace_API.TerritoryFlags flags,
                        out Marketplace_API.AdditionalTerritoryFlags additionalFlags);
                    if (inZone && checkZones)
                        continue;

                    resets++;
                    thisResets++;
                    thisReset = true;

                    var level = m_levelDelta[idx];
                    var smooth = m_smoothDelta[idx];
                    m_modifiedHeight[idx] = false;
                    m_levelDelta[idx] = 0;
                    m_smoothDelta[idx] = 0;
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

                    var vertexToWorld = VertexToWorld(terrainComp.m_hmap, w, h);
                    if (Utils.DistanceXZ(Player.m_localPlayer.transform.position, vertexToWorld) >= fuckingBugDistance)
                        continue;
                    var inWard = PrivateArea.InsideFactionArea(vertexToWorld,
                        Character.Faction.Players);
                    if (inWard && checkWards)
                        continue;
                    var inZone = Market_API.Marketplace_API.IsPointInsideTerritoryWithFlag(vertexToWorld,
                        Marketplace_API.TerritoryFlags.NoPickaxe, out string name,
                        out Marketplace_API.TerritoryFlags flags,
                        out Marketplace_API.AdditionalTerritoryFlags additionalFlags);
                    if (inZone && checkZones)
                        continue;


                    thisReset = true;
                    m_modifiedPaint[idx] = false;
                    m_paintMask[idx] = Color.clear;
                }
            }

            if (thisReset)
            {
                terrainComp.m_modifiedHeight = m_modifiedHeight;
                terrainComp.m_levelDelta = m_levelDelta;
                terrainComp.m_smoothDelta = m_smoothDelta;
                terrainComp.m_modifiedPaint = m_modifiedPaint;
                terrainComp.m_paintMask = m_paintMask;

                terrainComp.Save();
                terrainComp.m_hmap.Poke(true);
            }

            if (resets > 0 && ClutterSystem.instance)
                ClutterSystem.instance.ResetGrass(terrainComp.transform.position, 45);

            return;
        }

        public static IEnumerator WateForWardsIEnumerator(TerrainComp terrainComp)
        {
            yield return new WaitForSeconds(15f);

            ResetTerrainComp(terrainComp);
        }

        public static IEnumerator ResetAllIEnumerator()
        {
            yield return new WaitForSeconds(35);

            ResetAllTerrains(true);
            _self.StartCoroutine(Reseter.ResetAllIEnumerator());
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

        public static IEnumerator SaveTime()
        {
            yield return new WaitForSeconds(savedTimeUpdateInterval);

            timePassedInMinutesConfig.Value = timer.Timer / 60;
            ZNetScene.instance.StartCoroutine(SaveTime());
        }
    }
}