using GiddyUp;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.MakeDowned))]
    static class Patch_MakeDowned
    {
        static void Postfix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = __instance.pawn;
            if (pawn.Faction == null) return;

            //If an enemy animal is downed, make it a wild animal so it can be rescued. 
            if (pawn.RaceProps.Animal && pawn.HostileTo(Current.gameInt.worldInt.factionManager.ofPlayer))
            {
                pawn.SetFaction(null);
                return;
            }

            //If the owner of an NPC mount is downed, let the animal flee. Null checking the GUcomp 'cause this could happen before the world sets up.
            if (pawn.RaceProps.Humanlike && !pawn.Faction.def.isPlayer && ExtendedDataStorage.GUComp != null)
            {
                ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
                if (pawnData.reservedMount != null && !pawnData.reservedMount.Dead && pawnData.reservedMount.Spawned)
                {
                    pawnData.reservedMount.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee);
                }
            }
        }
    }
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.SetDead))]
    static class Patch_SetDead
    {
        static void Postfix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = __instance.pawn;
            if (pawn.Faction == null) return;
            //If the owner of an NPC mount is downed, let the animal flee
            if (pawn.RaceProps.Humanlike && !pawn.Faction.def.isPlayer)
            {
                ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
                if (pawnData.reservedMount != null && !pawnData.reservedMount.Dead && pawnData.reservedMount.Spawned)
                {
                    pawnData.reservedMount.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee);
                }
            }
        }
    }
}