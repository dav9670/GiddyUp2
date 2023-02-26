using HarmonyLib;
using RimWorld;
using System;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedAccuracy))]
    static class VerbProperties_AdjustedAccuracy
    {
        static float Postfix(float __result, VerbProperties __instance, Thing equipment)
        {
            var holdingOwner = equipment?.holdingOwner;
            if (holdingOwner == null || holdingOwner.Owner == null || !(holdingOwner.Owner is Pawn_EquipmentTracker eqt))
            {
                return __result;
            }
            Pawn pawn = eqt.pawn;
            if (pawn == null || pawn.stances == null) return __result;
            
            Pawn mount = pawn.GetGUData().mount;
            if (mount == null) return __result;

            float adjustedLevel = 5;
            if (pawn.skills != null)
            {
                adjustedLevel = pawn.skills.GetSkill(SkillDefOf.Animals).levelInt - (int)Math.Round(mount.GetStatValue(StatDefOf.MinimumHandlingSkill, true));
            }
            float animalHandlingOffset = adjustedLevel * Settings.handlingAccuracyImpact;
            float factor = (100f - ((float)Settings.accuracyPenalty - animalHandlingOffset)) / 100f;
            return __result *= factor;
        }
    }
}
