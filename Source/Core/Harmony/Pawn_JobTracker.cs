using GiddyUp.Jobs;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using System.Collections.Generic;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp.Harmony;

//This patch prevents animals from starting new jobs if they're currently mounted
[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
internal static class Patch_StartJob
{
    private static bool Prefix(Pawn_JobTracker __instance)
    {
        return !__instance.pawn.IsMountedAnimal();
    }
}

[HarmonyPatch(typeof(TransitionAction_EndAllJobs), nameof(TransitionAction_EndAllJobs.DoAction))]
internal static class Patch_DoAction
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return instructions.MethodReplacer(
            AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob)),
            AccessTools.Method(typeof(Patch_DoAction), nameof(EndCurrentJob)));
    }

    public static void EndCurrentJob(this Pawn_JobTracker job, JobCondition condition, bool startNewJob = true,
        bool canReturnToPool = true)
    {
        if (job.pawn.IsMountedAnimal())
            return;
        job.EndCurrentJob(condition, startNewJob, canReturnToPool);
    }
}

//TODO: There should be a centralized way to block these special job enders. For now they're individually handled, like this one
[HarmonyPatch(typeof(LordToil_KidnapCover), nameof(LordToil_KidnapCover.TryFindGoodOpportunisticTaskTarget))]
internal static class Patch_LordToil_KidnapCover_TryFindGoodOpportunisticTaskTarget
{
    private static bool Postfix(bool __result, Pawn pawn)
    {
        return !__result || pawn.IsMountedAnimal() ? false : __result;
    }
}

//Postfix, after a job has been determined, inject a job before it to go mount/dismount based on conditions
[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.DetermineNextJob))]
internal static class Patch_DetermineNextJob
{
    private static void Postfix(Pawn_JobTracker __instance, ref ThinkResult __result)
    {
        var pawn = __instance.pawn;
        if (pawn.Faction == null)
            return;
        if (pawn.def.race.intelligence == Intelligence.Humanlike)
        {
            //Sanity check, make sure the mount driver is still valid
            if (pawn.IsMounted() && pawn.IsColonist)
            {
                var pawnData = pawn.GetGUData();
                if (pawnData.mount.CurJobDef != ResourceBank.JobDefOf.Mounted ||
                    (pawnData.mount.jobs.curDriver is JobDriver_Mounted driver && driver.rider != pawn))
                    pawn.Dismount(null, pawnData, true);
            }
            //If a hostile pawn owns an animal, make sure it mounts it whenever possible
            else if (pawn.Faction.HostileTo(Current.gameInt.worldInt.factionManager.ofPlayer) &&
                     !pawn.Downed && !pawn.IsPrisoner && !pawn.HasAttachment(ThingDefOf.Fire))
            {
                var pawnData = pawn.GetGUData();
                var hostileMount = pawnData.reservedMount;
                if (hostileMount == null || !hostileMount.IsMountable(out var reason, pawn, true, true))
                    return;
                var qJob = pawn.jobs.jobQueue.FirstOrFallback(null);
                if (qJob?.job.def == ResourceBank.JobDefOf.Mount ||
                    __result.Job?.def == ResourceBank.JobDefOf.Mount)
                    return;

                pawn.GoMount(hostileMount);
            }
            else if (Settings.rideAndRollEnabled && pawn.Faction.def.isPlayer)
            {
                pawn.TryAutoMount(__instance, ref __result);
            }
        }

        if (Settings.caravansEnabled && !pawn.Faction.def.isPlayer)
        {
            //Handle failsafe for roped animals belonging to invalid pawns
            if (pawn.IsRoped())
            {
                var owner = pawn.GetGUData().reservedBy;
                if (owner == null || owner.Dead || !owner.Spawned)
                    pawn.roping.BreakAllRopes();
            }

            HandleVisitorsMounting(__instance, ref __result, pawn);
        }

        //This is responsible for friendly guests mounting/dismounting their animals they rode in on
        void HandleVisitorsMounting(Pawn_JobTracker jobTracker, ref ThinkResult thinkResult, Pawn pawn)
        {
            var lord = pawn.GetLord();
            if (lord == null)
                return;

            if (pawn.RaceProps.Animal && thinkResult.SourceNode is JobGiver_Wander jobGiver_Wander &&
                lord.CurLordToil is LordToil_DefendPoint)
            {
                var trader = TraderCaravanUtility.FindTrader(lord);
                //Unroped guest animals too far from their owners, go return
                if (trader != null && pawn.mindState.duty.focus.Cell.DistanceTo(trader.Position) > 150f &&
                    !pawn.IsRoped())
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.Follow, trader, 5f);
                else
                    jobGiver_Wander.wanderRadius = 5f;
            }

            //Filter out anything that is not a guest rider
            if (pawn.def.race.intelligence != Intelligence.Humanlike ||
                pawn.Faction.HostileTo(Current.gameInt.worldInt.factionManager.ofPlayer) || pawn.IsPrisoner ||
                thinkResult.Job == null)
                return;

            var job = thinkResult.Job;
            if (job == null || !job.GetFirstTarget(TargetIndex.A).IsValid)
                return;

            if (job.def == ResourceBank.JobDefOf.Dismount || job.def == ResourceBank.JobDefOf.Mount)
                return;

            if (pawn.jobs.jobQueue.FirstOrFallback(null) is QueuedJob queuedJob)
            {
                var qJob = queuedJob.job;
                if (qJob != null && (qJob.def == ResourceBank.JobDefOf.Dismount ||
                                     qJob.def == ResourceBank.JobDefOf.Mount))
                    return;
            }

            var pawnData = pawn.GetGUData();
            var curLordToil = lord.CurLordToil;

            //Caravan is headin' out, go mount up, boys
            if (curLordToil is LordToil_ExitMapAndEscortCarriers || curLordToil is LordToil_Travel ||
                curLordToil is LordToil_ExitMap || curLordToil is LordToil_ExitMapTraderFighting)
            {
                var animal = pawnData.reservedMount;
                if (animal != null && pawnData.mount == null)
                {
                    if (animal.IsMountable(out var reason, pawn, true, true))
                        thinkResult = pawn.GoMount(animal, MountUtility.GiveJobMethod.Inject, thinkResult).Value;
                    else if (Settings.logging)
                        Log.Message("[Giddy-Up] " + (pawn.Label ?? "NULL") + " could not mount: " + reason.ToString());
                }
                else if (Settings.logging)
                {
                    Log.Message("[Giddy-Up] " + (pawn.Label ?? "NULL") + " has no mount");
                }
            }

            //Caravan just arrived dismount
            else if (pawnData.mount != null &&
                     curLordToil is LordToil_DefendPoint) //first option is internal class, hence this way of accessing. 
            {
                //Dismount on-the-spot if it's a pack animal, the guards want to keep it nearby
                if (pawnData.mount.inventory != null && pawnData.mount.inventory.innerContainer.Count > 0)
                    pawn.Dismount(pawnData.mount, pawnData);
                //Other animals go to the assigned dismount spot.
                else
                    pawn.GoDismount(pawnData.mount);
            }
        }
    }
}

//A mount may have a master, be it the current rider or not. If the rider drafts, the animal will want to go over to them. This patch blocks that, if mounted.
[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.Notify_MasterDraftedOrUndrafted))]
internal static class Pawn_JobTracker_Notify_MasterDraftedOrUndrafted
{
    private static bool Prefix(Pawn_JobTracker __instance)
    {
        var pawn = __instance.pawn;
        return pawn == null || pawn.CurJobDef != ResourceBank.JobDefOf.Mounted;
    }
}