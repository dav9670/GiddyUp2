﻿using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(WorkGiver_TakeToPen), nameof(WorkGiver_TakeToPen.JobOnThing))]
    class Patch_TakeToPen
    {
        static bool Prefix(Thing t, Job __result)
        {
            if (t is Pawn animal && ExtendedDataStorage.GUComp[animal.thingIDNumber].reservedBy != null)
            {
                __result = null;
                return false;
            }
            
            return true;
        }
    }
}