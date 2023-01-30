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
        public static LocalTargetInfo GetFirstTarget(Job job, TargetIndex index)
        {
            if (!job.GetTargetQueue(index).NullOrEmpty<LocalTargetInfo>())
            {
                return job.GetTargetQueue(index)[0];
            }
            return job.GetTarget(index);
        }
        public static LocalTargetInfo GetLastTarget(Job job, TargetIndex index)
        {
            if (!job.GetTargetQueue(index).NullOrEmpty<LocalTargetInfo>())
            {
                return job.GetTargetQueue(index)[job.GetTargetQueue(index).Count - 1];
            }
            return job.GetTarget(index);
        }
        public static IntVec3 getClosestAreaLoc(IntVec3 sourceLocation, Area areaFound)
        {
            IntVec3 targetLoc = new IntVec3();
            double minDistance = double.MaxValue;
            foreach (IntVec3 loc in areaFound.ActiveCells)
            {
                double distance = loc.DistanceTo(sourceLocation);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    targetLoc = loc;
                }
            }
            return targetLoc;
        }

        public static IntVec3 getClosestAreaLoc(Pawn pawn, Area_GU areaFound)
        {
            return getClosestAreaLoc(pawn.Position, areaFound);
        }
    }
}
