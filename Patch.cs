using CodeMonkey.Utils;
using HarmonyLib;
using System;
using UnityEngine.SceneManagement;
using static GroundReset.Plugin;

namespace GroundReset
{
    [HarmonyPatch]
    internal class Patch
    {
        [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.Load)), HarmonyPostfix]
        public static void TerrainLoad_ResetItsDataIfTimerCompleted(TerrainComp __instance, ref bool __result)
        {
            ZDO zdo = __instance.m_nview.GetZDO();
            string json = zdo.GetString($"{ModName} time", "");
            if(string.IsNullOrEmpty(json))
            {
                zdo.Set($"{ModName} time", DateTime.MinValue.ToString());
                return;
            }
            if(json == lastReset.ToString()) return;

            DateTime time = Convert.ToDateTime(json); if(time == null) return;

            _self.StartCoroutine(Reseter.WateForReset(__instance.m_hmap.GetCenter(), 45));
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

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy)), HarmonyPostfix]
        public static void ZNet_OnShutdown()
        {
            if(!ZNet.m_isServer) return;

            timePassedInMinutesConfig.Value = timer.Timer / 60;
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix]
        public static void ZNetSceneAwake()
        {
            ZRoutedRpc.instance.Register("ResetTerrain", new Action<long>(_self.RPC_ResetTerrain));
        }
    }
}