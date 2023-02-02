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
using Settings = GiddyUp.ModSettings_GiddyUp;

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

            if (Settings.rideAndRollEnabled) RnRPostfix(__instance, ref __result, pawn);
            if (Settings.caravansEnabled) CaravanPostFix(__instance, ref __result, pawn);

            void RnRPostfix(Pawn_JobTracker jobTracker, ref ThinkResult thinkResult, Pawn pawn)
            {
                if (!pawn.IsColonistPlayerControlled ||
                    pawn.def.race.intelligence != Intelligence.Humanlike ||
                    thinkResult.Job == null || 
                    thinkResult.Job.def == ResourceBank.JobDefOf.Mount || 
                    pawn.Drafted || 
                    pawn.InMentalState || 
                    pawn.IsBorrowedByAnyFaction() ||
                    Setup.isMounted.Contains(pawn.thingIDNumber))
                {
                    return;
                }
                
                ExtendedPawnData pawnData = Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);

                IntVec3 firstTarget;
                IntVec3 secondTarget;

                //For some jobs the first target is B, and the second A.
                var thinkResultJob = thinkResult.Job;
                var thinkResultJobDef = thinkResultJob.def;
                if (thinkResultJobDef == JobDefOf.TendPatient || thinkResultJobDef == JobDefOf.Refuel || thinkResultJobDef == JobDefOf.FixBrokenDownBuilding)
                {
                    firstTarget = DistanceUtility.GetFirstTarget(thinkResultJob, TargetIndex.B);
                    secondTarget = DistanceUtility.GetFirstTarget(thinkResultJob, TargetIndex.A);
                }
                else if (thinkResultJobDef == JobDefOf.DoBill && !thinkResultJob.targetQueueB.NullOrEmpty()) {
                    firstTarget = thinkResultJob.targetQueueB[0].Cell;
                    secondTarget = DistanceUtility.GetFirstTarget(thinkResultJob, TargetIndex.A);
                }
                else
                {
                    firstTarget = DistanceUtility.GetFirstTarget(thinkResultJob, TargetIndex.A);
                    secondTarget = DistanceUtility.GetFirstTarget(thinkResultJob, TargetIndex.B);
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

                float pawnTargetDistance = pawn.Position.DistanceTo(firstTarget);
                float firstToSecondTargetDistance = 0;
                if (secondTarget.IsValid && (thinkResultJobDef == JobDefOf.HaulToCell || thinkResultJobDef == JobDefOf.HaulToContainer))
                {
                    firstToSecondTargetDistance = firstTarget.DistanceTo(secondTarget);
                }
                float totalDistance = pawnTargetDistance + firstToSecondTargetDistance;
                if (totalDistance > Settings.minAutoMountDistance)
                {
                    bestChoiceAnimal = GetBestChoiceAnimal(pawn, firstTarget, secondTarget, pawnTargetDistance, firstToSecondTargetDistance);
                    if (bestChoiceAnimal != null)
                    {
                        thinkResult = InsertMountingJobs(thinkResult, pawn, bestChoiceAnimal, firstTarget, jobTracker.jobQueue);
                    }
                }

                //Embedded methods
                //Gets animal that'll get the pawn to the target the quickest. Returns null if no animal is found or if walking is faster. 
                Pawn GetBestChoiceAnimal(Pawn pawn, IntVec3 firstTarget, IntVec3 secondTarget, float pawnTargetDistance, float firstToSecondTargetDistance)
                {
                    //Prepare locals
                    float pawnWalkSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed);
                    float timeNormalWalking = (pawnTargetDistance + firstToSecondTargetDistance) / pawnWalkSpeed;
                    ExtendedPawnData pawnData = Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
                    bool firstTargetInForbiddenArea = false;
                    bool secondTargetInForbiddenArea = false;
                    Map map = pawn.Map;
                    Zones.Area_GU.GetGUAreasFast(map, out Area areaNoMount, out Area areaDropAnimal);

                    //This notes that the first destination is in a no-ride zone
                    IntVec3[] areaDropCache = new IntVec3[0];
                    if (areaNoMount != null && areaDropAnimal != null)
                    {
                        firstTargetInForbiddenArea = areaNoMount.innerGrid[map.cellIndices.CellToIndex(firstTarget)];
                        secondTargetInForbiddenArea = secondTarget.y >= 0 && areaNoMount.innerGrid[map.cellIndices.CellToIndex(secondTarget)];
                        areaDropCache = areaDropAnimal.ActiveCells.ToArray();
                    }

                    //Start looking for an animal
                    Pawn closestAnimal = null;
                    float timeBestRiding = float.MaxValue;
                    float distanceBestRiding = float.MaxValue;
                    var list = map.mapPawns.pawnsSpawned;
                    var length = list.Count;
                    for (int i = 0; i < length; i++)
                    {
                        Pawn animal = list[i];

                        if ((!animal.RaceProps.Animal || !IsMountableUtility.IsMountable(animal) || animal.CurJob == null || animal.CurJob.def == ResourceBank.JobDefOf.Mounted) || 
                            (AnimalNotAvailable(animal, pawn) || !pawn.CanReserve(animal))
                        )
                        {
                            continue;
                        }
                    
                        ExtendedPawnData animalData = Setup._extendedDataStorage.GetExtendedDataFor(animal.thingIDNumber);
                        if(animalData.ownedBy != null) continue; //Already in use
                        if (!animalData.mountableByAnyone) continue; //Disallowed


                        #region CalculateTime
                        float distanceRiding = 0f;
                        var animalPos = animal.Position;
                        float distancePawnToAnimal = pawn.Position.DistanceTo(animalPos);
                        
                        //If the first target is in a forbidden ride area, we want to find which is the best drop off point to use.
                        IntVec3 firstDropOffPoint = IntVec3.Zero;
                        if (firstTargetInForbiddenArea)
                        {
                            float workingNum = float.MaxValue;
                            for (int j = 0; j < areaDropCache.Length; j++)
                            {
                                var cell = areaDropCache[j];
                                float tmp = animalPos.DistanceTo(cell) + cell.DistanceTo(firstTarget);
                                if (tmp < workingNum)
                                {
                                    workingNum = tmp;
                                    firstDropOffPoint = cell;
                                }
                            }
                            distanceRiding = workingNum;
                        }
                        else distanceRiding += animalPos.DistanceTo(firstTarget);

                        if (secondTargetInForbiddenArea)
                        {
                            //Return to drop off point
                            if (firstDropOffPoint != IntVec3.Zero)
                            {
                                distanceRiding += firstTarget.DistanceTo(firstDropOffPoint);
                            }
                            float workingNum = float.MaxValue;
                            for (int j = 0; j < areaDropCache.Length; j++)
                            {
                                var cell = areaDropCache[j];
                                float tmp = firstTarget.DistanceTo(cell) + cell.DistanceTo(secondTarget);
                                if (tmp < workingNum)
                                {
                                    workingNum = tmp;
                                }
                            }
                            distanceRiding += workingNum;
                        }
                        else distanceRiding += firstToSecondTargetDistance;

                        distanceRiding *= 1.05f; //Slight lag compensation due to targets moving around
                       
                        var animalMountedSpeed = Stats.StatPart_Riding.GetRidingSpeed(animal.GetStatValue(StatDefOf.MoveSpeed), animal, pawn.skills);

                        float timeNeededForThisMount = (distancePawnToAnimal / pawnWalkSpeed) + (distanceRiding / animalMountedSpeed);
                        #endregion

                        
                        if (timeNeededForThisMount < timeBestRiding)
                        {
                            closestAnimal = animal;
                            timeBestRiding = timeNeededForThisMount;
                            distanceBestRiding = distanceRiding; //Only used for logging
                        }
                    }
                    
                    if (Settings.logging)
                    {
                        if (closestAnimal == null) Log.Message("[Giddy-up] " + (pawn.Name?.ToString() ?? "NULL") + " tried to find an animal but couldn't fnid any.");
                        else Log.Message("[Giddy-up] report for " + (pawn.Name?.ToString() ?? "NULL") + ":\n" +
                        "Animal: " + (closestAnimal.Name?.ToString() ?? closestAnimal.thingIDNumber.ToString()) + "\n" +
                        "First target: " + (firstTarget.ToString()) + "\n" + 
                        "Second target: " + (secondTarget.ToString()) + "\n" + 
                        "Normal walking speed: " + ((int)(pawnWalkSpeed)).ToString() + "\n" + 
                        "Normal walking distance: " + ((int)(pawnTargetDistance + firstToSecondTargetDistance)).ToString() + "\n" + 
                        "Normal walking time: " + ((int)timeNormalWalking).ToString() + "\n" + 
                        "Distance to animal: " + ((int)pawn.Position.DistanceTo(closestAnimal.Position)).ToString() + "\n" + 
                        "Ride distance: " + ((int)distanceBestRiding).ToString()  + "\n" + 
                        "Ride speed: " + ((int)Stats.StatPart_Riding.GetRidingSpeed(closestAnimal.GetStatValue(StatDefOf.MoveSpeed), closestAnimal, pawn.skills)).ToString() + "\n" +
                        "Ride time: " + ((int)((pawn.Position.DistanceTo(closestAnimal.Position) / pawnWalkSpeed) + 
                            (distanceBestRiding / Stats.StatPart_Riding.GetRidingSpeed(closestAnimal.GetStatValue(StatDefOf.MoveSpeed), closestAnimal, pawn.skills)))).ToString()
                        );
                    }

                    if (timeBestRiding < timeNormalWalking)
                    {
                        return closestAnimal;
                    }
                    
                    return null;
                }
                ThinkResult InsertMountingJobs(ThinkResult __result, Pawn pawn, Pawn closestAnimal, IntVec3 target, JobQueue jobQueue)
                {
                    if (pawn.CanReserve(target))
                    {
                        Job oldJob = __result.Job;

                        Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber).targetJob = oldJob;
                        Job mountJob = new Job(ResourceBank.JobDefOf.Mount, closestAnimal);
                        jobQueue.EnqueueFirst(oldJob);
                        __result = new ThinkResult(mountJob, __result.SourceNode, __result.Tag, false);
                    }
                    return __result;
                }
                bool AnimalNotAvailable(Pawn animal, Pawn rider)
                {
                    if ((animal.Dead || animal.Downed || animal.IsBurning() || animal.InMentalState || !animal.Spawned) || //animal in bad state, should return before checking other things
                        (animal.IsForbidden(rider)) || 
                        (animal.Faction == null || !animal.factionInt.def.isPlayer) || //animal has wrong faction
                        (animal.health != null && animal.health.summaryHealth.SummaryHealthPercent < 1) || //animal wounded
                        animal.health.HasHediffsNeedingTend() || 
                        Utitlities.HungryOrTired(animal.needs)
                    )
                    {
                        return true;
                    }

                    var animalLord = animal.GetLord();
                    if (animalLord != null)
                    {
                        if (animalLord.LordJob != null && animalLord.LordJob is LordJob_FormAndSendCaravan)
                        {
                            return true; //animal forming caravan
                        }
                    }
                    var animalJob = animal.CurJob;
                    if (animalJob != null)
                    {
                        var jobDef = animalJob.def;
                        if ((jobDef == JobDefOf.LayDown && animal.needs?.rest != null && animal.needs.rest.CurLevelPercentage < 0.5f) || //only allow resting animals if they have enough energy
                            (jobDef == JobDefOf.Lovin || jobDef == JobDefOf.Ingest || jobDef == ResourceBank.JobDefOf.Mounted) //animal occupied
                        )
                        {
                            return true; 
                        }
                    }

                    return false;

                }
            }
            void CaravanPostFix(Pawn_JobTracker jobTracker, ref ThinkResult __result, Pawn pawn)
            {
                var lord = pawn.GetLord();
                if (lord == null) return;
                if (pawn.RaceProps.Animal && pawn.Faction != Current.gameInt.worldInt.factionManager.ofPlayer && pawn.Faction != null)
                {
                    if ((lord.CurLordToil is LordToil_DefendPoint || lord.CurLordToil.GetType() == typeof(LordToil_DefendTraderCaravan)) &&
                    (__result.SourceNode is JobGiver_Wander jobGiver_Wander))
                    {
                        jobGiver_Wander.wanderRadius = 5f;
                    }
                }
                //Check if pawn is enemy and can mount.
                if (pawn.IsColonistPlayerControlled || pawn.IsBorrowedByAnyFaction() || pawn.RaceProps.Animal || pawn.Faction.HostileTo(Current.gameInt.worldInt.factionManager.ofPlayer) || !pawn.RaceProps.Humanlike)
                {            
                    return;
                }
                if (pawn.IsPrisoner) return;
                if(__result.Job == null) return; //shouldn't happen, but may happen with mods.

                LocalTargetInfo target = DistanceUtility.GetFirstTarget(__result.Job, TargetIndex.A);
                if (!target.IsValid) return;

                ExtendedDataStorage store = Setup._extendedDataStorage;

                ExtendedPawnData pawnData = store.GetExtendedDataFor(pawn.thingIDNumber);
                if(__result.Job.def == ResourceBank.JobDefOf.Dismount || __result.Job.def == ResourceBank.JobDefOf.Mount)
                {
                    return;
                }

                QueuedJob qJob = pawn.jobs.jobQueue.FirstOrFallback(null);
                if(qJob != null && (qJob.job.def == ResourceBank.JobDefOf.Dismount || qJob.job.def == ResourceBank.JobDefOf.Mount))
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
                    Job mountJob = new Job(ResourceBank.JobDefOf.Mount, pawnData.owning);
                    mountJob.count = 1;
                    __result = new ThinkResult(mountJob, __result.SourceNode, __result.Tag, false);
                    __instance.jobQueue.EnqueueFirst(oldJob);
                }
                void ParkAnimal(Pawn_JobTracker __instance, Pawn pawn, ExtendedPawnData pawnData)
                {
                    Area areaFound = pawn.Map.areaManager.GetLabeled(ResourceBank.DropAnimal_NPC_LABEL);
                    IntVec3 targetLoc = pawn.Position;

                    if(areaFound != null && areaFound.ActiveCells.Count() > 0)
                    {
                        targetLoc = DistanceUtility.GetClosestAreaLoc(pawn, areaFound);
                    }
                    if (pawn.Map.reachability.CanReach(pawn.Position, targetLoc, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
                    {
                        Job dismountJob = new Job(ResourceBank.JobDefOf.Dismount);
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
