using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using GiddyUp;

namespace BattleMounts.Harmony
{
    [HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.AddDraftedOrders), new Type[] { typeof(Vector3), typeof(Pawn), typeof(List<FloatMenuOption>), typeof(bool) })]
    static class Patch_AddDraftedOrders
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.battleMountsEnabled;
        }
        static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            foreach (LocalTargetInfo current in GenUI.TargetsAt(clickPos, TargetingParameters.ForAttackHostile(), true))
            {
                if (current.Thing is Pawn target && target.RaceProps.Animal) GUC_FloatMenuUtility.AddMountingOptions(target, pawn, opts);
            }
        }
    }
}
