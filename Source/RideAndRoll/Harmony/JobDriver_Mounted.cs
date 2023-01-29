using GiddyUp.Jobs;
using GiddyUp.Storage;
using GiddyUpRideAndRoll.Jobs;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace GiddyUpRideAndRoll.Harmony
{


    [HarmonyPatch(typeof(JobDriver_Mounted), "shouldCancelJob")]
    class JobDriver_Mounted_ShouldCancelJob
    {
        //TODO: maybe xml this instead of hard coding. 
        static JobDef[] allowedJobs = {GiddyUp.ResourceBank.JobDefOf.RideToJob, JobDefOf.Arrest, JobDefOf.AttackMelee, JobDefOf.AttackStatic, JobDefOf.Capture, JobDefOf.DropEquipment, JobDefOf.EscortPrisonerToBed, JobDefOf.ExtinguishSelf, JobDefOf.Flee, JobDefOf.FleeAndCower, JobDefOf.Goto, JobDefOf.GotoSafeTemperature, JobDefOf.GotoWander, JobDefOf.HaulToCell, JobDefOf.HaulToContainer, JobDefOf.Ignite, JobDefOf.Insult, JobDefOf.Kidnap, JobDefOf.Open, JobDefOf.RemoveApparel, JobDefOf.Rescue, JobDefOf.TakeWoundedPrisonerToBed, JobDefOf.TradeWithPawn, JobDefOf.UnloadInventory, JobDefOf.UseArtifact, JobDefOf.UseVerbOnThing, JobDefOf.Vomit, JobDefOf.Wait, JobDefOf.Wait_Combat, JobDefOf.Wait_MaintainPosture, JobDefOf.Wait_SafeTemperature, JobDefOf.Wait_Wander, JobDefOf.Wear, JobDefOf.TakeInventory, JobDefOf.UnloadYourInventory, JobDefOf.RopeToPen, JobDefOf.ReturnedCaravan_PenAnimals, JobDefOf.RopeRoamerToUnenclosedPen, JobDefOf.Tame};
        static void Postfix(ExtendedPawnData riderData, JobDriver_Mounted __instance, ref bool __result)
        {
            if(__instance.Rider == null)
            {
                __result = true;
                return;
            }
            if (__instance.pawn.Faction == Faction.OfPlayer && !__instance.Rider.Drafted && __instance.Rider.CurJob != null && !allowedJobs.Contains(__instance.Rider.CurJob.def))
            {
                if(__instance.Rider.CurJob.def == JobDefOf.EnterTransporter)
                {
                    __result = true;
                }
                if(!__instance.Rider.Drafted && __instance.pawn.HungryOrTired())
                {
                    __result = true;
                }
                if(__instance.Rider.CurJob.def == JobDefOf.Hunt && GiddyUp.ModSettings_GiddyUp.noMountedHunting)
                {
                    __result = true;
                }
                else if (!__instance.Rider.pather.Moving)
                {
                    __result = true;
                }
            }
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
