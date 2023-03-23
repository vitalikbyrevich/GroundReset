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
        [HarmonyPatch(typeof(TerrainComp), nameof(TerrainComp.Load)), HarmonyPrefix]
        public static void TerrainLoad_ResetItsDataIfTimerCompleted(TerrainComp __instance, ref bool __result)
        {
            ZDO zdo = __instance.m_nview.GetZDO();
            string json = zdo.GetString($"{ModName} time", "");
            if (string.IsNullOrEmpty(json))
            {
                zdo.Set($"{ModName} time", DateTime.MinValue.ToString());
                return;
            }

            if (json == lastReset.ToString()) return;

            _self.StartCoroutine(Reseter.WateForReset(__instance.m_hmap));
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix]
        public static void ZNetSceneAwake_StartTimer(ZNetScene __instance)
        {
            if (SceneManager.GetActiveScene().name != "main") return;

            float time;
            if (timePassedInMinutes > 0) time = timePassedInMinutes;
            else time = timeInMinutes;

            time *= 60;


            FunctionTimer.Create(onTimer, time, "JF_GroundReset", true, true);
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy)), HarmonyPostfix]
        public static void ZNet_OnShutdown()
        {
            if (!ZNet.m_isServer) return;

            timePassedInMinutesConfig.Value = timer.Timer / 60;
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix]
        public static void ZNetSceneAwake()
        {
            ZRoutedRpc.instance.Register("ResetTerrain", new Action<long>(_self.RPC_ResetTerrain));
        }

        /*[HarmonyPatch(typeof(TerrainOp), nameof(TerrainOp.OnPlaced)), HarmonyPostfix]
        public static void TerrainOpOnPlaced(TerrainOp __instance)
        {
            FindWardOnPosition(__instance.transform.position);
        }*/

        [HarmonyPatch(typeof(PrivateArea), nameof(PrivateArea.Awake)), HarmonyPostfix]
        public static void PrivateAreaAwake(PrivateArea __instance)
        {
            LoadAreaOfWard(__instance);
        }

        private static void LoadAreaOfWard(PrivateArea ward)
        {
            TerrainOp.Settings modifier = new()
            {
                m_smooth = true,
                m_smoothPower = 999,
                m_smoothRadius = 3
            };
            if (ward.m_nview.GetZDO().GetBool("NeedToReturn", false))
            {
                ward.m_areaMarker.CreateSegments();
                foreach (var segment in ward.m_areaMarker.m_segments)
                {
                    var terrainComp = TerrainComp.FindTerrainCompiler(segment.transform.position);
                    if (!terrainComp) continue;
                    byte[] byteArray = ward.m_nview.GetZDO().GetByteArray($"TCData_{terrainComp.transform.position}");
                    if (byteArray == null) continue;
                    terrainComp.m_nview.GetZDO().Set($"TCData", byteArray);
                    terrainComp.DoOperation(segment.transform.position, modifier);
                    terrainComp.m_nview.GetZDO().m_dataRevision--;
                }

                ward.m_nview.GetZDO().Set("NeedToReturn", false);
            }

            Chat.instance.SetNpcText(ward.gameObject, Vector3.up * 1.5f, 20f, 2.5f, "", "I given ur terrain back",
                false);
        }

        //private static void FindWardOnPosition(Vector3 pos)
        //{
        //    PrivateArea.m_allAreas.Any(x => x.transform.position == pos);
        //}
    }
}