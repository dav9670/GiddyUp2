using GiddyUp;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace GiddyUpRideAndRoll.Jobs
{
	class JobDriver_WaitForRider : JobDriver
	{
		Pawn waitingFor;
		JobDef initialJob; //Keep track of the pawn's job
		int ticker = 1;
		bool riderReturning;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref this.riderReturning, "riderReturning");
			Scribe_Values.Look(ref this.initialJob, "initialJob");
			Scribe_Values.Look(ref this.ticker, "ticker");
			Scribe_Defs.Look<JobDef>(ref this.initialJob, "initialJob");
		}
		public override IEnumerable<Toil> MakeNewToils()
		{
			waitingFor = TargetA.Thing as Pawn;
			this.FailOn(() => pawn.Map == null || waitingFor == null);
			initialJob = waitingFor.CurJobDef;
			yield return new Toil { initAction = () => WalkRandomNearby(), defaultCompleteMode = ToilCompleteMode.Instant };
			yield return new Toil
			{
				defaultCompleteMode = ToilCompleteMode.Never,
				tickAction = delegate
				{
					if (ticker % 30 == 0)
					{
						if (waitingFor.Map == null ||
						waitingFor.Dead ||
						waitingFor.Downed ||
						waitingFor.InMentalState ||
						waitingFor.CurJobDef == ResourceBank.JobDefOf.Mount ||
						waitingFor.InBed() ||
						pawn.health.HasHediffsNeedingTend() ||
						(pawn.needs.food != null && pawn.needs.food.CurCategory >= HungerCategory.UrgentlyHungry) ||
						(pawn.needs.rest != null && pawn.needs.rest.CurCategory >= RestCategory.VeryTired))
						{
							EndJobWith(JobCondition.Succeeded);
							return;
						}
						//One last check - if the animal is in a barn, they needn't wait.
						var room = pawn.GetRoom();
						if (room.Role != null && room.Role == ResourceBank.RoomRoleDefOf.Barn)
						{
							EndJobWith(JobCondition.Succeeded);
							return;
						}

						//Wait a bit longer if the rider's job is just taking awhile
						if (job.expiryInterval - 30 < 31 && waitingFor.CurJobDef == initialJob)
						{
							pawn.CurJob.expiryInterval += 1000;
						}

						//Check if the rider is attempting to abandon the mount
						if (!riderReturning && waitingFor.pather.Destination.Cell.DistanceTo(pawn.Position) > 50f)
						{
							//"Wait, come pick me up!"
							if (waitingFor.Position.DistanceTo(pawn.Position) < 15f)
							{
								riderReturning = true;
								waitingFor.GoMount(pawn, MountUtility.GiveJobMethod.Inject);
							}
							//"Fine, I'll follow you instead :pouting_cat:"
							else if (pawn.roping == null || !pawn.roping.IsRoped)
							{
								pawn.pather.StartPath(waitingFor.pather.Destination.Cell, PathEndMode.Touch);
							}
						}
						//Did the rider get interupted?
						else if (riderReturning && waitingFor.CurJobDef != ResourceBank.JobDefOf.Mount) riderReturning = false;
					}
					
					if (ticker-- == 0)
					{
						ticker = Rand.Range(300, 600);
						if (!pawn.pather.Moving) WalkRandomNearby();
					}
				},
				finishActions = new List<Action>() { delegate
				{
					if (waitingFor.CurJobDef != ResourceBank.JobDefOf.Mount)
					{
						var pen = AnimalPenUtility.GetPenAnimalShouldBeTakenTo(waitingFor, pawn, out string failReason, true);
						if (pen != null)
						{
							waitingFor.jobs.jobQueue.EnqueueFirst(new Job(JobDefOf.RopeToPen, pawn, AnimalPenUtility.FindPlaceInPenToStand(pen, waitingFor)));
						}
					}

					UnsetOwnership();
				}}
			};
		}
		void UnsetOwnership()
		{
			ExtendedPawnData animalData = pawn.GetGUData();
			if (animalData.reservedBy != null)
			{
				ExtendedPawnData riderData = animalData.reservedBy.GetGUData();	
				if (riderData.reservedMount == pawn) riderData.ReservedMount = null;
			}
			animalData.ReservedBy = null;
		}
		void WalkRandomNearby()
		{
			if (pawn.roping != null && pawn.roping.IsRoped)
			{
				IntVec3 target = RCellFinder.RandomWanderDestFor(pawn, pawn.roping.RopedToSpot , 5, ((Pawn p, IntVec3 loc, IntVec3 root) => true), Danger.None);
				pawn.pather.StartPath(target, PathEndMode.Touch);
			}
			else
			{
				var room = waitingFor.GetRoom();
				if (room == null || room.Role == RoomRoleDefOf.None)
				{
					IntVec3 target = RCellFinder.RandomWanderDestFor(waitingFor, waitingFor.Position, 8, ((Pawn p, IntVec3 loc, IntVec3 root) => true), Danger.Some);
					pawn.pather.StartPath(target, PathEndMode.Touch);
				}
			}
		}
	}
}