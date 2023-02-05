//I'm not sure what the logic is behind this patch. Disabling for now.
/*
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(Building_Door), nameof(Building_Door.BlockedOpenMomentary), MethodType.Getter)]
    static class Patch_BlockedOpenMomentary
    {
        static bool Postfix(bool __result, Building_Door __instance)
        {
            if (!__result) return __result;
            
            //if true, check for false positives
            List<Thing> thingList =  __instance.Map.thingGrid.ThingsListAt(__instance.Position);
            var length = thingList.Count;
            for (int i = 0; i < length; i++)
            {
                Thing thing = thingList[i];

                if (thing.def.category == ThingCategory.Item) return true;
                else if(thing.def.category == ThingCategory.Pawn) //ignore mounted animals when determining whether door is blocked
                {
                    Pawn pawn = thing as Pawn;
                    //dont return, blocking things can still be found
                    return !(pawn.CurJob != null && pawn.CurJob.def == ResourceBank.JobDefOf.Mounted);
                }
            }
            return __result;
        }
    }
}
*/