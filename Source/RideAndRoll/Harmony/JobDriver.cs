using GiddyUp.Storage;
using GiddyUp.Utilities;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace GiddyUpRideAndRoll.Harmony
{
    [HarmonyPatch(typeof(JobDriver), nameof(JobDriver.SetupToils))]
    class JobDriver_SetupToils
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.rideAndRollEnabled;
        }
        static void Postfix(JobDriver __instance)
        {
            List<Toil> toils = __instance.toils;
            Pawn pawn = __instance.pawn;
            Map map = pawn.Map;
            if (map == null) return;
            if (pawn.Faction != Current.gameInt.worldInt.factionManager.ofPlayer || pawn.Drafted) return;
            if (!GiddyUp.Setup.isMounted.Contains(__instance.pawn.thingIDNumber)) return;

            ExtendedDataStorage store = GiddyUp.Setup._extendedDataStorage;
            ExtendedPawnData pawnData = store.GetExtendedDataFor(pawn.thingIDNumber);

            GiddyUp.Zones.Area_GU.GetGUAreasFast(map, out Area areaNoMount, out Area areaDropAnimal);
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
                    //checkedToil = true;
                    if (startedPark && toil.actor.pather.nextCell == parkLoc)
                    {
                        pawnData.Mount = null;
                        toil.actor.pather.StartPath(originalLoc, PathEndMode.OnCell);
                        if (pawnData.owning != null)
                        {
                            ExtendedPawnData animalData = store.GetExtendedDataFor(pawnData.owning.thingIDNumber);
                            animalData.ownedBy = null;
                            pawnData.owning = null;
                        }
                    }
                });
            }
        }
        static bool TryParkAnimalPen(Pawn roper, Pawn mount, ref IntVec3 parkLoc, Pawn actor)
        {
            bool succeeded = false;
            var pen = AnimalPenUtility.GetPenAnimalShouldBeTakenTo(roper, mount, out string failReason, true, true, false, true);
            if (pen != null)
            {
                parkLoc = AnimalPenUtility.FindPlaceInPenToStand(pen, roper);

                if (actor.Map.reachability.CanReach(actor.Position, parkLoc, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
                {
                    actor.pather.StartPath(parkLoc, PathEndMode.OnCell);
                    succeeded = true;
                }
            }
            return succeeded;
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
                Messages.Message("GU_RR_NotReachable_DropAnimal_Message".Translate(), new RimWorld.Planet.GlobalTargetInfo(parkLoc, actor.Map), MessageTypeDefOf.NegativeEvent);
            }
            return succeeded;
        }
    }
}