using GiddyUp.Jobs;
using GiddyUp;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Linq;
using Verse.AI.Group;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp.Harmony
{
	//This patch prevents animals from starting new jobs if they're currently mounted
	[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
	static class Patch_StartJob
	{    
	   static bool Prefix(Pawn_JobTracker __instance)
	   {
			return __instance.curDriver == null || __instance.curDriver.pawn == null || __instance.curDriver.pawn.CurJobDef != ResourceBank.JobDefOf.Mounted;
	   }
	}
	
	//Postfix, after a job has been determined, inject a job before it to go mount/dismount based on conditions
	[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.DetermineNextJob))]
	static class Patch_DetermineNextJob
	{
		static void Postfix(Pawn_JobTracker __instance, ref ThinkResult __result)
		{
			Pawn pawn = __instance.pawn;
			if (pawn.Faction == null) return;
			if (pawn.def.race.intelligence == Intelligence.Humanlike)
			{
				//Sanity check, make sure the mount driver is still valid
				if (ExtendedDataStorage.isMounted.Contains(pawn.thingIDNumber) && pawn.IsColonist)
				{
					ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
					if (pawnData.mount.CurJobDef != ResourceBank.JobDefOf.Mounted ||
						(pawnData.mount.jobs.curDriver is JobDriver_Mounted driver && driver.Rider != pawn))
					{
						pawn.Dismount(null, pawnData, true);
					}
				}
				//If a hostile pawn owns an animal, make sure it mounts it whenever possible
				else if (pawn.Faction.HostileTo(Current.gameInt.worldInt.factionManager.ofPlayer) && 
					!pawn.Downed && !pawn.IsPrisoner && !pawn.HasAttachment(ThingDefOf.Fire))
				{

					ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
					var hostileMount = pawnData.reservedMount;
					if (hostileMount == null || !hostileMount.IsMountable(out IsMountableUtility.Reason reason, pawn, true, true))
					{
						return;
					}
					QueuedJob qJob = pawn.jobs.jobQueue.FirstOrFallback(null);
					if (qJob?.job.def == ResourceBank.JobDefOf.Mount || __result.Job.def == ResourceBank.JobDefOf.Mount)
					{
						return;
					}

					pawn.GoMount(hostileMount);
				}
				else if (Settings.rideAndRollEnabled && pawn.Faction.def.isPlayer) RnRPostfix(__instance, ref __result, pawn);
			}
			if (Settings.caravansEnabled && !pawn.Faction.def.isPlayer) CaravanPostFix(__instance, ref __result, pawn);

			//This is responsbile for the automount mechanic
			void RnRPostfix(Pawn_JobTracker jobTracker, ref ThinkResult thinkResult, Pawn pawn)
			{
				if (!pawn.IsColonistPlayerControlled ||
					pawn.def.race.intelligence != Intelligence.Humanlike ||
					thinkResult.Job == null || 
					thinkResult.Job.def == ResourceBank.JobDefOf.Mount || 
					pawn.Drafted || 
					pawn.InMentalState || 
					ExtendedDataStorage.isMounted.Contains(pawn.thingIDNumber))
				{
					return;
				}
				
				ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];

				IntVec3 firstTarget;
				IntVec3 secondTarget;

				//For some jobs the first target is B, and the second A.
				var thinkResultJob = thinkResult.Job;
				var thinkResultJobDef = thinkResultJob.def;
				if (thinkResultJobDef == JobDefOf.TendPatient || thinkResultJobDef == JobDefOf.Refuel || thinkResultJobDef == JobDefOf.FixBrokenDownBuilding)
				{
					firstTarget = thinkResultJob.GetFirstTarget(TargetIndex.B);
					secondTarget = thinkResultJob.GetFirstTarget(TargetIndex.A);
				}
				else if (thinkResultJobDef == JobDefOf.DoBill && !thinkResultJob.targetQueueB.NullOrEmpty()) {
					firstTarget = thinkResultJob.targetQueueB[0].Cell;
					secondTarget = thinkResultJob.GetFirstTarget(TargetIndex.A);
				}
				else
				{
					firstTarget = thinkResultJob.GetFirstTarget(TargetIndex.A);
					secondTarget = thinkResultJob.GetFirstTarget(TargetIndex.B);
				}
				if (!firstTarget.IsValid) return;

				if (pawn.mindState != null && pawn.mindState.duty != null && (pawn.mindState.duty.def == DutyDefOf.TravelOrWait || pawn.mindState.duty.def == DutyDefOf.TravelOrLeave))
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
					bestChoiceAnimal = GetBestChoiceAnimal(pawn, firstTarget, secondTarget, pawnTargetDistance, firstToSecondTargetDistance, jobTracker.jobQueue);
					if (bestChoiceAnimal != null)
					{
						//Do some less performant final check. It's less costly to run these near the end on successful mount attempts than to check constantly
						if (pawn.IsWorkTypeDisabledByAge(WorkTypeDefOf.Handling, out int ageNeeded) || pawn.IsBorrowedByAnyFaction()) return;
						//Finally, go mount up
						thinkResult = pawn.GoMount(bestChoiceAnimal, MountUtility.GiveJobMethod.Inject, thinkResult, thinkResult.Job).Value;
					}
				}

				//Embedded methods
				//Gets animal that'll get the pawn to the target the quickest. Returns null if no animal is found or if walking is faster. 
				Pawn GetBestChoiceAnimal(Pawn pawn, IntVec3 firstTarget, IntVec3 secondTarget, float pawnTargetDistance, float firstToSecondTargetDistance, JobQueue jobQueue)
				{
					//Prepare locals
					float pawnWalkSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed);
					float timeNormalWalking = (pawnTargetDistance + firstToSecondTargetDistance) / pawnWalkSpeed;
					ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
					bool firstTargetInForbiddenArea = false;
					bool secondTargetInForbiddenArea = false;
					Map map = pawn.Map;
					Area_GU.GetGUAreasFast(map, out Area areaNoMount, out Area areaDropAnimal);

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

						if (!animal.IsMountable(out IsMountableUtility.Reason reason, pawn, true, true)) continue;
					
						ExtendedPawnData animalData = ExtendedDataStorage.GUComp[animal.thingIDNumber];
						if (!animalData.automount) continue; //Disallowed


						//TODO: Cache the dropoff calculations somehow
						#region CalculateTime
						float distanceRiding = 0f;
						var animalPos = animal.Position;
						float distancePawnToAnimal = pawn.Position.DistanceTo(animalPos);
						
						bool needsPen = (firstTargetInForbiddenArea || secondTargetInForbiddenArea) && (areaNoMount == null || AnimalPenUtility.NeedsToBeManagedByRope(animal));
						IntVec3 firstDropOffPoint = IntVec3.Zero;
						if (firstTargetInForbiddenArea)
						{
							float workingNum = float.MaxValue;
							if (needsPen)
							{
								firstDropOffPoint = DistanceUtility.GetClosestPen(ref workingNum, map, animal, pawn, animalPos, firstTarget);
							}
							else if (areaNoMount != null)
							{
								firstDropOffPoint = DistanceUtility.GetClosestDropoffPoint(ref workingNum, areaDropCache, animalPos, firstTarget);
							}
							distanceRiding = workingNum;
						}
						else distanceRiding += animalPos.DistanceTo(firstTarget);

						if (secondTargetInForbiddenArea)
						{
							//This assumes that the pawn will go from their pickup point, back to the firt drop off point to return to their animal.
							if (firstDropOffPoint != IntVec3.Zero) distanceRiding += firstTarget.DistanceTo(firstDropOffPoint);
							
							float workingNum = float.MaxValue;
							if (needsPen)
							{
								DistanceUtility.GetClosestPen(ref workingNum, map, animal, pawn, firstTarget, secondTarget);
							}
							else if (areaNoMount != null)
							{
								DistanceUtility.GetClosestDropoffPoint(ref workingNum, areaDropCache, firstTarget, secondTarget);
							}
							distanceRiding += workingNum;
						}
						else distanceRiding += firstToSecondTargetDistance;

						distanceRiding *= 1.05f; //Unassurance compensation due to their being more variables and moving parts, data may become stale by the time the pawn arrives
					   
						var animalMountedSpeed = StatPart_Riding.GetRidingSpeed(animal.GetStatValue(StatDefOf.MoveSpeed), animal, pawn.skills);

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
						"Ride speed: " + ((int)StatPart_Riding.GetRidingSpeed(closestAnimal.GetStatValue(StatDefOf.MoveSpeed), closestAnimal, pawn.skills)).ToString() + "\n" +
						"Ride time: " + ((int)((pawn.Position.DistanceTo(closestAnimal.Position) / pawnWalkSpeed) + 
							(distanceBestRiding / StatPart_Riding.GetRidingSpeed(closestAnimal.GetStatValue(StatDefOf.MoveSpeed), closestAnimal, pawn.skills)))).ToString()
						);
					}

					if (timeBestRiding < timeNormalWalking) return closestAnimal;
					return null;
				}
			}
			//This is responsible for friendly guests mounting/dismounting their animals they rode in on
			void CaravanPostFix(Pawn_JobTracker jobTracker, ref ThinkResult thinkResult, Pawn pawn)
			{
				Lord lord = pawn.GetLord();
				if (lord == null) return;
				
				var isAnimal = pawn.RaceProps.Animal;
				if (isAnimal && thinkResult.SourceNode is JobGiver_Wander jobGiver_Wander && (lord.CurLordToil is LordToil_DefendPoint || lord.CurLordToil.GetType() == typeof(LordToil_DefendTraderCaravan)))
				{
					jobGiver_Wander.wanderRadius = 5f; //TODO: is this really needed?
				}

				//Filter out anything that is not a guest rider
				if (pawn.def.race.intelligence != Intelligence.Humanlike || pawn.Faction.HostileTo(Current.gameInt.worldInt.factionManager.ofPlayer) || pawn.IsPrisoner || thinkResult.Job == null)
				{            
					return;
				}

				var job = thinkResult.Job;
				LocalTargetInfo target = job.GetFirstTarget(TargetIndex.A);
				if (!target.IsValid) return;

				if (job.def == ResourceBank.JobDefOf.Dismount || job.def == ResourceBank.JobDefOf.Mount)
				{
					return;
				}

				QueuedJob qJob = pawn.jobs.jobQueue.FirstOrFallback(null);
				if (qJob != null && (qJob.job.def == ResourceBank.JobDefOf.Dismount || qJob.job.def == ResourceBank.JobDefOf.Mount))
				{
					return;
				}

				ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
				var curLordToil = lord.CurLordToil;
				if (curLordToil is LordToil_ExitMapAndEscortCarriers || curLordToil is LordToil_Travel || curLordToil is LordToil_ExitMap || curLordToil is LordToil_ExitMapTraderFighting)
				{
					if (pawnData.reservedMount != null &&
						pawnData.mount == null && 
						pawnData.reservedMount.IsMountable(out IsMountableUtility.Reason reason, pawn, true, true))
					{
						thinkResult = pawn.GoMount(pawnData.reservedMount, MountUtility.GiveJobMethod.Inject, thinkResult, job).Value;
					}
				}
				else if (lord.CurLordToil.GetType() == typeof(LordToil_DefendTraderCaravan) || lord.CurLordToil is LordToil_DefendPoint) //first option is internal class, hence this way of accessing. 
				{
					if (pawnData.mount != null) pawn.GoDismount(pawnData.mount, MountUtility.GiveJobMethod.Try);
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
			return pawn == null || pawn.CurJob == null || pawn.CurJob.def != ResourceBank.JobDefOf.Mounted;
		}
	}
}