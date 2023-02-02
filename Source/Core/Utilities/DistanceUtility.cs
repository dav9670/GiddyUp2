using GiddyUp.Zones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace GiddyUp.Utilities
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
        public static IntVec3 GetLastTarget(Job job, TargetIndex index)
        {
            if (!job.GetTargetQueue(index).NullOrEmpty<LocalTargetInfo>())
            {
                return job.GetTargetQueue(index)[job.GetTargetQueue(index).Count - 1].Cell;
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

        public static IntVec3 GetClosestAreaLoc(IntVec3 sourceLocation, IntVec3[] cells)
        {
            IntVec3 targetLoc = new IntVec3();
            float minDistance = float.MaxValue;
            for (int i = 0; i < cells.Length; i++)
            {
                var loc = cells[i];
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
    }
}
