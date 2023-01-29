using GiddyUp.Utilities;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUpRideAndRoll.Harmony
{
    [HarmonyPatch(typeof(CompAnimalPenMarker), "AcceptsToPen")]
    class CompAnimalPenMarker_AcceptsToPen
    {
        static void Postfix(Pawn animal, ref bool __result)
        {
            if (!__result)
            {
                __result = IsMountableUtility.IsCurrentlyMounted(animal);
            }
        }
    }

}
