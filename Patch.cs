using CodeMonkey.Utils;
using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GroundReset.Plugin;

namespace GroundReset
{
    [HarmonyPatch]
    internal class Patch
    {
        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix]
        public static void ZNetSceneAwake_StartTimer(ZNetScene __instance)
        {
            if (SceneManager.GetActiveScene().name != "main") return;

            float time;
            if (timePassedInMinutes > 0) time = timePassedInMinutes;
            else time = timeInMinutes;

            time *= 60;


            FunctionTimer.Create(onTimer, time, "JF_GroundReset", true, true);
            _self.StartCoroutine(Reseter.ResetAllIEnumerator());
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake)), HarmonyPostfix]
        public static void ZNet_OnShutdown(ZNet __instance)
        {
            if (!ZNet.m_isServer) return;
            __instance.StartCoroutine(Reseter.SaveTime());
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix]
        public static void ZNetSceneAwake()
        {
            ZRoutedRpc.instance.Register("ResetTerrain", new Action<long>(_self.RPC_ResetTerrain));
        }
    }
}