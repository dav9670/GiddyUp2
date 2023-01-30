using GiddyUp.Storage;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
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

        protected override IEnumerable<Toil> MakeNewToils()
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

            Thing thing = pawn as Thing;
            Pawn rider = Rider;
            if (rider.Downed || rider.Dead || pawn.Downed || pawn.Dead || pawn.IsBurning() || rider.IsBurning() || rider.GetPosture() != PawnPosture.Standing)
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
                if (!rider.IsColonist && !rider.Dead)
                {
                    //Log.Message("rider not spawned, despawn");
                    pawn.ExitMap(false, CellRect.WholeMap(base.Map).GetClosestEdge(this.pawn.Position));
                    return true;
                }
                else if(rider.IsColonist && rider.GetCaravan() != null)
                {
                    //Log.Message("rider moved to map, despawn");
                    pawn.ExitMap(true, CellRect.WholeMap(base.Map).GetClosestEdge(this.pawn.Position));
                    return true;
                }
                else return true;
            }

            if (!rider.Drafted && rider.IsColonist) //TODO refactor this as a postfix in Giddy-up Caravan. 
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
            riderData = Setup._extendedDataStorage.GetExtendedDataFor(Rider.thingIDNumber);
            riderData.Reset();
            pawn.Drawer.tweener = new PawnTweener(pawn);
            if (!interrupted)
            {
                pawn.Position = Rider.Position;
            }

            pawn.pather.ResetToCurrentPosition();
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
