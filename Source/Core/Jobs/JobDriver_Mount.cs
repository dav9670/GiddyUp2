using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
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
        static SimpleCurve skillFactor = new SimpleCurve
		{
			{ new CurvePoint(0f, 1.4f) },
            { new CurvePoint(8f, 1f) },
			{ new CurvePoint(20f, 0.33f) }
		};
        static SimpleCurve wildnessFactor = new SimpleCurve
		{
			{ new CurvePoint(0.35f, 0.8f) },
			{ new CurvePoint(1f, 2f) },
		};
        static SimpleCurve bodySizeFactor = new SimpleCurve
		{
			{ new CurvePoint(2f, 200f) },
			{ new CurvePoint(4f, 300f) },
		};
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
            int mountComplexity = (int)(bodySizeFactor.Evaluate(mount.BodySize) * wildnessFactor.Evaluate(mount.RaceProps.wildness) * 
                skillFactor.Evaluate(pawn.skills.GetSkill(SkillDefOf.Animals).Level));
            if (Settings.logging) Log.Message("[Giddy-Up] Number of ticks for " + pawn.Name.ToString() + " to mount " + mount.def.defName + " : " + mountComplexity.ToString());
            return new Toil()
            {
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = mountComplexity,
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