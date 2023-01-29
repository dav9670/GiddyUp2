using GiddyUp.Utilities;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace GiddyUp.HarmonyPlaceholder
{
    [HarmonyPatch(typeof(WorkGiver_TakeToPen), nameof(WorkGiver_TakeToPen.JobOnThing))]
    class WorkGiver_TakeToPen_JobOnThing
    {
        static bool Prefix(WorkGiver_Train __instance, Pawn pawn, Thing t, ref Job __result)
        {
            if (t is Pawn animal && animal.def.race.Animal && IsMountableUtility.IsCurrentlyMounted(animal))
            {        
                __result = null;
                return false;
            }
            
            return true;
        }
    }
}