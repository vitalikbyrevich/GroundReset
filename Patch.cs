using CodeMonkey.Utils;
using fastJSON;
using HarmonyLib;
using System;
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
            ZDO zdo = __instance.m_nview.GetZDO();
            string json = zdo.GetString($"{ModName} time", "");
            if(string.IsNullOrEmpty(json))
            {
                zdo.Set($"{ModName} time", JSON.ToJSON(DateTime.MinValue));
                return true;
            }
            DateTime time = JSON.ToObject<DateTime>(json);
            if(time == null) return true;

            if(time == lastReset)
            {
                return true;
            }

            PrivateArea ward = IsPointInsideWard(__instance.transform.position);
            bool haveWard = ward != null;
            if(haveWard) return true;

            __result = true;
            __instance.Save();
            zdo.Set($"{ModName} time", JSON.ToJSON(lastReset));
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

        public static PrivateArea IsPointInsideWard(Vector3 point)
        {
            foreach(PrivateArea allArea in PrivateArea.m_allAreas)
            {
                if(allArea.m_ownerFaction == Character.Faction.Players && allArea.IsInside(point, 0.0f))
                    return allArea;
            }
            return null;
        }
    }
}