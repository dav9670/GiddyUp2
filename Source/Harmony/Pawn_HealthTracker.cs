using GiddyUp.Jobs;
using GiddyUp.Storage;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace GiddyUp.HarmonyPlaceholder
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
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
                ExtendedDataStorage dataStorage = Setup._extendedDataStorage;
                if(dataStorage != null)
                {
                    ExtendedPawnData pawnData = dataStorage.GetExtendedDataFor(pawn.thingIDNumber);
                    if (pawnData != null && pawnData.owning != null && !pawnData.owning.Dead && pawnData.owning.Spawned && pawnData.owning.RaceProps.Animal)
                    {
                        pawnData.owning.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee);
                    }
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
                ExtendedDataStorage dataStorage = Setup._extendedDataStorage;
                if (dataStorage != null)
                {
                    ExtendedPawnData pawnData = dataStorage.GetExtendedDataFor(pawn.thingIDNumber);
                    if (pawnData != null && pawnData.owning != null && !pawnData.owning.Dead && pawnData.owning.Spawned)
                    {
                        pawnData.owning.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee);
                    }
                }
            }
        }
    }

    

}
