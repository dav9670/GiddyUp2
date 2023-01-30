using GiddyUp.Utilities;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUpRideAndRoll.Harmony
{
    [HarmonyPatch(typeof(CompAnimalPenMarker), nameof(CompAnimalPenMarker.AcceptsToPen))]
    class CompAnimalPenMarker_AcceptsToPen
    {
        static bool Postfix(bool __result, Pawn animal)
        {
            if (!__result)
            {
                return IsMountableUtility.IsCurrentlyMounted(animal);
            }
            return __result;
        }
    }

}
