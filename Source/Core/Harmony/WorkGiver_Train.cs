using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace GiddyUp.Harmony;

//TODO: could transpile this...
[HarmonyPatch(typeof(WorkGiver_Train), nameof(WorkGiver_Train.JobOnThing))]
internal class WorkGiver_Train_JobOnThing
{
    private static bool Prefix(Thing t, Job __result)
    {
        if (t is Pawn animal && (animal.IsMountedAnimal() || animal.CurJobDef == ResourceBank.JobDefOf.WaitForRider))
        {
            __result = null;
            return false;
        }

        return true;
    }
}