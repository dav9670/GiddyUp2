using GiddyUp;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using System.Linq;
using System.Collections.Generic;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpCaravan.Harmony
{
	[HarmonyPatch(typeof(LordToil_PrepareCaravan_Leave), nameof(LordToil_PrepareCaravan_Leave.UpdateAllDuties))]
	static class Lordtoil_PrepareCaravan_Leave_UpdateAllDuties
	{
		static bool Prepare()
		{
			return Settings.caravansEnabled;
		}
		static void Postfix(LordToil_PrepareCaravan_Leave __instance)
		{
			foreach (Pawn pawn in __instance.lord.ownedPawns)
			{
				if (pawn.RaceProps.Animal) continue;
				var pawnData = pawn.GetGUData();
				if (pawnData.reservedMount == null) continue;
				pawn.GoMount(pawnData.reservedMount, MountUtility.GiveJobMethod.Inject);   
			}
		}
	}
	/*

	[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
	class Patch_StartJob
	{
		static bool Prepare()
		{
			return Settings.caravansEnabled || Settings.rideAndRollEnabled;
		}
		static bool Prefix(Job newJob, Pawn_JobTracker __instance)
		{
			Log.Message(":eyes:");
			if (newJob != null && newJob.def == JobDefOf.PrepareCaravan_CollectAnimals)
			{
				Log.Message("O_O");
				Pawn pawn = __instance.pawn;
				Pawn animal = pawn.GetGUData().reservedMount;
				if (animal != null)
				{
					Log.Message("^_^");
					pawn.jobs?.jobQueue?.EnqueueFirst(newJob);
					pawn.GoMount(animal, MountUtility.GiveJobMethod.Inject);
					return false;
				}
			}
			return true;
		}
	}
	
	//Normally, automount injection is handled by Patch_DetermineNextJob, sometimes this doesn't work for all jobs depending on how they're called
	//TODO: This is for the caravan roping before heading out. This whole this could probably be made generic and applied to a number of special cases
	//[HarmonyPatch(typeof(JobDriver_RopeToDestination), nameof(JobDriver_RopeToDestination.MakeNewToils))]
	class Patch_RopeToDestination
	{
		static bool Prepare()
		{
			return Settings.caravansEnabled || Settings.rideAndRollEnabled;
		}
		static IEnumerable<Toil> Postfix(IEnumerable<Toil> toils, JobDriver_RopeToDestination __instance)
		{
			Pawn pawn = __instance.pawn;
			int toilNum = 0;
			foreach (var toil in toils)
			{
				if (toilNum++ == 0)
				{
					toil.AddPreInitAction( delegate
					{
						var pawnData = pawn.GetGUData();
						Log.Message(":eyes:");
						if (pawn.CurJobDef != ResourceBank.JobDefOf.Mount && pawnData.reservedMount != null && pawnData.mount != pawnData.reservedMount && 
							pawn.mindState?.duty?.def == DutyDefOf.PrepareCaravan_CollectAnimals)
						{
							Log.Message("O_O");
							pawn.GoMount(pawnData.reservedMount, MountUtility.GiveJobMethod.Inject);
							return;
						}
					});
				}
				yield return toil;
			}
		}
	}
	*/
}