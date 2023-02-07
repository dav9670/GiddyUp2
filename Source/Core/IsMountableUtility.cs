﻿using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp
{
	public static class IsMountableUtility
	{
		public enum Reason{NotFullyGrown, NotInModOptions, CanMount, IsRoped, NeedsTraining, IsBusy, IsPoorCondition, WrongFaction, NotAnimal, IsReserved};

		static JobDef[] busyJobs = new JobDef[] {ResourceBank.JobDefOf.Mounted, JobDefOf.LayEgg, JobDefOf.Nuzzle, JobDefOf.Lovin, JobDefOf.Wait_Downed};

		public static bool IsMounted(this Pawn animal)
		{
			if (animal.CurJob == null || animal.CurJob.def != ResourceBank.JobDefOf.Mounted)
			{
				return false;
			}
			var rider = animal.jobs.curDriver.job.targetA.Thing;
			return ExtendedDataStorage.isMounted.Contains(rider.thingIDNumber);
		}
		public static bool IsEverMountable(this Pawn pawn)
		{
			return IsMountable(pawn, out Reason reason, null, false, false);
		}
		public static bool IsMountable(this Pawn animal, out Reason reason, Pawn rider, bool checkState = true, bool checkFaction = false)
		{
			reason = Reason.CanMount;
			//Is even an animal?
			if (!animal.RaceProps.Animal)
			{
				reason = Reason.NotAnimal;
				return false;
			}
			//Check faction
			if (checkFaction && animal.Faction == null || !animal.factionInt.def.isPlayer)
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
				var animalCurJob = animal.CurJob?.def;
				if (busyJobs.Contains(animalCurJob))
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
				}
				//Check health
				if ((animal.Dead || animal.Downed || animal.InMentalState || !animal.Spawned) || 
					(animal.health != null && animal.health.summaryHealth.SummaryHealthPercent < 1) ||
					animal.health.HasHediffsNeedingTend() || 
					animal.HasAttachment(ThingDefOf.Fire) || 
					animal.needs.food != null && animal.needs.food.CurCategory >= HungerCategory.UrgentlyHungry || 
					animal.needs.rest != null && animal.needs.rest.CurCategory >= RestCategory.VeryTired
				)
				{
					reason = Reason.IsPoorCondition;
					return false;
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
			if (animal.training == null || !animal.training.HasLearned(TrainableDefOf.Tameness))
			{
				reason = Reason.NeedsTraining;
				return false;
			}
			//Can reserve?
			if (rider != null && !rider.CanReserve(animal))
			{
				reason = Reason.IsReserved;
				return false;
			}
			
			return true;
		}
	}
}