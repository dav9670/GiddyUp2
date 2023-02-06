using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.TrySpawnHatchedOrBornPawn))]
    class PawnUtility_TrySpawnHatchedOrBornPawn
    {
        static void Postfix(Pawn pawn, Thing motherOrEgg)
        {
            if (motherOrEgg is Pawn mother)
            {
                var pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
                var motherData = ExtendedDataStorage.GUComp[mother.thingIDNumber];
                pawnData.automount = motherData.automount;
            }
        }
    }
}
