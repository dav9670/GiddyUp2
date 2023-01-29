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
    [HarmonyPatch(typeof(StatPart_Riding), "TransformValue")]
    class StatPart_Riding_TransformValue
    {
        static void PostFix(StatRequest req, ref float __result)
        {
            if (req.Thing is Pawn pawn)
            {
                if (pawn.CurJob != null && pawn.jobs.curDriver is JobDriver_WaitForRider jobDriver)
                {
                    __result = jobDriver.Followee.GetStatValue(StatDefOf.MoveSpeed);
                }
            }
        }
    }
}
