using GiddyUp.Jobs;
using GiddyUp.Storage;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace GiddyUpRideAndRoll.Harmony
{
    [HarmonyPatch(typeof(JobDriver_Mounted), nameof(JobDriver_Mounted.ShouldCancelJob))]
    class JobDriver_Mounted_ShouldCancelJob
    {
        //TODO: maybe xml this instead of hard coding. 
        static HashSet<JobDef> allowedJobs = new HashSet<JobDef>() {
            JobDefOf.Arrest, 
            JobDefOf.AttackMelee, 
            JobDefOf.AttackStatic, 
            JobDefOf.Capture, 
            JobDefOf.DropEquipment, 
            JobDefOf.EscortPrisonerToBed, 
            JobDefOf.ExtinguishSelf, 
            JobDefOf.Flee, 
            JobDefOf.FleeAndCower, 
            JobDefOf.Goto, 
            JobDefOf.GotoSafeTemperature, 
            JobDefOf.GotoWander, 
            JobDefOf.HaulToCell, 
            JobDefOf.HaulToContainer, 
            JobDefOf.Ignite, 
            JobDefOf.Insult, 
            JobDefOf.Kidnap, 
            JobDefOf.Open, 
            JobDefOf.RemoveApparel, 
            JobDefOf.Rescue, 
            JobDefOf.TakeWoundedPrisonerToBed, 
            JobDefOf.TradeWithPawn, 
            JobDefOf.UnloadInventory, 
            JobDefOf.UseArtifact, 
            JobDefOf.UseVerbOnThing, 
            JobDefOf.Vomit, 
            JobDefOf.Wait, 
            JobDefOf.Wait_Combat, 
            JobDefOf.Wait_MaintainPosture, 
            JobDefOf.Wait_SafeTemperature, 
            JobDefOf.Wait_Wander, 
            JobDefOf.Wear, 
            JobDefOf.TakeInventory, 
            JobDefOf.UnloadYourInventory, 
            JobDefOf.RopeToPen, 
            JobDefOf.ReturnedCaravan_PenAnimals, 
            JobDefOf.RopeRoamerToUnenclosedPen, 
            JobDefOf.Tame
            };
        static bool Postfix(bool __result, ExtendedPawnData riderData, JobDriver_Mounted __instance)
        {
            var rider = __instance.Rider;
            if(rider == null) return true;

            if (__instance.pawn.factionInt.def.isPlayer && !rider.Drafted && rider.CurJob != null && !allowedJobs.Contains(rider.CurJob.def))
            {
                if(rider.CurJob.def == JobDefOf.EnterTransporter)
                {
                    return true;
                }
                if(rider.CurJob.def == JobDefOf.Hunt && GiddyUp.ModSettings_GiddyUp.noMountedHunting)
                {
                    return true;
                }
                else if (!rider.pather.Moving)
                {
                    return true;
                }
                if(!rider.Drafted && __instance.pawn.HungryOrTired())
                {
                    return true;
                }
            }
            return __result;
        }
    }

    [HarmonyPatch(typeof(JobDriver_Mounted), "FinishAction")]
    class JobDriver_Mounted_FinishAction
    {
        static void Postfix(JobDriver_Mounted __instance)
        {
            ExtendedPawnData pawnData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(__instance.pawn.thingIDNumber);
            bool isRoped = __instance.pawn.roping != null && __instance.pawn.roping.IsRoped;
            if(!__instance.Rider.Drafted && __instance.pawn.Faction == Faction.OfPlayer && !isRoped)
            {
                if (pawnData.ownedBy != null && !__instance.interrupted && __instance.Rider.GetCaravan() == null)
                {
                    __instance.pawn.jobs.jobQueue.EnqueueFirst(new Job(GiddyUp.ResourceBank.JobDefOf.WaitForRider, pawnData.ownedBy)
                    {
                        expiryInterval = 10000,
                        checkOverrideOnExpire = true,
                        followRadius = 8,
                        locomotionUrgency = LocomotionUrgency.Walk
                    }
                    ); //follow the rider for a while to give it an opportunity to take a ride back.  
                }
            }
        }
    }

    
    

}
