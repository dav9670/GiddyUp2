using HarmonyLib;
using RimWorld;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp.Harmony;

[HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.TrySpawnHatchedOrBornPawn))]
internal class Patch_TrySpawnHatchedOrBornPawn
{
    private static void Postfix(Pawn pawn, Thing motherOrEgg)
    {
        if (Settings.automountDisabledByDefault)
        {
            pawn.GetExtendedPawnData().automount = ExtendedPawnData.Automount.False;
        }
        else if (motherOrEgg is Pawn mother)
        {
            var pawnData = pawn.GetExtendedPawnData();
            var motherData = mother.GetExtendedPawnData();
            pawnData.automount = motherData.automount;
        }
    }
}