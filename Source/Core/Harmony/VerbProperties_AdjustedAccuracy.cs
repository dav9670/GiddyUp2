using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp.Harmony;

[HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedAccuracy))]
internal static class VerbProperties_AdjustedAccuracy
{
    private static float Postfix(float __result, VerbProperties __instance, Thing equipment)
    {
        var holdingOwner = equipment?.holdingOwner;
        if (holdingOwner == null || holdingOwner.Owner == null || !(holdingOwner.Owner is Pawn_EquipmentTracker eqt))
            return __result;
        var pawn = eqt.pawn;
        if (pawn == null || pawn.stances == null)
            return __result;

        var mount = pawn.GetExtendedPawnData().Mount;
        if (mount == null)
            return __result;

        float adjustedLevel = 5;
        if (pawn.skills != null)
            adjustedLevel = pawn.skills.GetSkill(SkillDefOf.Animals).Level -
                            (int)Math.Round(mount.GetStatValue(StatDefOf.MinimumHandlingSkill, true));
        var animalHandlingOffset = adjustedLevel * Settings.handlingAccuracyImpact;
        var factor = (100f - ((float)Settings.accuracyPenalty - animalHandlingOffset)) / 100f;
        return __result *= factor;
    }
}