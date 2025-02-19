﻿using HarmonyLib;
using NebulaModel.Packets.Logistics;
using NebulaWorld;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NebulaPatcher.Patches.Dynamic
{
    [HarmonyPatch(typeof(UIDispenserWindow))]
    internal class UIDispenserWindow_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(UIDispenserWindow.OnCourierIconClick))]
        public static void OnCourierIconClick_Postfix(UIDispenserWindow __instance)
        {
            if (Multiplayer.IsActive)
            {
                DispenserComponent dispenserComponent = __instance.transport.dispenserPool[__instance.dispenserId];
                Multiplayer.Session.Network.SendPacketToLocalStar(
                    new DispenserSettingPacket(__instance.factory.planetId,
                                               __instance.dispenserId,
                                               EDispenserSettingEvent.SetCourierCount,
                                               dispenserComponent.workCourierCount + dispenserComponent.idleCourierCount));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(UIDispenserWindow.OnCourierAutoReplenishButtonClick))]
        public static void OnCourierAutoReplenishButtonClick_Postfix(UIDispenserWindow __instance)
        {
            if (Multiplayer.IsActive)
            {
                DispenserComponent dispenserComponent = __instance.transport.dispenserPool[__instance.dispenserId];
                Multiplayer.Session.Network.SendPacketToLocalStar(
                    new DispenserSettingPacket(__instance.factory.planetId,
                                               __instance.dispenserId,
                                               EDispenserSettingEvent.ToggleAutoReplenish,
                                               dispenserComponent.courierAutoReplenish ? 1 : 0));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(UIDispenserWindow.OnHoldupItemClick))]
        public static void OnHoldupItemClick_Postfix(UIDispenserWindow __instance)
        {
            if (Multiplayer.IsActive)
            {
                DispenserComponent dispenserComponent = __instance.transport.dispenserPool[__instance.dispenserId];
                if (__instance.player.inhandItemId == 0 && __instance.player.inhandItemCount == 0)
                {
                    Multiplayer.Session.Network.SendPacketToLocalStar(
                        new DispenserStorePacket(__instance.factory.planetId,
                                                 in dispenserComponent));
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(UIDispenserWindow.OnMaxChargePowerSliderValueChange))]
        public static void OnMaxChargePowerSliderValueChange_Postfix(UIDispenserWindow __instance, float value)
        {
            if (Multiplayer.IsActive && !Multiplayer.Session.StationsUI.IsIncomingRequest.Value)
            {
                Multiplayer.Session.Network.SendPacketToLocalStar(
                    new DispenserSettingPacket(__instance.factory.planetId,
                                               __instance.dispenserId,
                                               EDispenserSettingEvent.SetMaxChargePower,
                                               BitConverter.ToInt32(BitConverter.GetBytes(value), 0)));
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIDispenserWindow.OnItemIconMouseDown))]
        public static bool OnItemIconMouseDown_Prefix(UIDispenserWindow __instance, BaseEventData evt)
        {
            if (!Multiplayer.IsActive || __instance.dispenserId == 0 || __instance.factory == null)
            {
                return true;
            }
            DispenserComponent dispenserComponent = __instance.transport.dispenserPool[__instance.dispenserId];
            if (dispenserComponent == null || dispenserComponent.id != __instance.dispenserId)
            {
                return false;
            }
            PointerEventData pointerEventData = evt as PointerEventData;
            if (pointerEventData == null)
            {
                return false;
            }

            if (__instance.player.inhandItemId == 0)
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                {
                    __instance.CalculateStorageTotalCount(dispenserComponent, out int count, out int _);
                    if (count > 0)
                    {
                        UIRoot.instance.uiGame.OpenGridSplit(dispenserComponent.filter, count, Input.mousePosition);
                        __instance.insplit = true;
                        return false;
                    }
                }
            }
            else if (__instance.player.inhandItemId == dispenserComponent.filter && dispenserComponent.filter > 0 && pointerEventData.button == PointerEventData.InputButton.Left)
            {
                int entityId = dispenserComponent.storage.bottomStorage.entityId;
                int itemCount = __instance.factory.InsertIntoStorage(entityId, __instance.player.inhandItemId, __instance.player.inhandItemCount, __instance.player.inhandItemInc, out int handItemInc_Unsafe, false);

                // Player put itemCount into storage, broadcast the change to all users
                Multiplayer.Session.Network.SendPacketToLocalStar(
                    new DispenserAddTakePacket(__instance.factory.planetId,
                                               entityId,
                                               EDispenserAddTakeEvent.ManualAdd,
                                               __instance.player.inhandItemId, itemCount, __instance.player.inhandItemInc));

                __instance.player.AddHandItemCount_Unsafe(-itemCount);
                __instance.player.SetHandItemInc_Unsafe(handItemInc_Unsafe);
                if (__instance.player.inhandItemCount <= 0)
                {
                    __instance.player.SetHandItems(0, 0, 0);
                }
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIDispenserWindow.OnItemIconMouseUp))]
        public static bool OnItemIconMouseUp_Prefix(UIDispenserWindow __instance)
        {
            if (!Multiplayer.IsActive || __instance.dispenserId == 0 || __instance.factory == null)
            {
                return true;
            }
            DispenserComponent dispenserComponent = __instance.transport.dispenserPool[__instance.dispenserId];
            if (dispenserComponent == null || dispenserComponent.id != __instance.dispenserId)
            {
                return false;
            }

            if (__instance.insplit)
            {
                int count = UIRoot.instance.uiGame.CloseGridSplit();
                if (__instance.player.inhandItemId == 0 && __instance.player.inhandItemCount == 0 && dispenserComponent.filter > 0)
                {
                    int entityId = dispenserComponent.storage.bottomStorage.entityId;
                    int handItemCount_Unsafe = __instance.factory.PickFromStorage(entityId, dispenserComponent.filter, count, out int handItemInc_Unsafe);
                    __instance.player.SetHandItemId_Unsafe(dispenserComponent.filter);
                    __instance.player.SetHandItemCount_Unsafe(handItemCount_Unsafe);
                    __instance.player.SetHandItemInc_Unsafe(handItemInc_Unsafe);

                    // Player grab itemCount from storage, broadcast the change to all users
                    Multiplayer.Session.Network.SendPacketToLocalStar(
                        new DispenserAddTakePacket(__instance.factory.planetId,
                                                   entityId,
                                                   EDispenserAddTakeEvent.ManualTake,
                                                   __instance.player.inhandItemId, __instance.player.inhandItemCount, 0));
                }
                __instance.insplit = false;
            }

            return false;
        }
    }
}
