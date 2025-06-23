using GiddyUp;
using HarmonyLib;
//using Multiplayer.API;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpCaravan.Harmony;

[HarmonyPatch(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter),
    new Type[]
    {
        typeof(Caravan), typeof(Map), typeof(Func<Pawn, IntVec3>), typeof(CaravanDropInventoryMode), typeof(bool)
    })]
internal class Patch_CaravanEnterMapUtility
{
    private static bool Prepare()
    {
        return Settings.caravansEnabled;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var done = false;
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (!done && instruction.opcode == OpCodes.Call &&
                instruction.OperandIs(AccessTools.Method(typeof(Caravan), nameof(Caravan.RemoveAllPawns))))
            {
                yield return new CodeInstruction(OpCodes.Ldsfld,
                    AccessTools.Field(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.tmpPawns)));
                yield return new CodeInstruction(OpCodes.Call,
                    typeof(Patch_CaravanEnterMapUtility).GetMethod(nameof(MountCaravanMounts)));
                done = true;
            }
        }
    }

    //[SyncMethod]
    public static void MountCaravanMounts(List<Pawn> pawns)
    {
        foreach (var pawn in pawns)
            if (pawn.IsColonist && pawn.Spawned)
                pawn.GoMount(null, MountUtility.GiveJobMethod.Instant);
    }
}