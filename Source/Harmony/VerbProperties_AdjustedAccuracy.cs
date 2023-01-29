using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace GiddyUp.HarmonyPlaceholder
{
    [HarmonyPatch(typeof(VerbProperties), nameof(VerbProperties.AdjustedAccuracy))]
    static class VerbProperties_AdjustedAccuracy
    {
        static float Postfix(float __result, VerbProperties __instance, ref Thing equipment)
        {
            var holdingOwner = equipment.holdingOwner;
            if (equipment == null || holdingOwner == null || holdingOwner.Owner == null || !(holdingOwner.Owner is Pawn_EquipmentTracker eqt))
            {
                return __result;
            }
            Pawn pawn = eqt.pawn;
            if (pawn == null || pawn.stances == null) return __result;
            Pawn mount = Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber).mount;
            if (mount == null)
            {
                return __result;
            }
            float adjustedLevel = 5;
            if(pawn.skills != null && pawn.skills.GetSkill(SkillDefOf.Animals) is SkillRecord record)
            {
                adjustedLevel = record.levelInt - (int)Math.Round(mount.GetStatValue(StatDefOf.MinimumHandlingSkill, true));
            }
            float animalHandlingOffset = adjustedLevel * ModSettings_GiddyUp.handlingAccuracyImpact;
            float factor = (100f - ((float)ModSettings_GiddyUp.accuracyPenalty - animalHandlingOffset)) / 100f;
            return __result *= factor;
        }
    }
}
