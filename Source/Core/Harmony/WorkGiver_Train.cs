using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(WorkGiver_Train), nameof(WorkGiver_Train.JobOnThing))]
    class WorkGiver_Train_JobOnThing
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
