﻿using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp.Jobs;

public class JobDriver_Mount : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    public Pawn Mount => job.targetA.Thing as Pawn;

    private static readonly SimpleCurve SkillFactor = new()
    {
        { new CurvePoint(0f, 1.4f) },
        { new CurvePoint(8f, 1f) },
        { new CurvePoint(20f, 0.33f) }
    };

    private static readonly SimpleCurve WildnessFactor = new()
    {
        { new CurvePoint(0.35f, 0.8f) },
        { new CurvePoint(1f, 2f) }
    };

    private static readonly SimpleCurve BodySizeFactor = new()
    {
        { new CurvePoint(2f, 200f) },
        { new CurvePoint(4f, 300f) }
    };

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnDowned(TargetIndex.A);

        yield return LetMountParticipate();
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        if (pawn.interactions != null)
            yield return Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
        yield return TalkToAnimal();
    }

    private Toil LetMountParticipate()
    {
        return new Toil
        {
            defaultCompleteMode = ToilCompleteMode.Never,
            initAction = delegate
            {
                Mount.jobs.StopAll();
                Mount.pather.StopDead();
                Mount.jobs.TryTakeOrderedJob(new Job(ResourceBank.JobDefOf.Mounted, pawn) { count = 1 });
                ReadyForNextToil();
            }
        };
    }

    private Toil TalkToAnimal()
    {
        var mount = Mount;
        var mountComplexity = (int)(BodySizeFactor.Evaluate(mount.BodySize) *
                                    WildnessFactor.Evaluate(
                                        mount.kindDef.race.GetStatValueAbstract(StatDefOf.Wildness)) *
                                    SkillFactor.Evaluate(pawn.skills?.GetSkill(SkillDefOf.Animals).Level ?? 0));
        if (Settings.logging)
            Log.Message("[Giddy-Up] Number of ticks for " + pawn.Label + " to mount " + mount.def.defName + " : " +
                        mountComplexity.ToString());
        return new Toil
        {
            defaultCompleteMode = ToilCompleteMode.Delay,
            defaultDuration = mountComplexity,
            endConditions = new List<Func<JobCondition>>
            {
                delegate
                {
                    return mount?.CurJobDef != ResourceBank.JobDefOf.Mounted ? JobCondition.Incompletable : JobCondition.Ongoing;
                }
            },
            initAction = delegate
            {
                var actor = GetActor();
                actor.interactions?.TryInteractWith(mount, InteractionDefOf.AnimalChat);
            },
            finishActions = new List<Action>
            {
                delegate
                {
                    //Final checks
                    if (!pawn.CanReserve(mount) || //Can reserve?
                        (mount.GetExtendedPawnData().ReservedBy != pawn &&
                         mount.IsFormingCaravan()) || //Involved in someone else's caravan?
                        mount.Faction != pawn.Faction) //Switched factions mid-step? (quests)
                        return;

                    pawn.GoMount(mount, MountUtility.GiveJobMethod.Instant);
                }
            }
        };
    }
}