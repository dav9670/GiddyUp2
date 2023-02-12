using HarmonyLib;
using Verse;
using GiddyUp;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpCaravan
{
    [HarmonyPatch(typeof(AnimalPenUtility), nameof(AnimalPenUtility.NeedsToBeManagedByRope))]
    class Patch_NeedsToBeManagedByRope
    {
        static bool Prepare()
        {
            return Settings.caravansEnabled || Settings.rideAndRollEnabled;
        }
        static bool Postfix(bool __result, Pawn pawn)
        {
            if (__result) return !pawn.IsMountedAnimal();
            return __result;
        }
    }
}