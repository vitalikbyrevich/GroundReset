using CodeMonkey.Utils;
using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GroundReset.Plugin;

namespace GroundReset
{
    [HarmonyPatch]
    internal class Patch
    {
        [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.Load)), HarmonyPrefix]
        public static bool TerrainLoad_ResetItsDataIfTimerCompleted(TerrainComp __instance, ref bool __result)
        {
            __result = true;
            Debug("TerrainLoad_ResetItsDataIfTimerCompleted 0");
            ZDO zdo = __instance.m_nview.GetZDO();
            Debug("TerrainLoad_ResetItsDataIfTimerCompleted 1");
            string json = zdo.GetString($"{ModName} time", "");
            Debug("TerrainLoad_ResetItsDataIfTimerCompleted 2");
            if(string.IsNullOrEmpty(json))
            {
            Debug("TerrainLoad_ResetItsDataIfTimerCompleted 3");
                zdo.Set($"{ModName} time", DateTime.MinValue.ToString());
                return true;
            }
            if(json == lastReset.ToString()) return true;

            Debug("TerrainLoad_ResetItsDataIfTimerCompleted 4");
            DateTime time = Convert.ToDateTime(json); if(time == null) return true;
            Debug($"Saved time is {json}, lastResetTime is {lastReset}");



            Debug("TerrainLoad_ResetItsDataIfTimerCompleted 5");
            bool ward = IsPointInsideWard(__instance.transform.position);
            if(ward) return true;
            Debug($"Reset Terrain");

            __result = true;
            Debug("TerrainLoad_ResetItsDataIfTimerCompleted 6");
            zdo.Set($"{ModName} time", lastReset.ToString());
            Debug("TerrainLoad_ResetItsDataIfTimerCompleted 7");
            zdo.m_byteArrays?.Remove("TCData".GetStableHashCode());
            Debug("TerrainLoad_ResetItsDataIfTimerCompleted 8");
            return false;
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPrefix]
        public static void ZNetSceneAwake_StartTimer(ZNetScene __instance)
        {
            if(SceneManager.GetActiveScene().name != "main") return;

            float time = -1;
            if(timePassed > 0) time = timeInMinutes - timePassed;
            else time = timeInMinutes;

            time *= 60;


            FunctionTimer.Create(onTimer, time * 60, "JF_GroundReset", true, true);
        }

        public static bool IsPointInsideWard(Vector3 point)
        {
            foreach(PrivateArea allArea in PrivateArea.m_allAreas)
            {
                if(allArea.m_ownerFaction == Character.Faction.Players && allArea.IsInside(point, 0.0f))
                    return true;
            }
            return false;
        }
    }
}