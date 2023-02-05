using HarmonyLib;
using Verse;
using GiddyUp;

namespace GiddyUpCaravan
{
    [HarmonyPatch(typeof(AnimalPenUtility), nameof(AnimalPenUtility.NeedsToBeManagedByRope))]
    class Patch_NeedsToBeManagedByRope
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.caravansEnabled || GiddyUp.ModSettings_GiddyUp.rideAndRollEnabled;
        }
        static bool Postfix(bool __result, Pawn pawn)
        {
            if (__result) return ExtendedDataStorage.GUComp[pawn.thingIDNumber].reservedBy == null;
            return __result;
        }
    }
}
