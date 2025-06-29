using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUp.Harmony;

[HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.MakeDowned))]
internal static class Patch_MakeDowned
{
    private static void Postfix(Pawn_HealthTracker __instance)
    {
        var pawn = __instance.pawn;
        if (pawn.Faction == null || ExtendedDataStorage.Singleton == null)
            return; //Null checking the singleton 'cause this could happen before the world sets up.

        var pawnData = pawn.GetExtendedPawnData();
        if (pawn.RaceProps.Humanlike)
            pawn.InvoluntaryDismount(pawnData.ReservedMount, pawnData);
        else if (pawnData.ReservedBy != null && pawn.HostileTo(Current.gameInt.worldInt.factionManager.ofPlayer))
            pawn.SetFaction(null); //If an enemy animal is downed, make it a wild animal so it can be rescued.
    }
}

[HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.SetDead))]
internal static class Patch_SetDead
{
    private static void Postfix(Pawn_HealthTracker __instance)
    {
        var pawn = __instance.pawn;
        if (pawn.Faction != null && pawn.RaceProps.Humanlike)
        {
            var pawnData = pawn.GetExtendedPawnData();
            pawn.InvoluntaryDismount(pawnData.ReservedMount, pawnData);
        }
    }
}