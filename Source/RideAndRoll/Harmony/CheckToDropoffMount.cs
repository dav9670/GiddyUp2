using GiddyUp;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

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
			if (map == null || pawn.Faction != Current.gameInt.worldInt.factionManager.ofPlayer || pawn.Drafted || !ExtendedDataStorage.isMounted.Contains(__instance.pawn.thingIDNumber) || 
				!GiddyUp.Area_GU.GetGUAreasFast(map, out Area areaNoMount, out Area areaDropAnimal)) 
			{
				return;
			}

			bool isMovingToDismount = false;
			IntVec3 originalLoc = new IntVec3();
			IntVec3 parkLoc = new IntVec3();
			ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
			
			List<Toil> toils = __instance.toils;
			var length = toils.Count;
			for (int i = 0; i < length; i++)
			{
				Toil toil = toils[i];
				
				//checkedToil makes sure the ActiveCells.Contains is only called once, preventing performance impact. 
				toil.AddPreTickAction(delegate
				{
					var pather = toil.actor.pather;
					if (!isMovingToDismount && pawnData.mount != null && pawn.IsHashIntervalTick(60) && areaNoMount.innerGrid[map.cellIndices.CellToIndex(pather.Destination.Cell)] &&
						(pawn.roping == null || !pawn.roping.IsRopingOthers))
					{
						originalLoc = pather.Destination.Cell;
						if (areaDropAnimal == null) isMovingToDismount = TryParkAnimalPen(pawn, pawnData.mount, ref parkLoc);
						else isMovingToDismount = TryParkAnimalDropSpot(areaDropAnimal, ref parkLoc, pawn);
					}
					//Pawn has taken animal to dropoff point, remove association
					if (isMovingToDismount && pather.nextCell == parkLoc && pawnData.mount != null)
					{
						var animal = pawnData.mount;
						pawn.Dismount(animal, pawnData);

						//Check if the animal should be hitched
						if (AnimalPenUtility.NeedsToBeManagedByRope(animal))
						{
							if (animal.roping == null) animal.roping = new Pawn_RopeTracker(pawn); //Not needed, but changes to modded animals could maybe cause issues
							animal.roping.RopeToSpot(parkLoc);
						}
						pather.StartPath(originalLoc, PathEndMode.OnCell);
					}
				});
			}
		}
		static bool TryParkAnimalPen(Pawn roper, Pawn mount, ref IntVec3 parkLoc, bool simulateOnly = false)
		{
			var pen = AnimalPenUtility.GetPenAnimalShouldBeTakenTo(roper, mount, out string failReason, true, true, false, true);
			if (pen != null)
			{
				parkLoc = AnimalPenUtility.FindPlaceInPenToStand(pen, roper);

				if (roper.Map.reachability.CanReach(roper.Position, parkLoc, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
				{
					if (!simulateOnly) roper.pather.StartPath(parkLoc, PathEndMode.OnCell);
					return true;
				}
			}
			return false;
		}
		static bool TryParkAnimalDropSpot(Area areaDropAnimal, ref IntVec3 parkLoc, Pawn actor)
		{
			bool succeeded = false;
			parkLoc = DistanceUtility.GetClosestAreaLoc(actor.pather.Destination.Cell, areaDropAnimal);
			if (actor.Map.reachability.CanReach(actor.Position, parkLoc, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
			{
				actor.pather.StartPath(parkLoc, PathEndMode.OnCell);
				succeeded = true;
			}
			else
			{
				Messages.Message("GU_RR_NotReachable_DropAnimal_Message".Translate(parkLoc.ToString()), new RimWorld.Planet.GlobalTargetInfo(parkLoc, actor.Map), MessageTypeDefOf.NegativeEvent);
			}
			return succeeded;
		}
	}
}