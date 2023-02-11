using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using GiddyUpRideAndRoll;
using Verse;
using Verse.AI;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp.Jobs
{
	public class JobDriver_Mounted : JobDriver
	{
		public static HashSet<JobDef> allowedJobs;
		public Pawn Rider { get { return job.targetA.Thing as Pawn; } }
		ExtendedPawnData riderData;
		public bool interrupted = false;

		public override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return WaitForRider();
			yield return DelegateMovement();
		}
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}
		//This method is often responsible for why pawns dismount
		enum DismountReason { False, Interrupted, BadState, LeftMap, NotSpawned, WrongMount, BadJob };
		DismountReason ShouldDismount(ExtendedPawnData riderData)
		{
			if (interrupted || riderData == null || riderData.mount == null) return DismountReason.Interrupted;

			Pawn rider = Rider;
			var riderIsDead = rider.Dead;
			
			if (rider.Downed || riderIsDead || pawn.Downed || pawn.Dead || 
				pawn.HasAttachment(ThingDefOf.Fire) || rider.HasAttachment(ThingDefOf.Fire) || rider.GetPosture() != PawnPosture.Standing ||
				pawn.InMentalState || (rider.InMentalState && rider.MentalState.def != MentalStateDefOf.PanicFlee)
			)
			{
				return DismountReason.BadState;
			}
			if (!rider.Spawned)
			{
				var riderIsColonist = rider.IsColonist;
				if (!riderIsColonist && !riderIsDead)
				{
					pawn.ExitMap(false, CellRect.WholeMap(base.Map).GetClosestEdge(this.pawn.Position));
					return DismountReason.NotSpawned;
				}
				else if (riderIsColonist && rider.GetCaravan() != null)
				{
					pawn.ExitMap(true, CellRect.WholeMap(base.Map).GetClosestEdge(this.pawn.Position));
					return DismountReason.LeftMap;
				}
				else return DismountReason.NotSpawned;
			}

			if (rider.Drafted || !pawn.Faction.def.isPlayer) return DismountReason.False;
			
			var riderJobDef = rider.CurJobDef;
			if (Settings.caravansEnabled)
			{
				var riderMindstateDef = rider.mindState?.duty?.def;
				if (riderMindstateDef == DutyDefOf.TravelOrWait || 
					riderMindstateDef == DutyDefOf.TravelOrLeave || 
					riderMindstateDef == DutyDefOf.PrepareCaravan_GatherAnimals || 
					riderMindstateDef == DutyDefOf.PrepareCaravan_GatherDownedPawns)
				{
					return riderData.reservedMount == pawn ? DismountReason.False : DismountReason.WrongMount;
				}
				
				if (rider.Position.CloseToEdge(rider.Map, 10)) return DismountReason.False; //Caravan just entered map and has not picked a job yet on this tick.
			}

			//If the job is not on the whitelist...
			/*
			if (Settings.rideAndRollEnabled && riderJobDef != null && !allowedJobs.Contains(riderJobDef))
			{
				return DismountReason.BadJob;
			}
			*/
			return DismountReason.False;
		}
		bool CheckReason(DismountReason dismountReason, out DismountReason reason)
		{
			reason = dismountReason;
			return dismountReason != DismountReason.False;
		}
		Toil WaitForRider()
		{
			Toil toil = new Toil();

			Pawn rider = Rider;
			toil.defaultCompleteMode = ToilCompleteMode.Never;
			toil.tickAction = delegate
			{
				if (rider == null || rider.Dead || !rider.Spawned || rider.Downed || rider.InMentalState)
				{
					interrupted = true;
					ReadyForNextToil();
					return;
				}

				riderData = rider.GetGUData();
				if (riderData.mount != null && riderData.mount == pawn)
				{
					ReadyForNextToil();
				}

				var curJobDef = rider.CurJobDef;
				if (curJobDef != ResourceBank.JobDefOf.Mount && 
					curJobDef != JobDefOf.Vomit && 
					curJobDef != JobDefOf.Wait_MaintainPosture && 
					curJobDef != JobDefOf.SocialRelax && 
					curJobDef != JobDefOf.Wait && 
					riderData.mount == null)
				{
					//Log.Message("cancel wait for rider, rider is not mounting, curJob: " + Rider.CurJob.def.defName);                  
					interrupted = true;
					ReadyForNextToil();
				}

			};
			return toil;
		}
		Toil DelegateMovement()
		{
			Toil toil = new Toil();
			toil.defaultCompleteMode = ToilCompleteMode.Never;
			Pawn rider = Rider;

			toil.tickAction = delegate
			{
				riderData = rider.GetGUData();
				if (CheckReason(ShouldDismount(riderData), out DismountReason dismountReason))
				{
					if (Settings.logging) Log.Message("[Giddy-Up] Pawn " + pawn.thingIDNumber + " dismounting for reason: " + dismountReason.ToString());
					ReadyForNextToil();
					return;
				}
				pawn.Drawer.tweener = rider.Drawer.tweener; //Could probably just be set once, but reloading could cause issues?
				pawn.Position = rider.Position;
				pawn.Rotation = rider.Rotation;
				TryAttackEnemy(rider);
			};

			toil.AddFinishAction(delegate
			{
				//Check mount first. If it's null then they must have dismounted outside the driver's control
				if (riderData.mount != null) rider.Dismount(pawn, riderData, false);
			});
			
			return toil;
		}
		void TryAttackEnemy(Pawn rider)
		{
			Thing targetThing = null;
			bool confirmedHostile = false;
			
			//The mount has something targetted but not the rider, so pass the target
			if (rider.TargetCurrentlyAimingAt != null)
			{
				targetThing = rider.TargetCurrentlyAimingAt.Thing;
			}
			//The rider is already trying to attack something
			else if (rider.CurJobDef == JobDefOf.AttackMelee && rider.CurJob.targetA.Thing.HostileTo(rider))
			{
				targetThing = rider.CurJob.targetA.Thing;
				confirmedHostile = true;
			}

			if (targetThing != null && (confirmedHostile || targetThing.HostileTo(rider)))
			{
				var verb = pawn.meleeVerbs?.TryGetMeleeVerb(targetThing);
				if (verb == null || !verb.CanHitTarget(targetThing))
				{
					pawn.TryStartAttack(targetThing); //Try start ranged attack if possible
				}
				else pawn.meleeVerbs.TryMeleeAttack(targetThing);
			}
		}
	}
}