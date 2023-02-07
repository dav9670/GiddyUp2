using GiddyUp;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUpRideAndRoll.Harmony
{
    [HarmonyPatch(typeof(CompAnimalPenMarker), nameof(CompAnimalPenMarker.AcceptsToPen))]
    class CompAnimalPenMarker_AcceptsToPen
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.rideAndRollEnabled;
        }
        static bool Postfix(bool __result, Pawn animal)
        {
            if (!__result)
            {
                return animal.IsMounted();
            }
            return __result;
        }
    }

}
