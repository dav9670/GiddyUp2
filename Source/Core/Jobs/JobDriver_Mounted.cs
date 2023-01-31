﻿using GiddyUp.Storage;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using GiddyUpRideAndRoll;
using Verse;
using Verse.AI;

namespace GiddyUp.Jobs
{
    public class JobDriver_Mounted : JobDriver
    {
        public Pawn Rider { get { return job.targetA.Thing as Pawn; } }
        ExtendedPawnData riderData;
        public bool interrupted = false;
        bool isFinished = false;
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
        

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return WaitForRider();
            yield return DelegateMovement();
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        public bool ShouldCancelJob(ExtendedPawnData riderData)
        {
            if (interrupted) return true;
            if (riderData == null || riderData.mount == null) return true;

            Pawn rider = Rider;
            var riderIsDead = rider.Dead;
            if (rider.Downed || riderIsDead || pawn.Downed || pawn.Dead || pawn.IsBurning() || rider.IsBurning() || rider.GetPosture() != PawnPosture.Standing)
            {
                //Log.Message("cancel job, rider downed or dead");
                return true;
            }
            if (pawn.InMentalState || (rider.InMentalState && rider.MentalState.def != MentalStateDefOf.PanicFlee))
            {
                //Log.Message("cancel job, rider or mount in mental state");
                return true;
            }
            if (!rider.Spawned)
            {
                var riderIsColonist = rider.IsColonist;
                if (!riderIsColonist && !riderIsDead)
                {
                    //Log.Message("rider not spawned, despawn");
                    pawn.ExitMap(false, CellRect.WholeMap(base.Map).GetClosestEdge(this.pawn.Position));
                    return true;
                }
                else if(riderIsColonist && rider.GetCaravan() != null)
                {
                    //Log.Message("rider moved to map, despawn");
                    pawn.ExitMap(true, CellRect.WholeMap(base.Map).GetClosestEdge(this.pawn.Position));
                    return true;
                }
                else return true;
            }
            bool riderIsDrafted = rider.Drafted;
            if (!riderIsDrafted && rider.IsColonist) //TODO refactor this as a postfix in Giddy-up Caravan. 
            {
                if((rider.mindState != null && rider.mindState.duty != null && (rider.mindState.duty.def == DutyDefOf.TravelOrWait || rider.mindState.duty.def == DutyDefOf.TravelOrLeave || rider.mindState.duty.def == DutyDefOf.PrepareCaravan_GatherAnimals || rider.mindState.duty.def == DutyDefOf.PrepareCaravan_GatherDownedPawns)))
                {
                    if(riderData.caravanMount == pawn) return false;
                    return true;
                    //if forming caravan, stay mounted. 
                }
                else if(riderData.owning == pawn)
                {
                    //Log.Message("cancel job, rider not drafted while being colonist");
                    //Log.Message("riderData.owning: " + riderData.owning);
                    return false;
                }
                else return true;
            }
            if (riderData.mount == null) return true;
            
            if (ModSettings_GiddyUp.rideAndRollEnabled)
            {
                if (pawn.factionInt.def.isPlayer && !riderIsDrafted && rider.CurJob != null && !allowedJobs.Contains(rider.CurJob.def))
                {
                    var jobDef = rider.CurJob.def;

                    if(jobDef == JobDefOf.EnterTransporter) return true;

                    if(jobDef == JobDefOf.Hunt && GiddyUp.ModSettings_GiddyUp.noMountedHunting) return true;
                    else if (!rider.pather.Moving) return true;

                    if(!riderIsDrafted && pawn.HungryOrTired()) return true;
                }
            }
            return false;
        }
        Toil WaitForRider()
        {
            Toil toil = new Toil();

            Pawn rider = Rider;
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.tickAction = delegate
            {
                if (rider == null || rider.Dead || !rider.Spawned || rider.Downed || rider.InMentalState)
                {
                    interrupted = true;
                    ReadyForNextToil();
                    return;
                }

                riderData = Setup._extendedDataStorage.GetExtendedDataFor(rider.thingIDNumber);
                if (riderData.mount != null && riderData.mount == pawn)
                {
                    ReadyForNextToil();
                }

                var curJobDef = rider.CurJob.def;
                if (curJobDef != ResourceBank.JobDefOf.Mount && 
                    curJobDef != JobDefOf.Vomit && 
                    curJobDef != JobDefOf.Wait_MaintainPosture && 
                    curJobDef != JobDefOf.SocialRelax && 
                    curJobDef != JobDefOf.Wait && 
                    riderData.mount == null)
                {
                    //Log.Message("cancel wait for rider, rider is not mounting, curJob: " + Rider.CurJob.def.defName);                  
                    interrupted = true;
                    ReadyForNextToil();
                }

            };
            return toil;
        }
        public Toil DelegateMovement()
        {
            Toil toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            Pawn rider = Rider;

            toil.tickAction = delegate
            {
                if (isFinished) return;
                riderData = Setup._extendedDataStorage.GetExtendedDataFor(rider.thingIDNumber);
                if (ShouldCancelJob(riderData))
                {
                    ReadyForNextToil();
                    return;
                }
                pawn.Drawer.tweener = rider.Drawer.tweener;

                pawn.Position = rider.Position;
                TryAttackEnemy();
                pawn.Rotation = rider.Rotation;
            };

            toil.AddFinishAction(delegate
            {

                FinishAction();
            });
            
            return toil;

        }
        void FinishAction()
        {
            isFinished = true;
            var rider = Rider;
            riderData = Setup._extendedDataStorage.GetExtendedDataFor(rider.thingIDNumber);

            riderData.Reset();
            pawn.Drawer.tweener = new PawnTweener(pawn);
            if (!interrupted) pawn.Position = rider.Position;
            pawn.pather.ResetToCurrentPosition();

            if (ModSettings_GiddyUp.rideAndRollEnabled)
            {
                ExtendedPawnData pawnData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
                bool isRoped = pawn.roping != null && pawn.roping.IsRoped;
                if(!isRoped && !rider.Drafted && pawn.factionInt.def.isPlayer)
                {
                    if (pawnData.ownedBy != null && !interrupted && rider.GetCaravan() == null)
                    {
                        pawn.jobs.jobQueue.EnqueueFirst(new Job(GiddyUp.ResourceBank.JobDefOf.WaitForRider, pawnData.ownedBy)
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
        public void TryAttackEnemy()
        {
            Thing targetThing = null;
            Pawn rider = Rider;

            if (rider == null)
                return;
            
            if (rider.TargetCurrentlyAimingAt != null)
            {
                targetThing = rider.TargetCurrentlyAimingAt.Thing;
            }
            else if (rider.CurJob?.def == JobDefOf.AttackMelee && rider.CurJob.targetA.Thing.HostileTo(rider))
            {
                targetThing = rider.CurJob.targetA.Thing;
            }
            if (targetThing != null && targetThing.HostileTo(rider))
            {
                if (pawn.meleeVerbs == null || pawn.meleeVerbs.TryGetMeleeVerb(targetThing) == null || !pawn.meleeVerbs.TryGetMeleeVerb(targetThing).CanHitTarget(targetThing))
                {
                    pawn.TryStartAttack(targetThing); //Try start ranged attack if possible
                }
                else
                {
                    pawn.meleeVerbs.TryMeleeAttack(targetThing);
                }
            }
        }
    }
}