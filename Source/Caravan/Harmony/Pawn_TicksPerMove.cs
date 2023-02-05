using GiddyUp;
using HarmonyLib;
using Verse;

namespace GiddyUpCaravan.Harmony
{
    //TODO remove this?
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
            ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[__instance.thingIDNumber];
            if (pawnData.reservedMount != null && __instance.def.race.intelligence == Intelligence.Humanlike && !__instance.Spawned)
            {
                return TicksPerMoveUtility.AdjustedTicksPerMove(__instance.skills, pawnData.reservedMount, diagonal);
            }
            return __result;
        }
    }
}
