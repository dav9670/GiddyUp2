using GiddyUp;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.MakeDowned))]
    static class Pawn_HealthTracker_MakeDowned
    {
        static void Postfix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = __instance.pawn;

            //If an enemy animal is downed, make it a wild animal so it can be rescued. 
            if (pawn.RaceProps.Animal && pawn.Faction != null && !pawn.Faction.IsPlayer)
            {
                pawn.SetFaction(null);
            }

            //If the owner of an NPC mount is downed, let the animal flee
            if (pawn.RaceProps.Humanlike && pawn.Faction != null && !pawn.Faction.IsPlayer)
            {
                ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
                if (pawnData != null && pawnData.owning != null && !pawnData.owning.Dead && pawnData.owning.Spawned && pawnData.owning.RaceProps.Animal)
                {
                    pawnData.owning.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee);
                }
            }
        }

    }
    [HarmonyPatch(typeof(Pawn_HealthTracker), "SetDead")]
    static class Pawn_HealthTracker_SetDead
    {
        static void Postfix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = __instance.pawn;
            //If the owner of an NPC mount is downed, let the animal flee
            if (pawn.RaceProps.Humanlike && pawn.Faction != null && !pawn.Faction.IsPlayer)
            {
                ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
                if (pawnData != null && pawnData.owning != null && !pawnData.owning.Dead && pawnData.owning.Spawned)
                {
                    pawnData.owning.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee);
                }
            }
        }
    }

    

}
