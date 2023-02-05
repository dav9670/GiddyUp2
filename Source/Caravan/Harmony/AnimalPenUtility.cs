using HarmonyLib;
using Verse;
using GiddyUp;

namespace GiddyUpCaravan.Harmony
{
    [HarmonyPatch(typeof(AnimalPenUtility), nameof(AnimalPenUtility.NeedsToBeManagedByRope))]
    class AnimalPenUtility_NeedsToBeManagedByRope
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.caravansEnabled;
        }
        static bool Postfix(bool __result, Pawn pawn)
        {
            if (__result) return ExtendedDataStorage.GUComp[pawn.thingIDNumber].reservedBy == null;
            return __result;
        }
    }
}
