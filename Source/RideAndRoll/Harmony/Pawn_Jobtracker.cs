using GiddyUp;
using GiddyUp.Storage;
using GiddyUp.Utilities;
using GiddyUp.Zones;
using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace GiddyUpRideAndRoll.Harmony
{
    //[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.DetermineNextJob))]
    //Merging postfix to avoid harmony overhead
    public static class GiddyUpRideAndRoll_DetermineNextJob
    {
        public static void Postfix(Pawn_JobTracker jobTracker, ref ThinkResult thinkResult, Pawn pawn)
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
        }
        //Gets animal that'll get the pawn to the target the quickest. Returns null if no animal is found or if walking is faster. 
        static Pawn GetBestChoiceAnimal(Pawn pawn, LocalTargetInfo target, LocalTargetInfo secondTarget, float pawnTargetDistance, float firstToSecondTargetDistance)
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
        static ThinkResult InsertMountingJobs(ThinkResult __result, Pawn pawn, Pawn closestAnimal, LocalTargetInfo target, Pawn_JobTracker __instance)
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
        static bool AnimalNotAvailable(Pawn animal, Pawn rider)
        {
            if (animal.Dead || animal.Downed || animal.IsBurning() || animal.InMentalState || !animal.Spawned) //animal in bad state, should return before checking other things
            {
                return true;
            }

            if (animal.IsForbidden(rider))
            {
                return true;
            }

            if (animal.Faction == null || animal.Faction != Faction.OfPlayer) //animal has wrong faction
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
        static float CalculateTimeNeeded(Pawn pawn, IntVec3 target, int index, IntVec3 secondTarget, float firstToSecondTargetDistance, Pawn animal, bool firstTargetNoMount, bool secondTargetNoMount, Area areaDropAnimal)
        {
            var animalPos = animal.Position;
            float walkDistance = pawn.Position.DistanceTo(animalPos);
            float rideDistance = 0f;
            if (areaDropAnimal != null)
            {
                if (firstTargetNoMount)
                {
                    IntVec3 parkLoc = DistanceUtility.getClosestAreaLoc(animalPos, areaDropAnimal);
                    rideDistance = animalPos.DistanceTo(parkLoc);
                    walkDistance += parkLoc.DistanceTo(target) + firstToSecondTargetDistance;
                }
                else if (secondTargetNoMount && secondTarget.IsValid)
                {
                    IntVec3 parkLoc = DistanceUtility.getClosestAreaLoc(target, areaDropAnimal);
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
}
