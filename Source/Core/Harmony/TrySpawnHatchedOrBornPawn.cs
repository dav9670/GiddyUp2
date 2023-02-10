using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.TrySpawnHatchedOrBornPawn))]
    class Patch_TrySpawnHatchedOrBornPawn
    {
        static void Postfix(Pawn pawn, Thing motherOrEgg)
        {
            if (motherOrEgg is Pawn mother)
            {
                var pawnData = pawn.GetGUData();
                var motherData = mother.GetGUData();
                pawnData.automount = motherData.automount;
            }
        }
    }
}