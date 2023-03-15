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

            ZDO zdo = __instance.m_nview.GetZDO();
            string json = zdo.GetString($"{ModName} time", "");
            if(string.IsNullOrEmpty(json))
            {
                zdo.Set($"{ModName} time", DateTime.MinValue.ToString());
                return true;
            }
            if(json == lastReset.ToString()) return true;

            DateTime time = Convert.ToDateTime(json); if(time == null) return true;
            Debug($"Saved time is {json}, lastResetTime is {lastReset}");



            bool ward = IsPointInsideWard(__instance.transform.position);
            if(ward) return true;
            Debug($"Reset Terrain");

            __result = true;
            zdo.Set($"{ModName} time", lastReset.ToString());
            zdo.m_byteArrays?.Remove("TCData".GetStableHashCode());
            return false;
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix]
        public static void ZNetSceneAwake_StartTimer(ZNetScene __instance)
        {
            if(SceneManager.GetActiveScene().name != "main") return;

            float time;
            if(timePassedInMinutes > 0) time = timePassedInMinutes;
            else time = timeInMinutes;

            time *= 60;


            FunctionTimer.Create(onTimer, time, "JF_GroundReset", true, true);
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


        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy)), HarmonyPostfix]
        public static void ZNet_OnShutdown()
        {
            timePassedInMinutesConfig.Value = timer.Timer / 60;
        }

    }
}