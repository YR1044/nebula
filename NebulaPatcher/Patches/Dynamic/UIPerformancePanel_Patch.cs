﻿using HarmonyLib;
using NebulaWorld;

namespace NebulaPatcher.Patches.Dynamic
{
    [HarmonyPatch(typeof(UIPerformancePanel))]
    internal class UIPerformancePanel_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIPerformancePanel.OnDataActiveButtonClick))]
        public static bool OnDataActiveButtonClick_Prefix(UIPerformancePanel __instance)
        {
            if (Multiplayer.IsActive && Multiplayer.Session.LocalPlayer.IsHost)
            {
                // Replace SaveAsLastExit() because it only triggers on UI exit in multiplayer mode
                GameSave.SaveCurrentGame(GameSave.LastExit);
                __instance.RefreshDataStatTexts();
                return false;
            }
            return true;
        }
    }
}
