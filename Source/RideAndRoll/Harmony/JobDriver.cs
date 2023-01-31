using GiddyUp.Storage;
using GiddyUp.Utilities;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
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
        static void Postfix(JobDriver __instance, List<Toil> ___toils)
        {            
            Pawn pawn = __instance.pawn;
            if (pawn.Map == null) return;
            if (pawn.Faction != Faction.OfPlayer || pawn.Drafted) return;
            if (!GiddyUp.Setup.isMounted.Contains(__instance.pawn.thingIDNumber)) return;

            ExtendedDataStorage store = GiddyUp.Setup._extendedDataStorage;
            ExtendedPawnData pawnData = store.GetExtendedDataFor(pawn.thingIDNumber);

            GiddyUp.Zones.Area_GU.GetGUAreasFast(pawn.Map, out Area areaNoMount, out Area areaDropAnimal);
            bool startedPark = false;
            IntVec3 originalLoc = new IntVec3();
            IntVec3 parkLoc = new IntVec3();

            if (areaNoMount != null)
            {
                foreach (Toil toil in ___toils)
                {
                    //checkedToil makes sure the ActiveCells.Contains is only called once, preventing performance impact. 
                    toil.AddPreTickAction(delegate
                    {
                        if (!startedPark && pawnData.mount != null && pawn.IsHashIntervalTick(60) && pawn.CurJobDef != JobDefOf.RopeToPen && areaNoMount.ActiveCells.Contains(toil.actor.pather.Destination.Cell))
                        {
                            originalLoc = toil.actor.pather.Destination.Cell;
                            if (AnimalPenUtility.NeedsToBeManagedByRope(pawnData.mount) || areaDropAnimal == null)
                            {
                                startedPark = TryParkAnimalPen(__instance, pawnData, ref parkLoc, toil);
                            }
                            else
                            {
                                startedPark = TryParkAnimalDropSpot(areaDropAnimal, ref parkLoc, toil);
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
        }
        static bool TryParkAnimalPen(JobDriver __instance, ExtendedPawnData pawnData, ref IntVec3 parkLoc, Toil toil)
        {
            bool succeeded = false;
            var pen = AnimalPenUtility.GetPenAnimalShouldBeTakenTo(__instance.pawn, pawnData.mount, out string failReason, true, true, false, true);
            if (pen != null)
            {
                parkLoc = AnimalPenUtility.FindPlaceInPenToStand(pen, __instance.pawn);

                if (toil.actor.Map.reachability.CanReach(toil.actor.Position, parkLoc, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
                {
                    toil.actor.pather.StartPath(parkLoc, PathEndMode.OnCell);
                    succeeded = true;
                }
            }
            else
            {
                //Log.Message(pawnData.mount.Name + " failed: " + failReason);
            }
            return succeeded;
        }
        static bool TryParkAnimalDropSpot(Area areaDropAnimal, ref IntVec3 parkLoc, Toil toil)
        {
            
            bool succeeded = false;
            parkLoc = DistanceUtility.GetClosestAreaLoc(toil.actor.pather.Destination.Cell, areaDropAnimal);
            if (toil.actor.Map.reachability.CanReach(toil.actor.Position, parkLoc, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
            {
                toil.actor.pather.StartPath(parkLoc, PathEndMode.OnCell);
                succeeded = true;
            }
            else
            {
                Messages.Message("GU_RR_NotReachable_DropAnimal_Message".Translate(), new RimWorld.Planet.GlobalTargetInfo(parkLoc, toil.actor.Map), MessageTypeDefOf.NegativeEvent);
            }
            return succeeded;
        }
    }
}
