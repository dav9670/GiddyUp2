using HarmonyLib;
using RimWorld;
//using Multiplayer.API;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(Pawn_PlayerSettings), nameof(Pawn_PlayerSettings.GetGizmos))]
    static class Pawn_PlayerSettings_GetGizmos
    {
        //purpose: Make sure animals don't throw of their rider when released. 
        static IEnumerable<Gizmo> PostFix(IEnumerable<Gizmo> values, Pawn_PlayerSettings __instance)
        {
            foreach (var item in values)
            {
                if (item is Command_Toggle toggle)
                {
                    toggle.toggleAction = () => UpdateAnimalRelease(__instance.pawn, ref __instance.animalsReleased);
                }
                yield return item;
            }
        }

        //[SyncMethod]
        static void UpdateAnimalRelease(Pawn pawn, ref bool animalsReleased)
        {
            animalsReleased = !animalsReleased;
            if (animalsReleased)
            {
                foreach (Pawn current in PawnUtility.SpawnedMasteredPawns(pawn))
                {
                    if (current.caller != null) current.caller.Notify_Released();

                    if (current.CurJobDef != ResourceBank.JobDefOf.Mounted) current.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                }
            }
        }
    }
}
