using GiddyUp.Jobs;
using GiddyUp.Storage;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using GiddyUp.Utilities;
using GiddyUpRideAndRoll;
using System.Linq;
using Verse.AI.Group;
using GiddyUp.Zones;

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
                if (pawnData.owning == null || pawnData.owning.Faction != pawn.Faction || 
                    pawnData.mount != null || pawnData.owning.Downed || pawnData.owning.Dead || 
                    !pawnData.owning.Spawned || pawnData.owning.IsBurning())
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

            if (ModSettings_GiddyUp.rideAndRollEnabled) RnRPostfix(__instance, ref __result, pawn);
            if (ModSettings_GiddyUp.caravansEnabled) CaravanPostFix(__instance, ref __result, pawn);

            void RnRPostfix(Pawn_JobTracker jobTracker, ref ThinkResult thinkResult, Pawn pawn)
            {
                if (!pawn.IsColonistPlayerControlled ||
                    pawn.def.race.intelligence != Intelligence.Humanlike ||
                    thinkResult.Job == null || 
                    thinkResult.Job.def == ResourceBank.JobDefOf.Mount || 
                    pawn.Drafted || 
                    pawn.InMentalState || 
                    pawn.IsBorrowedByAnyFaction() ||
                    GiddyUp.Setup.isMounted.Contains(pawn.thingIDNumber))
                {
                    return;
                }
                
                ExtendedPawnData pawnData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
                if (pawnData == null) return;

                LocalTargetInfo firstTarget = null;
                LocalTargetInfo secondTarget = null;

                //For some jobs the first target is B, and the second A.
                if (thinkResult.Job.def == JobDefOf.TendPatient || thinkResult.Job.def == JobDefOf.Refuel || thinkResult.Job.def == JobDefOf.FixBrokenDownBuilding)
                {
                    firstTarget = DistanceUtility.GetFirstTarget(thinkResult.Job, TargetIndex.B);
                    secondTarget = DistanceUtility.GetFirstTarget(thinkResult.Job, TargetIndex.A);
                }
                else if (thinkResult.Job.def == JobDefOf.DoBill && !thinkResult.Job.targetQueueB.NullOrEmpty()) {
                    firstTarget = thinkResult.Job.targetQueueB[0];
                    secondTarget = DistanceUtility.GetFirstTarget(thinkResult.Job, TargetIndex.A);
                }
                else
                {
                    firstTarget = DistanceUtility.GetFirstTarget(thinkResult.Job, TargetIndex.A);
                    secondTarget = DistanceUtility.GetFirstTarget(thinkResult.Job, TargetIndex.B);
                }
                if (!firstTarget.IsValid)
                {
                    return;
                }
                if (pawnData.wasRidingToJob)
                {
                    pawnData.wasRidingToJob = false;
                    return;
                }

                if(pawn.mindState != null && pawn.mindState.duty != null && (pawn.mindState.duty.def == DutyDefOf.TravelOrWait || pawn.mindState.duty.def == DutyDefOf.TravelOrLeave))
                {
                    return;
                }

                Pawn bestChoiceAnimal = null;

                float pawnTargetDistance = pawn.Position.DistanceTo(firstTarget.Cell);
                float firstToSecondTargetDistance = 0;
                if (thinkResult.Job.def == JobDefOf.HaulToCell || thinkResult.Job.def == JobDefOf.HaulToContainer)
                {
                    if (secondTarget.IsValid)
                    {
                        firstToSecondTargetDistance = firstTarget.Cell.DistanceTo(secondTarget.Cell);
                    }
                }
                float totalDistance = pawnTargetDistance + firstToSecondTargetDistance;
                if (totalDistance > GiddyUp.ModSettings_GiddyUp.minAutoMountDistance)
                {
                    bestChoiceAnimal = GetBestChoiceAnimal(pawn, firstTarget, secondTarget, pawnTargetDistance, firstToSecondTargetDistance);
                    if (bestChoiceAnimal != null)
                    {
                        thinkResult = InsertMountingJobs(thinkResult, pawn, bestChoiceAnimal, firstTarget, jobTracker);
                    }
                    //Log.Message("timeNeededOriginal: " + timeNeededOriginal);
                    //Log.Message("adjusted ticks per move: " + TicksPerMoveUtility.adjustedTicksPerMove(pawn, closestAnimal, true));
                    //Log.Message("original ticks per move: " + pawn.TicksPerMoveDiagonal);
                }

                //Embedded methods
                //Gets animal that'll get the pawn to the target the quickest. Returns null if no animal is found or if walking is faster. 
                Pawn GetBestChoiceAnimal(Pawn pawn, LocalTargetInfo target, LocalTargetInfo secondTarget, float pawnTargetDistance, float firstToSecondTargetDistance)
                {
                    //float minDistance = float.MaxValue;
                    Pawn closestAnimal = null;
                    float timeNeededMin = (pawnTargetDistance + firstToSecondTargetDistance) / pawn.GetStatValue(StatDefOf.MoveSpeed);
                    ExtendedPawnData pawnData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
                    bool firstTargetNoMount = false;
                    bool secondTargetNoMount = false;

                    Map map = pawn.Map;

                    GiddyUp.Zones.Area_GU.GetGUAreasFast(map, out Area areaNoMount, out Area areaDropAnimal);
                    var index = map.cellIndices.CellToIndex(target.Cell);

                    if (areaNoMount != null && areaNoMount.innerGrid[index])
                    {
                        firstTargetNoMount = true;
                        if(pawnTargetDistance < GiddyUp.ModSettings_GiddyUp.minAutoMountDistance)
                        {
                            return null;
                        }
                    }
                    
                    //If owning an animal, prefer this animal
                    //This'll make sure pawns prefer the animals they were already riding previously.
                    if (pawnData.owning != null && pawnData.owning.Spawned && !AnimalNotAvailable(pawnData.owning, pawn) && pawn.CanReserve(pawnData.owning))
                    {
                        return pawnData.owning;  
                    }
                    //Otherwise search the animal on the map that gets you to the goal the quickest
                    foreach (Pawn animal in from p in map.mapPawns.AllPawnsSpawned
                                            where p.RaceProps.Animal && IsMountableUtility.isMountable(p) && p.CurJob != null && p.CurJob.def != ResourceBank.JobDefOf.Mounted
                                            select p)
                    {
                        if (AnimalNotAvailable(animal, pawn) || !pawn.CanReserve(animal))
                        {
                            continue;
                        }
                        float distanceFromAnimal = animal.Position.DistanceTo(target.Cell);
                        if (!firstTargetNoMount)
                        {
                            distanceFromAnimal += firstToSecondTargetDistance;
                        }
                        if(distanceFromAnimal < GiddyUp.ModSettings_GiddyUp.minAutoMountDistanceFromAnimal)
                        {
                            continue;
                        }
                        ExtendedPawnData animalData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(animal.thingIDNumber);
                        if(animalData.ownedBy != null)
                        {
                            continue;
                        }
                        if (!animalData.mountableByMaster && !animalData.mountableByAnyone)
                        {
                            continue;
                        }
                        else if (!animalData.mountableByAnyone && animalData.mountableByMaster)
                        {
                            if (animal.playerSettings != null && animal.playerSettings.Master != pawn)
                            {
                                continue;
                            }
                        }

                        float timeNeeded = CalculateTimeNeeded(pawn, target.Cell, index, secondTarget.Cell, firstToSecondTargetDistance, animal, firstTargetNoMount, secondTargetNoMount, areaDropAnimal);

                        if (timeNeeded < timeNeededMin)
                        {
                            closestAnimal = animal;
                            timeNeededMin = timeNeeded;
                        }
                    }
                    return closestAnimal;
                }
                ThinkResult InsertMountingJobs(ThinkResult __result, Pawn pawn, Pawn closestAnimal, LocalTargetInfo target, Pawn_JobTracker __instance)
                {
                    if (pawn.CanReserve(target) && pawn.CanReserve(closestAnimal))
                    {
                        Job oldJob = __result.Job;

                        ExtendedPawnData pawnDataTest = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
                        pawnDataTest.targetJob = oldJob;
                        Job mountJob = new Job(ResourceBank.JobDefOf.Mount, closestAnimal);
                        __instance.jobQueue.EnqueueFirst(oldJob);
                        __result = new ThinkResult(mountJob, __result.SourceNode, __result.Tag, false);
                    }
                    return __result;
                }
                bool AnimalNotAvailable(Pawn animal, Pawn rider)
                {
                    if (animal.Dead || animal.Downed || animal.IsBurning() || animal.InMentalState || !animal.Spawned) //animal in bad state, should return before checking other things
                    {
                        return true;
                    }

                    if (animal.IsForbidden(rider))
                    {
                        return true;
                    }

                    if (animal.Faction == null || !animal.factionInt.def.isPlayer) //animal has wrong faction
                    {
                        return true;
                    }

                    if (animal.health != null && animal.health.summaryHealth.SummaryHealthPercent < 1) //animal wounded
                    {
                        return true;
                    }
                    if (animal.health.HasHediffsNeedingTend())
                    {
                        return true;
                    }
                    if (animal.HungryOrTired())
                    {
                        return true;
                    }

                    if (animal.GetLord() != null)
                    {
                        if (animal.GetLord().LordJob != null && animal.GetLord().LordJob is LordJob_FormAndSendCaravan) //animal forming caravan
                        {
                            return true;
                        }
                    }
                    if (animal.CurJob != null && animal.CurJob.def == JobDefOf.LayDown && animal.needs?.rest != null && animal.needs.rest.CurLevelPercentage < 0.5f)//only allow resting animals if they have enough energy. 
                    {
                        return true;
                    }

                    if (animal.CurJob != null && (animal.CurJob.def == JobDefOf.Lovin || animal.CurJob.def == JobDefOf.Ingest || animal.CurJob.def == ResourceBank.JobDefOf.Mounted)) //animal occupied
                    {
                        return true;
                    }

                    return false;

                }
                //uses abstract unit of time. Real time values aren't needed, only relative values. 
                float CalculateTimeNeeded(Pawn pawn, IntVec3 target, int index, IntVec3 secondTarget, float firstToSecondTargetDistance, Pawn animal, bool firstTargetNoMount, bool secondTargetNoMount, Area areaDropAnimal)
                {
                    var animalPos = animal.Position;
                    float walkDistance = pawn.Position.DistanceTo(animalPos);
                    float rideDistance = 0f;
                    if (areaDropAnimal != null)
                    {
                        if (firstTargetNoMount)
                        {
                            IntVec3 parkLoc = DistanceUtility.GetClosestAreaLoc(animalPos, areaDropAnimal);
                            rideDistance = animalPos.DistanceTo(parkLoc);
                            walkDistance += parkLoc.DistanceTo(target) + firstToSecondTargetDistance;
                        }
                        else if (secondTargetNoMount && secondTarget.IsValid)
                        {
                            IntVec3 parkLoc = DistanceUtility.GetClosestAreaLoc(target, areaDropAnimal);
                            rideDistance += animalPos.DistanceTo(target) + target.DistanceTo(parkLoc);
                            walkDistance += parkLoc.DistanceTo(secondTarget);
                        }
                    }
                    else
                    {
                        rideDistance += animalPos.DistanceTo(target) + firstToSecondTargetDistance;
                    }
                    var areaNoMount = pawn.Map.areaManager.GetLabeled(GiddyUp.Setup.NOMOUNT_LABEL);
                    if(areaNoMount != null)
                    {
                        if (areaNoMount.innerGrid[index] || (secondTarget.IsValid && areaNoMount.innerGrid[index]))
                        {
                            walkDistance += 10; //apply a fixed 10 cell walk penalty when the animal has to be penned
                        }
                    }

                    var animalBaseSpeed = animal.GetStatValue(StatDefOf.MoveSpeed);
                    var pawnPaseSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed);

                    var animalMountedSpeed = GiddyUp.Stats.StatPart_Riding.GetRidingSpeed(animalBaseSpeed, animal, pawn);

                    float timeNeeded = walkDistance/pawnPaseSpeed + rideDistance/animalMountedSpeed;
                    return timeNeeded;
                }
            }
            void CaravanPostFix(Pawn_JobTracker jobTracker, ref ThinkResult __result, Pawn pawn)
            {
                var lord = pawn.GetLord();
                if (lord == null) return;
                if (pawn.RaceProps.Animal && pawn.Faction != Faction.OfPlayer && pawn.Faction != null)
                {
                    if ((lord.CurLordToil is LordToil_DefendPoint || lord.CurLordToil.GetType() == typeof(LordToil_DefendTraderCaravan)) &&
                    (__result.SourceNode is JobGiver_Wander jobGiver_Wander))
                    {
                        jobGiver_Wander.wanderRadius = 5f;
                    }
                }
                //Check if pawn is enemy and can mount.
                if (pawn.IsColonistPlayerControlled || pawn.IsBorrowedByAnyFaction() || pawn.RaceProps.Animal || pawn.Faction.HostileTo(Faction.OfPlayer) || !pawn.RaceProps.Humanlike)
                {            
                    return;
                }
                if (pawn.IsPrisoner) return;
                if(__result.Job == null) return; //shouldn't happen, but may happen with mods.

                LocalTargetInfo target = DistanceUtility.GetFirstTarget(__result.Job, TargetIndex.A);
                if (!target.IsValid) return;

                ExtendedDataStorage store = GiddyUp.Setup._extendedDataStorage;

                ExtendedPawnData pawnData = store.GetExtendedDataFor(pawn.thingIDNumber);
                if(__result.Job.def == GiddyUp.ResourceBank.JobDefOf.Dismount || __result.Job.def == GiddyUp.ResourceBank.JobDefOf.Mount)
                {
                    return;
                }

                QueuedJob qJob = pawn.jobs.jobQueue.FirstOrFallback(null);
                if(qJob != null && (qJob.job.def == GiddyUp.ResourceBank.JobDefOf.Dismount || qJob.job.def == GiddyUp.ResourceBank.JobDefOf.Mount))
                {
                    return;
                }

                if (lord.CurLordToil is LordToil_ExitMapAndEscortCarriers || lord.CurLordToil is LordToil_Travel || lord.CurLordToil is LordToil_ExitMap || lord.CurLordToil is LordToil_ExitMapTraderFighting)
                {
                    if (pawnData.owning != null &&
                        pawnData.owning.Faction == pawn.Faction &&
                        pawnData.mount == null && 
                        !pawnData.owning.Downed &&
                        pawnData.owning.Spawned && 
                        !pawn.IsBurning() &&
                        !pawn.Downed)
                    {
                        MountAnimal(jobTracker, pawn, pawnData, ref __result);
                    }
                }
                else if(lord.CurLordToil.GetType() == typeof(LordToil_DefendTraderCaravan) || lord.CurLordToil is LordToil_DefendPoint) //first option is internal class, hence this way of accessing. 
                {
                    if (pawnData.mount != null)
                    {
                        ParkAnimal(jobTracker, pawn, pawnData);
                    }
                }

                void MountAnimal(Pawn_JobTracker __instance, Pawn pawn, ExtendedPawnData pawnData, ref ThinkResult __result)
                {
                    Job oldJob = __result.Job;
                    Job mountJob = new Job(GiddyUp.ResourceBank.JobDefOf.Mount, pawnData.owning);
                    mountJob.count = 1;
                    __result = new ThinkResult(mountJob, __result.SourceNode, __result.Tag, false);
                    __instance.jobQueue.EnqueueFirst(oldJob);
                }
                void ParkAnimal(Pawn_JobTracker __instance, Pawn pawn, ExtendedPawnData pawnData)
                {
                    Area_GU areaFound = (Area_GU) pawn.Map.areaManager.GetLabeled(GiddyUp.Setup.DropAnimal_NPC_LABEL);
                    IntVec3 targetLoc = pawn.Position;

                    if(areaFound != null && areaFound.ActiveCells.Count() > 0)
                    {
                        targetLoc = DistanceUtility.getClosestAreaLoc(pawn, areaFound);
                    }
                    if (pawn.Map.reachability.CanReach(pawn.Position, targetLoc, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
                    {
                        Job dismountJob = new Job(GiddyUp.ResourceBank.JobDefOf.Dismount);
                        dismountJob.count = 1;
                        __instance.jobQueue.EnqueueFirst(dismountJob);
                        __instance.jobQueue.EnqueueFirst(new Job(JobDefOf.Goto, targetLoc));
                        PawnDuty animalDuty = pawnData.mount.mindState.duty;
                        //if(pawnData.mount.GetLord().CurLordToil is LordToil)

                        if(animalDuty != null)
                        {
                            animalDuty.focus = new LocalTargetInfo(targetLoc);
                        }
                    }
                    else
                    {
                        Messages.Message("GU_Car_NotReachable_DropAnimal_NPC_Message".Translate(), new RimWorld.Planet.GlobalTargetInfo(targetLoc, pawn.Map), MessageTypeDefOf.NegativeEvent);
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.Notify_MasterDraftedOrUndrafted))]
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
