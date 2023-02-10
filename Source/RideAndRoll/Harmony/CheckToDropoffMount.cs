using GiddyUp;
using HarmonyLib;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using System;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpRideAndRoll.Harmony
{
	//When a job is put together it is made of up many components - toils. This patch will inject a pre-instruction to each toil
	//component to see if their destination cell does not allow animals, and to find a suitable place to dismoun
	[HarmonyPatch(typeof(JobDriver), nameof(JobDriver.SetupToils))]
	class Patch_CheckToDropoffMount
	{
		static bool Prepare()
		{
			return GiddyUp.ModSettings_GiddyUp.rideAndRollEnabled;
		}
		static void Postfix(JobDriver __instance)
		{
			Pawn pawn = __instance.pawn;
			Map map = pawn.Map;
			//TODO maybe move the JobDefOf.PrepareCaravan_CollectAnimals part to a blacklist hashset
			if (map == null || pawn.Faction != Current.gameInt.worldInt.factionManager.ofPlayer || pawn.Drafted || !__instance.pawn.IsMounted() || 
				!Area_GU.GetGUAreasFast(map, out Area areaNoMount, out Area areaDropAnimal)) 
			{
				return;
			}

			bool needsToDismount = false;
			IntVec3 originalLoc = new IntVec3();
			IntVec3 parkLoc = new IntVec3();
			ExtendedPawnData pawnData = pawn.GetGUData();
			
			List<Toil> toils = __instance.toils;
			var length = toils.Count;
			for (int i = 0; i < length; i++)
			{
				Toil toil = toils[i];
				
				//TODO: This all needs to be refactored into the GoDismount() method
				toil.AddPreTickAction(delegate
				{
					var pather = toil.actor.pather;
					if (!needsToDismount && pawnData.mount != null && //Not already parking?
						pawn.IsHashIntervalTick(60) && //Tick this?
						((__instance.job != null && !GiddyUp.Jobs.JobDriver_Mounted.allowedJobs.Contains(__instance.job.def)) || //Not a job that can be done mounted?
						areaNoMount.innerGrid[map.cellIndices.CellToIndex(pather.Destination.Cell)]) && //OR heading towards a forbidden cell?
						(pawn.roping == null || !pawn.roping.IsRopingOthers)) //And nope roping? TODO: this can probably be part of the job check
					{
						originalLoc = pather.Destination.Cell;
						needsToDismount = true;

						if (areaDropAnimal == null) TryParkAnimalPen(pawn, pawnData.mount, ref parkLoc);
						else TryParkAnimalDropSpot(areaDropAnimal, ref parkLoc, pawn);

						//Dropoff is too far away, setup a hitching point instead
						if (parkLoc.DistanceTo(originalLoc) > Settings.autoHitchDistance)
						{
							Predicate<IntVec3> freeCell = delegate(IntVec3 cell)
							{
								return (cell.Standable(map) && 
									cell.GetDangerFor(pawnData.mount, map) == Danger.None && 
									!cell.Fogged(map) &&
									cell.InBounds(map) &&
									pawn.CanReserveAndReach(cell, PathEndMode.OnCell, Danger.None, 1, -1, null, false));
							};
							CellFinder.TryFindRandomCellNear(originalLoc, map, 4, freeCell, out parkLoc, 16);
						}
						//Validate results
						if (originalLoc == parkLoc || parkLoc == IntVec3.Invalid)
						{
							Log.Message("[Giddy-Up] Pawn " + pawn.Name.ToString() + " could not ride their mount to their job but could not find any places to dismount. Immediately dismounting.");
							pawn.Dismount(pawnData.mount, pawnData, false, parkLoc);
						}
						//Looks good, begin pathing
						else pawn.pather.StartPath(parkLoc, PathEndMode.OnCell);
					}
					//Pawn has taken animal to dropoff point, remove association
					if (needsToDismount && pather.nextCell == parkLoc && pawnData.mount != null)
					{
						pawn.Dismount(pawnData.mount, pawnData, false, parkLoc);
						pather.StartPath(originalLoc, PathEndMode.OnCell); //Resume original work
					}
				});
			}
		}
		static void TryParkAnimalPen(Pawn roper, Pawn mount, ref IntVec3 parkLoc)
		{
			var pen = AnimalPenUtility.GetPenAnimalShouldBeTakenTo(roper, mount, out string failReason, true, true, false, true);
			if (pen != null)
			{
				parkLoc = AnimalPenUtility.FindPlaceInPenToStand(pen, roper);

				if (!roper.Map.reachability.CanReach(roper.Position, parkLoc, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
				{
					parkLoc = IntVec3.Invalid;
				}
			}
		}
		static void TryParkAnimalDropSpot(Area areaDropAnimal, ref IntVec3 parkLoc, Pawn actor)
		{
			parkLoc = areaDropAnimal.GetClosestAreaLoc(actor.pather.Destination.Cell);
			if (!actor.Map.reachability.CanReach(actor.Position, parkLoc, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
			{
				parkLoc = IntVec3.Invalid;
			}
		}
	}
}