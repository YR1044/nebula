﻿using HarmonyLib;
using NebulaModel.Packets.GameHistory;
using NebulaWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NebulaPatcher.Patches.Transpilers
{
    [HarmonyPatch(typeof(MechaLab))]
    public class MechaLab_Transpiler
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(MechaLab.GameTick))]
        public static IEnumerable<CodeInstruction> GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Target: make this.gameHistory.AddTechHash((long)num4) only run in host

            CodeMatcher matcher = new CodeMatcher(instructions)
                .MatchForward(true,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldloc_S),
                    new CodeMatch(OpCodes.Conv_I8),
                    new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "AddTechHash")
                );

            if (matcher.IsInvalid)
            {
                NebulaModel.Logger.Log.Error("MechaLab.GameTick_Transpiler failed. Mod version not compatible with game version.");
                return instructions;
            }

            return matcher
                .SetInstruction(
                    HarmonyLib.Transpilers.EmitDelegate<Action<GameHistoryData, long>>((history, addcnt) =>
                    {
                        //Host in multiplayer can do normal research in the mecha
                        if (!Multiplayer.IsActive || Multiplayer.Session.LocalPlayer.IsHost)
                        {
                            history.AddTechHash(addcnt);
                            return;
                        }
                        
                        //Clients just sends contributing packet to the server
                        Multiplayer.Session.Network.SendPacket(new GameHistoryResearchContributionPacket(addcnt, history.currentTech));
                        return;
                    })
                )
                .InstructionEnumeration();
        }
    }
}
