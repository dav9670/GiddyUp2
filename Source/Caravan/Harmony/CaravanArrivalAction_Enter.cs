using GiddyUp;
using HarmonyLib;
//using Multiplayer.API;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using Verse.AI;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpCaravan.Harmony
{
    [HarmonyPatch(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.Enter), 
        new Type[] { typeof(Caravan), typeof(Map), typeof(Func<Pawn, IntVec3>), typeof(CaravanDropInventoryMode), typeof(bool) })]
    class CaravanEnterMapUtility_Enter
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.caravansEnabled;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool done = false;
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (!done && instruction.OperandIs(AccessTools.Method(typeof(Caravan), nameof(Caravan.RemoveAllPawns) ) ) )
                {
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(CaravanEnterMapUtility), nameof(CaravanEnterMapUtility.tmpPawns)));
                    yield return new CodeInstruction(OpCodes.Call, typeof(CaravanEnterMapUtility_Enter).GetMethod(nameof(CaravanEnterMapUtility_Enter.MountCaravanMounts)));
                    done = true;
                }
            }

        }
        //[SyncMethod]
        public static void MountCaravanMounts(List<Pawn> pawns)
        {
            foreach (Pawn pawn in pawns)
            {
                if (pawn.IsColonist && pawn.Spawned)
                {
                    MountUtility.GiveMountJob(pawn, null, MountUtility.GiveJobMethod.Instant);
                }
            }
        }
    }
}