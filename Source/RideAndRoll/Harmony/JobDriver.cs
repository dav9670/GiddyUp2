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
	class JobDriver_SetupToils
	{
		static bool Prepare()
		{
			return GiddyUp.ModSettings_GiddyUp.rideAndRollEnabled;
		}
		static void Postfix(JobDriver __instance)
		{
			Pawn pawn = __instance.pawn;
			Map map = pawn.Map;
			if (map == null || 
				pawn.Faction != Current.gameInt.worldInt.factionManager.ofPlayer || 
				pawn.Drafted || 
				!ExtendedDataStorage.isMounted.Contains(__instance.pawn.thingIDNumber)) 
			{
				return;
			}

			List<Toil> toils = __instance.toils;
			ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];

			GiddyUp.Area_GU.GetGUAreasFast(map, out Area areaNoMount, out Area areaDropAnimal);
			if (areaNoMount == null) return;
			bool startedPark = false;
			IntVec3 originalLoc = new IntVec3();
			IntVec3 parkLoc = new IntVec3();
			
			var length = toils.Count;
			for (int i = 0; i < length; i++)
			{
				Toil toil = toils[i];
				
				//checkedToil makes sure the ActiveCells.Contains is only called once, preventing performance impact. 
				toil.AddPreTickAction(delegate
				{
					if (!startedPark && pawnData.mount != null && pawn.IsHashIntervalTick(60) && pawn.CurJobDef != JobDefOf.RopeToPen && 
						areaNoMount.innerGrid[map.cellIndices.CellToIndex(toil.actor.pather.Destination.Cell)])
					{
						originalLoc = toil.actor.pather.Destination.Cell;
						if (AnimalPenUtility.NeedsToBeManagedByRope(pawnData.mount) || areaDropAnimal == null)
						{
							startedPark = TryParkAnimalPen(__instance.pawn, pawnData.mount, ref parkLoc, toil.actor);
						}
						else
						{
							startedPark = TryParkAnimalDropSpot(areaDropAnimal, ref parkLoc, toil.actor);
						}
					}
					//Pawn has taken animal to dropoff point, remove association
					if (startedPark && toil.actor.pather.nextCell == parkLoc)
					{
						pawnData.Mount = null;
						toil.actor.pather.StartPath(originalLoc, PathEndMode.OnCell);
						if (pawnData.reservedMount != null)
						{
							ExtendedPawnData animalData = ExtendedDataStorage.GUComp[pawnData.reservedMount.thingIDNumber];
							animalData.reservedBy = null;
							pawnData.ReserveMount = null;
						}
					}
				});
			}
		}
		static bool TryParkAnimalPen(Pawn roper, Pawn mount, ref IntVec3 parkLoc, Pawn actor, bool simulateOnly = false)
		{
			var pen = AnimalPenUtility.GetPenAnimalShouldBeTakenTo(roper, mount, out string failReason, true, true, false, true);
			if (pen != null)
			{
				parkLoc = AnimalPenUtility.FindPlaceInPenToStand(pen, roper);

				if (actor.Map.reachability.CanReach(actor.Position, parkLoc, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
				{
					if (!simulateOnly) actor.pather.StartPath(parkLoc, PathEndMode.OnCell);
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