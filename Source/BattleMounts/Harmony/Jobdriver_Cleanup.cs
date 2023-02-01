/*
using GiddyUp.Jobs;
using GiddyUp.Storage;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace BattleMounts.Harmony
{
    [HarmonyPatch(typeof(JobDriver), nameof(JobDriver.Cleanup))]
    static class JobDriver_Cleanup
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.battleMountsEnabled;
        }
        static void Prefix(JobDriver __instance)
        {
            if(__instance.job.def != GiddyUp.ResourceBank.JobDefOf.Mounted) return;

            JobDriver_Mounted jobDriver = (JobDriver_Mounted)__instance;
            ExtendedPawnData riderData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(jobDriver.Rider.thingIDNumber);

            riderData.Reset();
            jobDriver.pawn.Drawer.tweener = new PawnTweener(jobDriver.pawn);
        }
    }
}
*/