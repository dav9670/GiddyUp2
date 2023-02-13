using GiddyUp.Jobs;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
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
			return !__instance.pawn.IsMountedAnimal();
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
				if (pawn.IsMounted() && pawn.IsColonist)
				{
					ExtendedPawnData pawnData = pawn.GetGUData();
					if (pawnData.mount.CurJobDef != ResourceBank.JobDefOf.Mounted ||
						(pawnData.mount.jobs.curDriver is JobDriver_Mounted driver && driver.rider != pawn))
					{
						pawn.Dismount(null, pawnData, true);
					}
				}
				//If a hostile pawn owns an animal, make sure it mounts it whenever possible
				else if (pawn.Faction.HostileTo(Current.gameInt.worldInt.factionManager.ofPlayer) && 
					!pawn.Downed && !pawn.IsPrisoner && !pawn.HasAttachment(ThingDefOf.Fire))
				{

					ExtendedPawnData pawnData = pawn.GetGUData();
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
				else if (Settings.rideAndRollEnabled && pawn.Faction.def.isPlayer) TryAutoMount(__instance, ref __result, pawn);
			}
			if (Settings.caravansEnabled && !pawn.Faction.def.isPlayer)
			{
				//Handle failsafe for roped animals belonging to invalid pawns
				//TODO: Ensure logic, would this be a problem for quest animals you may rope?
				if (pawn.roping != null && pawn.roping.IsRoped)
				{
					var owner = pawn.GetGUData().reservedBy;
					if (owner == null || owner.Dead || !owner.Spawned) pawn.roping.BreakAllRopes();
				}
				HandleVisitorsMounting(__instance, ref __result, pawn);
			}

			//This is responsbile for the automount mechanic
			void TryAutoMount(Pawn_JobTracker jobTracker, ref ThinkResult thinkResult, Pawn pawn)
			{
				if (!pawn.IsColonistPlayerControlled ||
					pawn.def.race.intelligence != Intelligence.Humanlike ||
					thinkResult.Job == null || 
					thinkResult.Job.def == ResourceBank.JobDefOf.Mount || 
					pawn.Drafted || 
					pawn.InMentalState || 
					pawn.IsMounted() ||
					(pawn.mindState != null && pawn.mindState.duty != null && (pawn.mindState.duty.def == DutyDefOf.TravelOrWait || pawn.mindState.duty.def == DutyDefOf.TravelOrLeave)))
				{
					return;
				}

				//Is this a job that cannot be done mounted?
				var jobDef = thinkResult.Job.def;
				
				//Sort out where the targets are. For some jobs the first target is B, and the second A.
				if (!thinkResult.Job.DetermineTargets(out IntVec3 firstTarget, out IntVec3 secondTarget)) return;
				
				//Determine distances
				float pawnTargetDistance = pawn.Position.DistanceTo(firstTarget);
				
				float firstToSecondTargetDistance;
				if (secondTarget.IsValid && (jobDef == JobDefOf.HaulToCell || jobDef == JobDefOf.HaulToContainer)) firstToSecondTargetDistance = firstTarget.DistanceTo(secondTarget);
				else firstToSecondTargetDistance = 0;

				if (pawnTargetDistance + firstToSecondTargetDistance > Settings.minAutoMountDistance)
				{
					//Do some less performant final check. It's less costly to run these near the end on successful mount attempts than to check constantly
					if (pawn.IsWorkTypeDisabledByAge(WorkTypeDefOf.Handling, out int ageNeeded) || pawn.IsBorrowedByAnyFaction()) return;

					if (MountUtility.GetBestAnimal(pawn, out Pawn bestAnimal, firstTarget, secondTarget, pawnTargetDistance, firstToSecondTargetDistance))
					{
						//Finally, go mount up
						thinkResult = pawn.GoMount(bestAnimal, MountUtility.GiveJobMethod.Inject, thinkResult, thinkResult.Job).Value;
					}
				}
			}
			//This is responsible for friendly guests mounting/dismounting their animals they rode in on
			void HandleVisitorsMounting(Pawn_JobTracker jobTracker, ref ThinkResult thinkResult, Pawn pawn)
			{
				Lord lord = pawn.GetLord();
				if (lord == null) return;
				
				if (pawn.RaceProps.Animal && thinkResult.SourceNode is JobGiver_Wander jobGiver_Wander && 
					(lord.CurLordToil is LordToil_DefendPoint || lord.CurLordToil.GetType() == typeof(LordToil_DefendTraderCaravan)))
				{
					jobGiver_Wander.wanderRadius = 5f; //TODO: is this really needed?
				}

				//Filter out anything that is not a guest rider
				if (pawn.def.race.intelligence != Intelligence.Humanlike || pawn.Faction.HostileTo(Current.gameInt.worldInt.factionManager.ofPlayer) || pawn.IsPrisoner || thinkResult.Job == null)
				{            
					return;
				}

				var job = thinkResult.Job;
				if (!job.GetFirstTarget(TargetIndex.A).IsValid) return;

				if (job.def == ResourceBank.JobDefOf.Dismount || job.def == ResourceBank.JobDefOf.Mount)
				{
					return;
				}

				QueuedJob qJob = pawn.jobs.jobQueue.FirstOrFallback(null);
				if (qJob != null && (qJob.job.def == ResourceBank.JobDefOf.Dismount || qJob.job.def == ResourceBank.JobDefOf.Mount))
				{
					return;
				}

				ExtendedPawnData pawnData = pawn.GetGUData();
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
					if (pawnData.mount != null) 
					{
						//Dismount on-the-top if it's a pack animal, the guards want to keep it nearby
						if (pawnData.mount.inventory != null && pawnData.mount.inventory.innerContainer.Count > 0) pawn.Dismount(pawnData.mount, pawnData);
						//Other animals go to the assigned dismount spot.
						else pawn.GoDismount(pawnData.mount, MountUtility.GiveJobMethod.Try);
					}
				}
			}
		}
	}
	//A mount may have a maste, be it the current rider or not. If the rider drafts, the animal will want to go over to them. This patch blocks that, if mounted.
	[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.Notify_MasterDraftedOrUndrafted))]
	static class Pawn_JobTracker_Notify_MasterDraftedOrUndrafted
	{
		static bool Prefix(Pawn_JobTracker __instance)
		{
			Pawn pawn = __instance.pawn;
			return pawn == null || pawn.CurJobDef != ResourceBank.JobDefOf.Mounted;
		}
	}
}