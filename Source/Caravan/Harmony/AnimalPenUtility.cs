using HarmonyLib;
using RimWorld.Planet;
using Verse;
using GiddyUp.Storage;

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
            if (__result && pawn.IsFormingCaravan())
            {
                return ExtendedDataStorage.GUComp[pawn.thingIDNumber].caravanRider == null;
            }
            return __result;
        }
    }
}
