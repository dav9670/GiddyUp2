using GiddyUp.Stats;
using GiddyUp.Storage;
using GiddyUpRideAndRoll.Jobs;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUpRideAndRoll.Harmony
{
    /// <summary>
    /// Set speed of mount so it always matches the speed of the pawn the animal is waiting for. 
    /// </summary>
    [HarmonyPatch(typeof(StatPart_Riding), nameof(StatPart_Riding.TransformValue))]
    class StatPart_Riding_TransformValue
    {
        static float PostFix(float __result, StatRequest req)
        {
            if (req.thingInt is Pawn pawn)
            {
                if (pawn.CurJob != null && pawn.jobs.curDriver is JobDriver_WaitForRider jobDriver)
                {
                    return jobDriver.Followee.GetStatValue(StatDefOf.MoveSpeed);
                }
            }
            return __result;
        }
    }
}
