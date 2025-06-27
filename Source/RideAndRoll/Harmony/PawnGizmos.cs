using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Settings = GiddyUp.ModSettings_GiddyUp;

//using Multiplayer.API;

namespace GiddyUpRideAndRoll.Harmony;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
public class Pawn_GetGizmos
{
    private static bool Prepare()
    {
        return Settings.rideAndRollEnabled;
    }

    private static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
    {
        foreach (var value in values)
            yield return value;

        if (__instance.CurJobDef == GiddyUp.ResourceBank.JobDefOf.WaitForRider)
            yield return new Command_Action
            {
                defaultLabel = "GU_RR_Gizmo_LeaveRider_Label".Translate(),
                defaultDesc = "GU_RR_Gizmo_LeaveRider_Description".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/" + "LeaveRider", true),
                action = () => PawnEndCurrentJob(__instance)
            };
    }

    //[SyncMethod]
    private static void PawnEndCurrentJob(Pawn pawn)
    {
        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }
}