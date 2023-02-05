using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp.Jobs
{
    public class JobDriver_Mount : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        public Pawn Mount { get { return job.targetA.Thing as Pawn; } }
        public override IEnumerable<Toil> MakeNewToils()
        {
            job.canBashDoors = true;
            job.canBashFences = true;
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDowned(TargetIndex.A);

            yield return LetMountParticipate();
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            if(this.pawn.interactions != null)
            {
                yield return Toils_Interpersonal.WaitToBeAbleToInteract(this.pawn);
            }
            yield return TalkToAnimal();
        }
        Toil LetMountParticipate()
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
        Toil TalkToAnimal()
        {
            Toil toil = new Toil();
            toil.AddFailCondition(delegate { return Mount.CurJob.def != ResourceBank.JobDefOf.Mounted; });
            toil.initAction = delegate
            {
                Pawn actor = toil.GetActor();
                if(actor.interactions != null)
                {
                    actor.interactions.TryInteractWith(Mount, InteractionDefOf.AnimalChat);
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = 150; //TODO tie into handling skill bonus
            toil.AddFinishAction(delegate
            {
                FinishAction();
            });
            return toil;
        }
        public void FinishAction()
        {
            var mount = Mount;
            bool flag = mount.CurJob != null && mount.CurJob.def == ResourceBank.JobDefOf.Mounted;
            if (Settings.rideAndRollEnabled || flag)
            {
                var pawnData = ExtendedDataStorage.GUComp[this.pawn.thingIDNumber];
                var animalData = ExtendedDataStorage.GUComp[mount.thingIDNumber];

                if (flag)
                {
                    pawnData.Mount = mount;
                    TextureUtility.SetDrawOffset(pawnData);
                }

                if (Settings.rideAndRollEnabled)
                {
                    pawnData.reservedMount = mount;
                    animalData.reservedBy = this.pawn;
                }
            }
        }
    }
}