using GiddyUp;
using HarmonyLib;
using RimWorld;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpCore.RideAndRoll.Harmony;

[HarmonyPatch(typeof(CompAnimalPenMarker), nameof(CompAnimalPenMarker.AcceptsToPen))]
internal class CompAnimalPenMarker_AcceptsToPen
{
    private static bool Prepare()
    {
        return Settings.rideAndRollEnabled;
    }

    private static bool Postfix(bool __result, Pawn animal)
    {
        if (!__result)
            return animal.IsMountedAnimal();
        return __result;
    }
}