using GiddyUp.Utilities;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(WorkGiver_TakeToPen), nameof(WorkGiver_TakeToPen.JobOnThing))]
    class WorkGiver_TakeToPen_JobOnThing
    {
        static bool Prefix(Thing t, Job __result)
        {
            if (t is Pawn animal && animal.def.race.Animal && (IsMountableUtility.IsCurrentlyMounted(animal) || animal.CurJobDef == GiddyUp.ResourceBank.JobDefOf.WaitForRider))
            {
                __result = null;
                return false;
            }
            
            return true;
        }
    }
}