using HarmonyLib;
using RimWorld;
//using Multiplayer.API;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace GiddyUp.Harmony;

//This patches the release animals gizmo so that it won't throw the rider off
[HarmonyPatch(typeof(Pawn_PlayerSettings), nameof(Pawn_PlayerSettings.GetGizmos))]
internal static class Patch_PawnGetGizmos
{
    private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn_PlayerSettings __instance)
    {
        foreach (var item in values)
        {
            if (item is Command_Toggle toggle && toggle.defaultDesc == "CommandReleaseAnimalsDesc".Translate())
                toggle.toggleAction = () => UpdateAnimalRelease(__instance.pawn, ref __instance.animalsReleased);
            yield return item;
        }
    }

    //[SyncMethod]
    private static void UpdateAnimalRelease(Pawn pawn, ref bool animalsReleased)
    {
        animalsReleased = !animalsReleased;
        if (animalsReleased)
            foreach (var current in PawnUtility.SpawnedMasteredPawns(pawn))
            {
                if (current.caller != null)
                    current.caller.Notify_Released();

                if (current.CurJobDef != ResourceBank.JobDefOf.Mounted)
                    current.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            }
    }
}