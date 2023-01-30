using GiddyUp.Jobs;
using GiddyUp.Storage;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    static class Pawn_JobTracker_StartJob
    {    
       static bool Prefix(Pawn_JobTracker __instance)
       {
            if (__instance.curDriver != null && __instance.curDriver.pawn != null && __instance.curDriver.pawn.CurJob != null && __instance.curDriver.pawn.CurJob.def == ResourceBank.JobDefOf.Mounted)
            {
                return false;
            }
           return true;
       }
    }
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.DetermineNextJob))]
    static class Pawn_JobTracker_DetermineNextJob
    {
        static void Postfix(Pawn_JobTracker __instance, ref ThinkResult __result)
        {
            Pawn pawn = __instance.pawn;
            //Remove mount in case the mount somehow isn't mounted by pawn. 
            if (Setup.isMounted.Contains(pawn.thingIDNumber) && pawn.IsColonist)
            {
                ExtendedPawnData pawnData = Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
                if (pawnData.mount.CurJob == null || pawnData.mount.CurJobDef != ResourceBank.JobDefOf.Mounted)
                {
                    pawnData.Reset();
                }
                else if (pawnData.mount.jobs.curDriver is JobDriver_Mounted driver && driver.Rider != pawn)
                {
                    pawnData.Reset();
                }
            }
            //If a hostile pawn owns an animal, make sure it mounts it whenever possible
            if (pawn.def.race.intelligence == Intelligence.Humanlike && 
                pawn.factionInt != null && 
                pawn.factionInt.HostileTo(Current.gameInt.worldInt.factionManager.ofPlayer) && 
                !pawn.Downed && !pawn.IsPrisoner && !pawn.IsBurning()
                )
            {

                ExtendedPawnData pawnData = Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
                if (pawnData.owning == null || pawnData.owning.Faction != pawn.Faction || pawnData.mount != null || pawnData.owning.Downed || pawnData.owning.Dead || !pawnData.owning.Spawned || pawnData.owning.IsBurning())
                {
                    return;
                }
                QueuedJob qJob = pawn.jobs.jobQueue.FirstOrFallback(null);
                if (qJob != null && (qJob.job.def == ResourceBank.JobDefOf.Mount))
                {
                    return;
                }
                if (__result.Job.def == ResourceBank.JobDefOf.Mount)
                {
                    return;
                }

                Job mountJob = new Job(ResourceBank.JobDefOf.Mount, pawnData.owning);
                mountJob.count = 1;
                __instance.jobQueue.EnqueueFirst(mountJob);
            }

            GiddyUpRideAndRoll.Harmony.GiddyUpRideAndRoll_DetermineNextJob.Postfix(__instance, ref __result, pawn);
        }
    }
    [HarmonyPatch(typeof(Pawn_JobTracker), "Notify_MasterDraftedOrUndrafted")]
    static class Pawn_JobTracker_Notify_MasterDraftedOrUndrafted
    {
        static bool Prefix(Pawn_JobTracker __instance)
        {
            Pawn pawn = __instance.pawn;
            if (pawn != null && pawn.CurJob != null && pawn.CurJob.def == ResourceBank.JobDefOf.Mounted)
            {
                return false;
            }
            return true;

        }
        
    }
}
