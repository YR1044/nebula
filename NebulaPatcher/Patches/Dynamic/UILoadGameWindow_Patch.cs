﻿using HarmonyLib;
using NebulaModel;
using NebulaModel.Logger;
using NebulaNetwork;
using NebulaWorld;

namespace NebulaPatcher.Patches.Dynamic
{
    [HarmonyPatch(typeof(UILoadGameWindow))]
    internal class UILoadGameWindow_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(UILoadGameWindow.DoLoadSelectedGame))]
        public static void DoLoadSelectedGame_Postfix()
        {
            if (Multiplayer.IsInMultiplayerMenu)
            {
                Log.Info($"Listening server on port {Config.Options.HostPort}");
                Multiplayer.HostGame(new Server(Config.Options.HostPort, true));
            }
        }
    }
}
