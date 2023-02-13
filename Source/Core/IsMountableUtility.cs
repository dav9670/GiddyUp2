using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using System.Collections.Generic;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp
{
	public static class IsMountableUtility
	{
		public enum Reason{NotFullyGrown, NotInModOptions, CanMount, IsRoped, NeedsTraining, IsBusy, IsPoorCondition, WrongFaction, NotAnimal, IsReserved, MissingResearch};

		static HashSet<JobDef> busyJobs = new HashSet<JobDef>() {ResourceBank.JobDefOf.Mounted, JobDefOf.LayEgg, JobDefOf.Nuzzle, JobDefOf.Lovin, JobDefOf.Vomit, JobDefOf.Wait_Downed};

		public static bool IsMountedAnimal(this Pawn animal)
		{
			return IsMountedAnimal(animal, out Thing rider);
		}
		public static bool IsMountedAnimal(this Pawn animal, out Thing rider)
		{
			if (animal.CurJobDef != ResourceBank.JobDefOf.Mounted)
			{
				rider = null;
				return false;
			}
			rider = animal.jobs.curDriver.job.targetA.Thing;
			return rider.IsMounted();
		}
		public static bool IsEverMountable(this Pawn pawn)
		{
			return IsEverMountable(pawn, out Reason reason);
		}
		public static bool IsEverMountable(this Pawn pawn, out Reason reason)
		{
			return IsMountable(pawn, out reason, null, false, false, false);
		}
		public static bool IsMountable(this Pawn animal, out Reason reason, Pawn rider, bool checkState = true, bool checkFaction = false, bool checkTraining = true)
		{
			reason = Reason.CanMount;
			//Is even an animal?
			if (!animal.RaceProps.Animal)
			{
				reason = Reason.NotAnimal;
				return false;
			}
			//Check faction
			if (checkFaction && animal.Faction != rider.Faction)
			{
				reason = Reason.WrongFaction;
				return false;
			}
			//Check mod options
			if (animal == null || !Settings.mountableCache.Contains(animal.def.shortHash))
			{
				reason = Reason.NotInModOptions;
				return false;
			}
			//Check animal's jobs to see if busy
			if (checkState)
			{
				if (busyJobs.Contains(animal.CurJobDef))
				{
					reason = Reason.IsBusy;
					return false;
				}
				//Check if roped
				if (animal.roping?.IsRopedByPawn ?? false)
				{
					reason = Reason.IsRoped;
					return false;
				}
				//animal forming caravan?
				var animalLord = animal.GetLord();
				if (animalLord != null)
				{
					if (animalLord.LordJob != null && animalLord.LordJob is LordJob_FormAndSendCaravan)
					{
						reason = Reason.IsBusy;
						return false;
					}
					//TODO maybe add some logic to check if involved with a ritual
				}
				//Check health
				if (animal.Dead || animal.Downed || animal.InMentalState || !animal.Spawned || 
					(animal.health != null && animal.health.summaryHealth.SummaryHealthPercent < 0.75f) ||
					animal.health.HasHediffsNeedingTend() || 
					animal.HasAttachment(ThingDefOf.Fire) || 
					animal.needs.food != null && animal.needs.food.CurCategory >= HungerCategory.UrgentlyHungry || 
					animal.needs.rest != null && animal.needs.rest.CurCategory >= RestCategory.VeryTired
				)
				{
					reason = Reason.IsPoorCondition;
					return false;
				}
				var modExt = animal.def.GetModExtension<ResearchRestrictions>();
				if (modExt != null)
				{
					foreach (var researchProjectDef in modExt.researchProjectDefs)
					{
						if (!researchProjectDef.IsFinished)
						{
							reason = Reason.MissingResearch;
							return false;
						}
					}
				}
			}
			//Check age
			if (!animal.ageTracker.Adult)
			{
				var customLifeStages = animal.def.GetModExtension<AllowedLifeStages>();
				if (customLifeStages == null || !customLifeStages.IsAllowedAge(animal.ageTracker.CurLifeStageIndex))
				{
					reason = Reason.NotFullyGrown;
					return false;
				}
			}
			//Check training
			if (checkTraining && (animal.training == null || !animal.training.HasLearned(TrainableDefOf.Tameness)))
			{
				reason = Reason.NeedsTraining;
				return false;
			}
			//Can reserve? Null check as this may be a non-specific check like the UI
			if (rider != null && !rider.CanReserve(animal))
			{
				reason = Reason.IsReserved;
				return false;
			}
			
			return true;
		}
		public static bool IsAllowed(this Pawn rider, Pawn animal)
		{
			return rider.IsAllowed(animal.GetGUData());
		}
		public static bool IsAllowed(this Pawn rider, ExtendedPawnData animalData)
		{
			var automount = animalData.automount;
			if (automount == ExtendedPawnData.Automount.Anyone) return true;
			else if (automount == ExtendedPawnData.Automount.Colonists && rider.GuestStatus == null) return true;
			else if (automount == ExtendedPawnData.Automount.Slaves && rider.GuestStatus == GuestStatus.Slave) return true;
			return false;
		}
	}
}