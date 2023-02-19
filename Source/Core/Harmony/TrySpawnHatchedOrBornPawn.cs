using HarmonyLib;
using RimWorld;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.TrySpawnHatchedOrBornPawn))]
    class Patch_TrySpawnHatchedOrBornPawn
    {
        static void Postfix(Pawn pawn, Thing motherOrEgg)
        {
            if (Settings.automountDisabledByDefault) pawn.GetGUData().automount = ExtendedPawnData.Automount.False;
            else if (motherOrEgg is Pawn mother)
            {
                var pawnData = pawn.GetGUData();
                var motherData = mother.GetGUData();
                pawnData.automount = motherData.automount;
            }
        }
    }
}