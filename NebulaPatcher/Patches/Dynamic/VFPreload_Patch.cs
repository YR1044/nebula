﻿using HarmonyLib;
using NebulaModel;
using NebulaModel.Logger;
using NebulaWorld;
using NebulaWorld.GameStates;

namespace NebulaPatcher.Patches.Dynamic
{
    [HarmonyPatch(typeof(VFPreload))]
    internal class VFPreload_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoad))]
        public static void InvokeOnLoad_Postfix()
        {
            if (Multiplayer.IsDedicated)
            {
                VFAudio.audioVolume = 0f;
                NebulaModel.Utils.NativeInterop.HideWindow();
                NebulaModel.Utils.NativeInterop.SetConsoleCtrlHandler();
                // Logging to provide progression to user
                Log.Info("VFPreload.InvokeOnLoad");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadHalf))]
        public static void InvokeOnLoadHalf_Postfix()
        {
            if (Multiplayer.IsDedicated)
            {
                Log.Info("VFPreload.InvokeOnLoadHalf");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(VFPreload.InvokeOnLoadWorkEnded))]
        public static void InvokeOnLoadWorkEnded_Postfix()
        {
            if (GameStatesManager.DuringReconnect)
            {
                string ip = "127.0.0.1:8469";
                if (Config.Options.RememberLastIP && !string.IsNullOrWhiteSpace(Config.Options.LastIP))
                {
                    ip = Config.Options.LastIP;
                }

                string password = "";
                if (Config.Options.RememberLastClientPassword && !string.IsNullOrWhiteSpace(Config.Options.LastClientPassword))
                {
                    password = Config.Options.LastClientPassword;
                }

                UIMainMenu_Patch.JoinGame(ip, password);
                GameStatesManager.DuringReconnect = false;
            }

            if (Multiplayer.IsDedicated)
            {
                if (GameStatesManager.ImportedSaveName != null)
                {
                    NebulaPlugin.StartDedicatedServer(GameStatesManager.ImportedSaveName);
                }
                else if (GameStatesManager.NewGameDesc != null)
                {
                    NebulaPlugin.StartDedicatedServer(GameStatesManager.NewGameDesc);
                }
                else
                {
                    Log.Warn("No game start option provided!");
                }
            }
        }
    }
}
