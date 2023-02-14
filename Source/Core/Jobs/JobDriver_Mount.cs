using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;

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
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDowned(TargetIndex.A);

            yield return LetMountParticipate();
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            if (pawn.interactions != null) yield return Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
            yield return TalkToAnimal();
        }
        Toil LetMountParticipate()
        {
            return new Toil()
            {
                defaultCompleteMode = ToilCompleteMode.Never,
                initAction = delegate
                {
                    Mount.jobs.StopAll();
                    Mount.pather.StopDead();
                    Mount.jobs.TryTakeOrderedJob(new Job(ResourceBank.JobDefOf.Mounted, pawn) { count = 1});
                    ReadyForNextToil();
                }
            };
        }
        Toil TalkToAnimal()
        {
            var mount = Mount;
            return new Toil()
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 150,
                endConditions = new List<System.Func<JobCondition>>()
                {
                    delegate
                    {
                        if (mount?.CurJobDef != ResourceBank.JobDefOf.Mounted)
                        {
                            return JobCondition.Incompletable;
                        }
                        return JobCondition.Ongoing;
                    }
                },
                initAction = delegate
                {
                    Pawn actor = GetActor();
                    if (actor.interactions != null)
                    {
                        actor.interactions.TryInteractWith(mount, InteractionDefOf.AnimalChat);
                    }
                },
                finishActions = new List<System.Action>() { delegate
                {
                    if (!pawn.CanReserve(mount) || (mount.GetGUData().reservedBy != pawn && mount.IsFormingCaravan())) return;
                    pawn.GoMount(mount, MountUtility.GiveJobMethod.Instant);
                }}
            };
        }
    }
}