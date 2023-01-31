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
            if(motherOrEgg is Pawn mother)
            {
                var pawnData = Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
                var motherData = Setup._extendedDataStorage.GetExtendedDataFor(mother.thingIDNumber);
                pawnData.mountableByAnyone = motherData.mountableByAnyone;
                pawnData.mountableByMaster = motherData.mountableByMaster;
            }
        }
    }
}
