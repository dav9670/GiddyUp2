using GiddyUp.Storage;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace GiddyUpCaravan.Harmony
{
    [HarmonyPatch(typeof(AnimalPenUtility), nameof(AnimalPenUtility.NeedsToBeManagedByRope))]
    class AnimalPenUtility_NeedsToBeManagedByRope
    {
        static bool Postfix(bool __result, Pawn pawn)
        {
            if (__result && pawn.IsFormingCaravan())
            {
                return GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber).caravanRider == null;
            }
            return __result;
        }
    }
}
