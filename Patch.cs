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
        [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.Load)), HarmonyPrefix]
        public static bool TerrainLoad_ResetItsDataIfTimerCompleted(TerrainComp __instance, ref bool __result)
        {
            bool v = ResetTerrain(__instance, out __result);
            return v;
        }

        [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.CheckLoad)), HarmonyPrefix]
        public static void TerrainLoad_ResetCheckLoad(TerrainComp __instance)
        {
            ResetTerrain(__instance, out _);
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