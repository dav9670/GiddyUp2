using GiddyUp;
using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace GiddyUpRideAndRoll.Harmony
{
    [HarmonyPatch(typeof(AnimalPenUtility), nameof(AnimalPenUtility.GetPenAnimalShouldBeTakenTo))]
    class Patch_GetPenAnimalShouldBeTakenTo
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.rideAndRollEnabled;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(AccessTools.Method(typeof(AnimalPenUtility), nameof(AnimalPenUtility.NeedsToBeManagedByRope)),
				AccessTools.Method(typeof(Patch_GetPenAnimalShouldBeTakenTo), nameof(Patch_GetPenAnimalShouldBeTakenTo.NeedsToBeManagedByRopeModified)));
        }
        static bool NeedsToBeManagedByRopeModified(Pawn animal)
        {
            if (IsMountableUtility.IsCurrentlyMounted(animal)) return true;
            else return AnimalPenUtility.NeedsToBeManagedByRope(animal);
        }
    }
}