using GiddyUp;
using HarmonyLib;
using RimWorld;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpRideAndRoll.Harmony
{
    [HarmonyPatch(typeof(CompAnimalPenMarker), nameof(CompAnimalPenMarker.AcceptsToPen))]
    class CompAnimalPenMarker_AcceptsToPen
    {
        static bool Prepare()
        {
            return Settings.rideAndRollEnabled;
        }
        static bool Postfix(bool __result, Pawn animal)
        {
            if (!__result)
            {
                return animal.IsMountedAnimal();
            }
            return __result;
        }
    }

}
