﻿using HarmonyLib;
using NebulaModel.Packets.Logistics;
using NebulaWorld;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NebulaPatcher.Patches.Dynamic
{
    [HarmonyPatch(typeof(UIStationWindow))]
    internal class UIStationWindow_Patch
    {
        private static long lastUpdateGametick;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIStationWindow.OnMaxChargePowerSliderValueChange))]
        public static bool OnMaxChargePowerSliderValueChange_Prefix(UIStationWindow __instance, float value)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return true;
            }
            Multiplayer.Session.StationsUI.SliderBarPacket.SettingIndex = StationUI.EUISettings.MaxChargePower;
            Multiplayer.Session.StationsUI.SliderBarPacket.SettingValue = value;
            if (Multiplayer.Session.LocalPlayer.IsClient)
            {
                StringBuilderUtility.WriteKMG(__instance.powerServedSB, 8, (long)(3000000.0 * value + 0.5), false);
                __instance.maxChargePowerValue.text = __instance.powerServedSB.ToString();
            }
            return Multiplayer.Session.LocalPlayer.IsHost;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIStationWindow.OnMaxTripDroneSliderValueChange))]
        public static bool OnMaxTripDroneSliderValueChange_Prefix(UIStationWindow __instance, float value)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return true;
            }
            Multiplayer.Session.StationsUI.SliderBarPacket.SettingIndex = StationUI.EUISettings.MaxTripDrones;
            Multiplayer.Session.StationsUI.SliderBarPacket.SettingValue = value;
            if (Multiplayer.Session.LocalPlayer.IsClient)
            {
                __instance.maxTripDroneValue.text = value.ToString("0 °");
            }
            return Multiplayer.Session.LocalPlayer.IsHost;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIStationWindow.OnMaxTripVesselSliderValueChange))]
        public static bool OnMaxTripVesselSliderValueChange_Prefix(UIStationWindow __instance, float value)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return true;
            }
            Multiplayer.Session.StationsUI.SliderBarPacket.SettingIndex = StationUI.EUISettings.MaxTripVessel;
            Multiplayer.Session.StationsUI.SliderBarPacket.SettingValue = value;
            if (Multiplayer.Session.LocalPlayer.IsClient)
            {
                float num;
                if (value > 40.5f)
                {
                    num = 10000.0f;
                }
                else if (value > 20.5f)
                {
                    num = (value * 2f - 20f);
                }
                else
                {
                    num = value;
                }
                __instance.maxTripVesselValue.text = num < 9999.0f ? num.ToString("0 ly") : "∞";
            }
            return Multiplayer.Session.LocalPlayer.IsHost;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIStationWindow.OnMinDeliverDroneValueChange))]
        public static bool OnMinDeliverDroneValueChange_Prefix(UIStationWindow __instance, float value)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return true;
            }
            Multiplayer.Session.StationsUI.SliderBarPacket.SettingIndex = StationUI.EUISettings.MinDeliverDrone;
            Multiplayer.Session.StationsUI.SliderBarPacket.SettingValue = value;
            if (Multiplayer.Session.LocalPlayer.IsClient)
            {
                int num = (int)(value * 10f + 0.5f);
                if (num < 1)
                {
                    num = 1;
                }
                __instance.minDeliverDroneValue.text = num.ToString("0") + " %";
            }
            return Multiplayer.Session.LocalPlayer.IsHost;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIStationWindow.OnMinDeliverVesselValueChange))]
        public static bool OnMinDeliverVesselValueChange_Prefix(UIStationWindow __instance, float value)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return true;
            }
            Multiplayer.Session.StationsUI.SliderBarPacket.SettingIndex = StationUI.EUISettings.MinDeliverVessel;
            Multiplayer.Session.StationsUI.SliderBarPacket.SettingValue = value;
            if (Multiplayer.Session.LocalPlayer.IsClient)
            {
                int num = (int)(value * 10f + 0.5f);
                num = num < 1 ? 1 : num;
                __instance.minDeliverVesselValue.text = num.ToString("0") + " %";
            }
            return Multiplayer.Session.LocalPlayer.IsHost;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIStationWindow.OnMaxMiningSpeedChange))]
        public static bool OnMaxMiningSpeedChangee_Prefix(UIStationWindow __instance, float value)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return true;
            }
            Multiplayer.Session.StationsUI.SliderBarPacket.SettingIndex = StationUI.EUISettings.MaxMiningSpeed;
            Multiplayer.Session.StationsUI.SliderBarPacket.SettingValue = value;
            if (Multiplayer.Session.LocalPlayer.IsClient)
            {
                int num = 10000 + (int)(value + 0.5f) * 1000;
                __instance.maxMiningSpeedValue.text = (num / 100).ToString("0") + " %";
            }
            return Multiplayer.Session.LocalPlayer.IsHost;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIStationWindow.OnWarperDistanceValueChange))]
        public static bool OnWarperDistanceValueChange_Prefix(UIStationWindow __instance, float value)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return true;
            }
            // make minimum value set at 0.5 ly
            if (value < 2f)
            {
                value = 2f;
                __instance.event_lock = true;
                __instance.warperDistanceSlider.value = 2f;
                __instance.event_lock = false;
            }
            Multiplayer.Session.StationsUI.SliderBarPacket.SettingIndex = StationUI.EUISettings.WarpDistance;
            Multiplayer.Session.StationsUI.SliderBarPacket.SettingValue = value;
            if (Multiplayer.Session.LocalPlayer.IsClient)
            {
                float num;
                if (value < 1.5)
                {
                    num = 0.2f;
                }
                else if (value < 7.5)
                {
                    num = value * 0.5f- 0.5f;
                }
                else if (value < 16.5)
                {
                    num = value - 4f;
                }
                else if (value < 20.5)
                {
                    num = value * 2f - 20f;
                }
                else
                {
                    num = 60.0f;
                }
                 __instance.warperDistanceValue.text = num < 10.0 ? num.ToString("0.0 AU") : num.ToString("0 AU");
            }
            return Multiplayer.Session.LocalPlayer.IsHost;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(UIStationWindow.OnMinPilerValueChange))]
        [HarmonyPatch(nameof(UIStationWindow.OnTechPilerClick))]
        public static void OnPilerCountChange(UIStationWindow __instance)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            StationUI packet = new StationUI(__instance.factory.planet.id, stationComponent.id, stationComponent.gid, StationUI.EUISettings.PilerCount, stationComponent.pilerCount);
            Multiplayer.Session.Network.SendPacket(packet);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIStationWindow.OnWarperNecessaryClick))]
        public static bool OnWarperNecessaryClick_Prefix(UIStationWindow __instance)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return true;
            }
            StationUI packet = new StationUI(__instance.factory.planet.id, __instance.factory.transport.stationPool[__instance.stationId].id, __instance.factory.transport.stationPool[__instance.stationId].gid, StationUI.EUISettings.WarperNeeded, 0f);
            Multiplayer.Session.Network.SendPacket(packet);
            return Multiplayer.Session.LocalPlayer.IsHost;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIStationWindow.OnIncludeOrbitCollectorClick))]
        public static bool OnIncludeOrbitCollectorClick_Prefix(UIStationWindow __instance)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return true;
            }
            StationUI packet = new StationUI(__instance.factory.planet.id, __instance.factory.transport.stationPool[__instance.stationId].id, __instance.factory.transport.stationPool[__instance.stationId].gid, StationUI.EUISettings.IncludeCollectors, 0f);
            Multiplayer.Session.Network.SendPacket(packet);
            return Multiplayer.Session.LocalPlayer.IsHost;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIStationWindow.OnDroneIconClick))]
        [HarmonyPriority(Priority.First)]
        public static void OnDroneIconClick_Prefix(UIStationWindow __instance, ref int __state)
        {
            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            __state = stationComponent.idleDroneCount;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(UIStationWindow.OnDroneIconClick))]
        [HarmonyPriority(Priority.Last)]
        public static void OnDroneIconClick_Posfix(UIStationWindow __instance, int __state)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            if (__state != stationComponent.idleDroneCount)
            {
                int droneCount = stationComponent.idleDroneCount + stationComponent.workDroneCount;
                StationUI packet = new StationUI(__instance.factory.planet.id, stationComponent.id, stationComponent.gid, StationUI.EUISettings.SetDroneCount, droneCount);
                Multiplayer.Session.Network.SendPacket(packet);
                if (Multiplayer.Session.LocalPlayer.IsClient)
                {
                    // Revert drone count until host verify
                    stationComponent.idleDroneCount = __state;
                    __instance.droneIconButton.button.interactable = false;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(UIStationWindow.OnDroneAutoReplenishButtonClick))]
        public static void OnDroneAutoReplenishButtonClick_Postfix(UIStationWindow __instance)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            StationUI packet = new StationUI(__instance.factory.planet.id, stationComponent.id, stationComponent.gid, StationUI.EUISettings.DroneAutoReplenish, stationComponent.droneAutoReplenish ? 1f : 0f);
            Multiplayer.Session.Network.SendPacket(packet);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIStationWindow.OnShipIconClick))]
        [HarmonyPriority(Priority.First)]
        public static void OnShipIconClick_Prefix(UIStationWindow __instance, ref int __state)
        {
            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            __state = stationComponent.idleShipCount;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(UIStationWindow.OnShipIconClick))]
        [HarmonyPriority(Priority.Last)]
        public static void OnShipIconClick_Posfix(UIStationWindow __instance, int __state)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            if (__state != stationComponent.idleShipCount)
            {
                int ShipCount = stationComponent.idleShipCount + stationComponent.workShipCount;
                StationUI packet = new StationUI(__instance.factory.planet.id, stationComponent.id, stationComponent.gid, StationUI.EUISettings.SetShipCount, ShipCount);
                Multiplayer.Session.Network.SendPacket(packet);
                if (Multiplayer.Session.LocalPlayer.IsClient)
                {
                    // Revert ship count until host verify
                    stationComponent.idleShipCount = __state;
                    __instance.shipIconButton.button.interactable = false;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(UIStationWindow.OnShipAutoReplenishButtonClick))]
        public static void OnShipAutoReplenishButtonClick_Postfix(UIStationWindow __instance)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            StationUI packet = new StationUI(__instance.factory.planet.id, stationComponent.id, stationComponent.gid, StationUI.EUISettings.ShipAutoReplenish, stationComponent.shipAutoReplenish ? 1f : 0f);
            Multiplayer.Session.Network.SendPacket(packet);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIStationWindow.OnWarperIconClick))]
        [HarmonyPriority(Priority.First)]
        public static void OnWarperIconClick_Prefix(UIStationWindow __instance, ref int __state)
        {
            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            __state = stationComponent.warperCount;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(UIStationWindow.OnWarperIconClick))]
        [HarmonyPriority(Priority.Last)]
        public static void OnWarperIconClick_Posfix(UIStationWindow __instance, int __state)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            if (__state != stationComponent.warperCount)
            {
                StationUI packet = new StationUI(__instance.factory.planet.id, stationComponent.id, stationComponent.gid, StationUI.EUISettings.SetWarperCount, stationComponent.warperCount);
                Multiplayer.Session.Network.SendPacket(packet);
                if (Multiplayer.Session.LocalPlayer.IsClient)
                {
                    // Revert warper count until host verify
                    stationComponent.warperCount = __state;
                    __instance.warperIconButton.button.interactable = false;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIStationWindow.OnNameInputSubmit))]
        public static bool OnNameInputSubmit_Postfix(UIStationWindow __instance)
        {
            if (__instance.event_lock || !Multiplayer.IsActive || Multiplayer.Session.Ships.PatchLockILS)
            {
                return true;
            }
            StationUI packet = new StationUI(__instance.factory.planet.id, __instance.factory.transport.stationPool[__instance.stationId].id, __instance.factory.transport.stationPool[__instance.stationId].gid, StationUI.EUISettings.NameInput, __instance.nameInput.text.Trim());
            Multiplayer.Session.Network.SendPacket(packet);
            return Multiplayer.Session.LocalPlayer.IsHost;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(UIStationWindow._OnOpen))]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Original Function Name")]
        public static void _OnOpen_Postfix(UIStationWindow __instance)
        {
            if (!Multiplayer.IsActive || __instance.transport == null)
            {
                return;
            }

            StationComponent stationComponent = __instance.transport.stationPool[__instance.stationId];
            Multiplayer.Session.StationsUI.SliderBarPacket = new StationUI(__instance.factory.planet.id, stationComponent.id, stationComponent.gid, StationUI.EUISettings.None, 0);
            Multiplayer.Session.StationsUI.StorageMaxChangeId = -1;
            // We want OnNameInputSubmit only call on nameInput.onEndEdit, so remove listener on onValueChanged
            __instance.nameInput.onValueChanged.RemoveAllListeners();
            if (Multiplayer.Session.LocalPlayer.IsHost)
            {
                return;
            }
            lastUpdateGametick = GameMain.gameTick;

            // Stage 0 : Hide UI elements until sync data arrive
            __instance.titleText.text = "Loading...";
            for (int i = 0; i < __instance.storageUIs.Length; i++)
            {
                __instance.storageUIs[i]._Close();
                __instance.storageUIs[i].ClosePopMenu();
            }
            __instance.panelDown.SetActive(false);

            // for some reason advance miner has planetId set to 0, so we use UI's factory planetId
            Multiplayer.Session.Network.SendPacket(new StationUIInitialSyncRequest(__instance.factory.planetId, stationComponent.id, stationComponent.gid));
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIStationWindow._OnUpdate))]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Original Function Name")]
        public static bool _OnUpdate_Prefix(UIStationWindow __instance)
        {
            if (!Multiplayer.IsActive)
            {
                return true;
            }

            // When releasing left mouse button
            if (Input.GetMouseButtonUp(0))
            {
                if (Multiplayer.Session.StationsUI.SliderBarPacket.SettingIndex != StationUI.EUISettings.None)
                {
                    // Send SliderBarPacket when left mouse button is released
                    Multiplayer.Session.Network.SendPacket(Multiplayer.Session.StationsUI.SliderBarPacket);
                    Multiplayer.Session.StationsUI.SliderBarPacket.SettingIndex = StationUI.EUISettings.None;
                }
                if (Multiplayer.Session.StationsUI.StorageMaxChangeId >= 0)
                {
                    // Do the job in UIStationStorage.OnMaxSliderValueChange()
                    int index = Multiplayer.Session.StationsUI.StorageMaxChangeId;
                    float val = __instance.storageUIs[index].maxSlider.value;
                    StationStore stationStore = __instance.transport.stationPool[__instance.stationId].storage[index];
                    __instance.transport.SetStationStorage(__instance.stationId, index, stationStore.itemId, (int)(val * 100f + 0.5f), stationStore.localLogic, stationStore.remoteLogic, GameMain.mainPlayer);

                    // In client side, preserve displaying slider value until host response
                    Multiplayer.Session.StationsUI.StorageMaxChangeId = Multiplayer.Session.LocalPlayer.IsHost ? -1 : -2;
                }
            }

            // Request for remoteOrder update every 60tick
            if (Multiplayer.Session.LocalPlayer.IsClient && GameMain.gameTick - lastUpdateGametick > 60)
            {
                int gid = __instance.transport?.stationPool?[__instance.stationId].gid ?? 0;
                if (gid > 0)
                {
                    Multiplayer.Session.Network.SendPacket(new RemoteOrderUpdate(gid, System.Array.Empty<int>()));
                }
                lastUpdateGametick = GameMain.gameTick;
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(UIStationWindow.OnStationIdChange))]
        public static void OnStationIdChange_Postfix(UIStationWindow __instance)
        {
            if (!Multiplayer.IsActive || Multiplayer.Session.LocalPlayer.IsHost)
            {
                return;
            }

            __instance.warperIconButton.button.interactable = true;
            __instance.shipIconButton.button.interactable = true;
            __instance.droneIconButton.button.interactable = true;
        }
    }
}
