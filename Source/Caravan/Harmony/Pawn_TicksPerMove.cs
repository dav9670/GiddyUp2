using GiddyUpCaravan.Utilities;
using GiddyUp.Storage;
using GiddyUp.Utilities;
using HarmonyLib;
using Verse;

namespace GiddyUpCaravan.Harmony
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TicksPerMove))]
    [HarmonyPriority(Priority.Low)]
    static class Pawn_TicksPerMove
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.caravansEnabled;
        }
        static int Postfix(int __result, Pawn __instance, ref bool diagonal)
        {
            ExtendedPawnData pawnData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(__instance.thingIDNumber);
            if (pawnData.caravanMount != null && __instance.RidingCaravanMount(pawnData) && !__instance.Spawned)
            {
                return TicksPerMoveUtility.AdjustedTicksPerMove(__instance.skills, pawnData.caravanMount, diagonal);
            }
            return __result;
        }
    }
}
