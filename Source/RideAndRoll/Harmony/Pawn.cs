using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
//using Multiplayer.API;

namespace GiddyUpRideAndRoll.Harmony
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    [HarmonyPriority(Priority.First)]
    public class Pawn_GetGizmos
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.rideAndRollEnabled;
        }
        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            foreach (var value in values) yield return value;
            
            if (__instance.jobs != null && __instance.jobs.curJob.def == GiddyUp.ResourceBank.JobDefOf.WaitForRider && __instance.def.race.Animal)
            {
                yield return new Command_Action
                {
                    defaultLabel = "GU_RR_Gizmo_LeaveRider_Label".Translate(),
                    defaultDesc = "GU_RR_Gizmo_LeaveRider_Description".Translate(),
                    icon = ContentFinder<Texture2D>.Get(("UI/" + "LeaveRider"), true),
                    action = () =>
                    {
                        PawnEndCurrentJob(__instance);
                    }
                };
            }
        }
        //[SyncMethod]
        static void PawnEndCurrentJob(Pawn pawn)
        {
            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }
    }
}
