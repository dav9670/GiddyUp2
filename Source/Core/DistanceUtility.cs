using RimWorld;
using Verse;
using Verse.AI;

namespace GiddyUp
{
    public static class DistanceUtility
    {
        public static IntVec3 GetFirstTarget(Job job, TargetIndex index)
        {
            if (!job.GetTargetQueue(index).NullOrEmpty<LocalTargetInfo>())
            {
                return job.GetTargetQueue(index)[0].Cell;
            }
            return job.GetTarget(index).Cell;
        }
        public static IntVec3 GetClosestAreaLoc(IntVec3 sourceLocation, Area area)
        {
            IntVec3 targetLoc = new IntVec3();
            float minDistance = float.MaxValue;
            foreach (IntVec3 loc in area.ActiveCells)
            {
                float distance = loc.DistanceTo(sourceLocation);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    targetLoc = loc;
                }
            }
            return targetLoc;
        }
        public static IntVec3 GetClosestAreaLoc(Pawn pawn, Area areaFound)
        {
            return GetClosestAreaLoc(pawn.Position, areaFound);
        }
        public static IntVec3 GetClosestPen(ref float workingNum, Map map, Pawn animal, Pawn rider, IntVec3 firstTarget, IntVec3 secondTarget)
        {
            IntVec3 cell = IntVec3.Invalid;
            foreach (var pen in map.listerBuildings.allBuildingsAnimalPenMarkers)
            {
                var penMarker = pen.TryGetComp<CompAnimalPenMarker>();
                if (penMarker == null || !penMarker.AcceptsToPen(animal) || penMarker.parent.IsForbidden(rider)) continue;
                if (!animal.Map.reachability.CanReach(secondTarget, penMarker.parent, PathEndMode.Touch, TraverseParms.For(rider).WithFenceblockedOf(animal) )) continue;
                
                float tmp = firstTarget.DistanceTo(penMarker.parent.Position) + penMarker.parent.Position.DistanceTo(secondTarget);
                if (tmp < workingNum)
                {
                    workingNum = tmp;
                    cell = penMarker.parent.Position;
                }
            }
            return cell;
        }
        public static IntVec3 GetClosestDropoffPoint(ref float workingNum, IntVec3[] areas, IntVec3 animalPos, IntVec3 target)
        {
            IntVec3 dropOffCell = IntVec3.Invalid;
            for (int j = 0; j < areas.Length; j++)
            {
                var cell = areas[j];
                float tmp = animalPos.DistanceTo(cell) + cell.DistanceTo(target);
                if (tmp < workingNum)
                {
                    workingNum = tmp;
                    dropOffCell = cell;
                }
            }
            return dropOffCell;
        }
    }
}
