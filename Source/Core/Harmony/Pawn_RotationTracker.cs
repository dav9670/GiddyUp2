using GiddyUp.Jobs;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace GiddyUp.Harmony;

[HarmonyPatch(typeof(Thing), nameof(Thing.Rotation), MethodType.Setter)]
internal static class Patch_Rotation
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var code in instructions)
        {
            yield return code;
            if (code.opcode == OpCodes.Stfld)
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(Patch_Rotation), nameof(RotChanged)));
            }
        }
    }

    private static void RotChanged(Thing __instance)
    {
        if (__instance is not Pawn pawn)
            return;

        if (pawn.jobs != null && pawn.jobs.curDriver is JobDriver_Mounted jobDriver)
            __instance.rotationInt = jobDriver.rider.Rotation;
    }
}