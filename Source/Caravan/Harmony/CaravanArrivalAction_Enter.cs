using GiddyUp.Storage;
using HarmonyLib;
//using Multiplayer.API;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

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
                    var store = GiddyUp.Setup._extendedDataStorage;
                    ExtendedPawnData pawnData = store.GetExtendedDataFor(pawn.thingIDNumber);
                    if (pawnData.caravanMount is Pawn animal)
                    {
                        ExtendedPawnData animalData = store.GetExtendedDataFor(animal.thingIDNumber);
                        pawnData.mount = animal;
                        Job jobAnimal = new Job(GiddyUp.ResourceBank.JobDefOf.Mounted, pawn);
                        jobAnimal.count = 1;
                        animal.jobs.TryTakeOrderedJob(jobAnimal);
                    }
                }
            }
        }
    }
}