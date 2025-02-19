﻿using HarmonyLib;
using NebulaWorld;

namespace NebulaPatcher.Patches.Dynamic
{
    [HarmonyPatch(typeof(UIStarmap))]
    internal class UIStarmap_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(UIStarmap._OnLateUpdate))]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Original Function Name")]
        public static void _OnLateUpdate_Postfix(UIStarmap __instance)
        {
            if (Multiplayer.IsActive)
            {
                Multiplayer.Session.World.RenderPlayerNameTagsOnStarmap(__instance);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(UIStarmap._OnClose))]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Original Function Name")]
        public static void _OnClose_Postfix()
        {
            if (Multiplayer.IsActive)
            {
                Multiplayer.Session.World.ClearPlayerNameTagsOnStarmap();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIStarmap.StartFastTravelToUPosition))]
        public static bool TeleportToUPosition_Prefix(VectorLF3 uPos)
        {
            if (Multiplayer.IsActive && Multiplayer.Session.LocalPlayer.IsClient)
            {
                GameMain.data.QueryNearestStarPlanet(uPos, out StarData starData, out PlanetData planetData);
                if (GameMain.localPlanet != planetData && planetData != null && planetData.type == EPlanetType.Gas)
                {
                    InGamePopup.ShowWarning("Unavailable", "Cannot teleport to gas giant", "OK");
                    return false;
                }
            }
            return true;
        }
    }
}
