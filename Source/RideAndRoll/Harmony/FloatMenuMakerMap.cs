using GiddyUp;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GiddyUpRideAndRoll.Harmony
{
    [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.ChoicesAtFor))]
    static class FloatMenuMakerMap_ChoicesAtFor
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.rideAndRollEnabled;
        }
        static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> __result)
        {
            foreach (LocalTargetInfo current in GenUI.TargetsAt(clickPos, TargetingParameters.ForAttackHostile(), true))
            {
                if ((current.Thing is Pawn target) && !pawn.Drafted && target.RaceProps.Animal)
                {
                    GUC_FloatMenuUtility.AddMountingOptions(target, pawn, __result);
                }
            }
        }
    }
}
