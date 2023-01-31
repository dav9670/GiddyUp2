using System.Collections.Generic;
using Verse;
using Verse.AI;
using GiddyUp.Utilities;
using GiddyUp.Storage;
using RimWorld;
using UnityEngine;

namespace GiddyUp.Jobs
{
    public class JobDriver_Mount : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (ModSettings_GiddyUp.rideAndRollEnabled && GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(this.pawn.thingIDNumber).targetJob == null)
            {
                return true;
            }
            return true;
        }
        public Pawn Mount { get { return job.targetA.Thing as Pawn; } }
        public override IEnumerable<Toil> MakeNewToils()
        {
            job.canBashDoors = true;
            job.canBashFences = true;
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDowned(TargetIndex.A);

            yield return letMountParticipate();
            //yield return Toils_General.Wait(1);//wait one tick to ensure animal is waiting to get mounted before proceding. 
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            if(this.pawn.interactions != null)
            {
                yield return Toils_Interpersonal.WaitToBeAbleToInteract(this.pawn);
            }
            yield return TalkToAnimal(TargetIndex.A);
        }
        Toil letMountParticipate()
        {
            Toil toil = new Toil();

            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.initAction = delegate
            {
                Mount.jobs.StopAll();
                Mount.pather.StopDead();
                Job jobAnimal = new Job(ResourceBank.JobDefOf.Mounted, pawn);
                jobAnimal.count = 1;
                Mount.jobs.TryTakeOrderedJob(jobAnimal);
                ReadyForNextToil();
            };
            return toil;
        }
        Toil TalkToAnimal(TargetIndex tameeInd)
        {
            Toil toil = new Toil();
            toil.AddFailCondition(delegate { return Mount.CurJob.def != ResourceBank.JobDefOf.Mounted; });
            //toil.AddFailCondition(delegate { return Mount.CurJob.targetA.Thing != pawn; });
            toil.initAction = delegate
            {
                Pawn actor = toil.GetActor();
                if(actor.interactions != null)
                {
                    actor.interactions.TryInteractWith(Mount, InteractionDefOf.AnimalChat);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = 150;
            toil.AddFinishAction(delegate
            {
                FinishAction();
            });
            return toil;
        }
        public void FinishAction()
        {
            bool flag = Mount.CurJob != null && Mount.CurJob.def == ResourceBank.JobDefOf.Mounted;
            if (ModSettings_GiddyUp.rideAndRollEnabled || flag)
            {
                var mount = Mount;
                var pawnData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(this.pawn.thingIDNumber);
                var animalData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(Mount.thingIDNumber);

                if (flag)
                {
                    pawnData.Mount = mount;
                    TextureUtility.SetDrawOffset(pawnData);
                }

                if (ModSettings_GiddyUp.rideAndRollEnabled)
                {
                    pawnData.owning = mount;
                    animalData.ownedBy = this.pawn;
                }
            }
        }
    }
}
