using HarmonyLib;
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
            if (t is Pawn animal && (animal.IsMountedAnimal() || animal.CurJobDef == ResourceBank.JobDefOf.WaitForRider))
            {
                __result = null;
                return false;
            }            
            return true;
        }
    }
}