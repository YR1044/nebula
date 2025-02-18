﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NebulaPatcher.Patches.Transpilers
{
    [HarmonyPatch(typeof(PlanetTransport))]
    public class PlanetTransport_Transpiler
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(PlanetTransport.RefreshDispenserOnStoragePrebuildBuild))]
        public static IEnumerable<CodeInstruction> RefreshDispenserOnStoragePrebuildBuild_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // factoryModel.gpuiManager is null for remote planets, so we need to use GameMain.gpuiManager which is initialized by nebula
                // replace : this.factory.planet.factoryModel.gpuiManager
                // with    : GameMain.gpuiManager
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld),
                        new CodeMatch(OpCodes.Callvirt),
                        new CodeMatch(OpCodes.Ldfld),
                        new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "gpuiManager")
                    )
                    .Repeat(matcher => matcher
                            .RemoveInstructions(4)
                            .SetAndAdvance(OpCodes.Call, typeof(GameMain).GetProperty("gpuiManager").GetGetMethod()
                    ));

                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                NebulaModel.Logger.Log.Error("RefreshDispenserOnStoragePrebuildBuild_Transpiler fail!");
                NebulaModel.Logger.Log.Error(e);
                return instructions;
            }
        }
    }
}
