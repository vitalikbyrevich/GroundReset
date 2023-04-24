using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GroundReset;

public static class TerminalCommands
{
    private static string modName => Plugin.ModName;

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    internal class AddChatCommands
    {
        private static void Postfix()
        {
            _ = new Terminal.ConsoleCommand(modName, $"Manages the {modName.Replace(".", "")} commands.",
                args =>
                {
                    if (!Plugin.configSync.IsAdmin && !ZNet.instance.IsServer())
                    {
                        args.Context.AddString("You are not an admin on this server.");
                        return;
                    }

                    if (args.Length == 4 && args[1] == "ResetNearestTerrains")
                    {
                        Reseter.ResetAllTerrains(false, bool.Parse(args[2]), bool.Parse(args[3]));
                    }

                    args.Context.AddString("ResetNearestTerrains [check wards] [chack zones]");
                },
                optionsFetcher: () => new List<string>
                {
                    "ResetNearestTerrains true true", "ResetNearestTerrains true false", "ResetNearestTerrains false true", "ResetNearestTerrains false false"
                });
        }
    }
}