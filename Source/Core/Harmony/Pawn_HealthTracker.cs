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
        if (pawn.Faction == null || ExtendedDataStorage.GUComp == null)
            return; //Null checking the GUcomp 'cause this could happen before the world sets up.

        var pawnData = pawn.GetGUData();
        if (pawn.RaceProps.Humanlike)
            pawn.InvoluntaryDismount(pawnData.reservedMount, pawnData);
        else if (pawnData.reservedBy != null && pawn.HostileTo(Current.gameInt.worldInt.factionManager.ofPlayer))
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
            var pawnData = pawn.GetGUData();
            pawn.InvoluntaryDismount(pawnData.reservedMount, pawnData);
        }
    }
}