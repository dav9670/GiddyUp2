using GiddyUp.Jobs;
using RimWorld;
using System;
using System.Text;
using UnityEngine;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp
{
    public class StatPart_Riding : StatPart
    {
        public override string ExplanationPart(StatRequest req)
        {
            StringBuilder sb = new StringBuilder();

            if (req.Thing is Pawn pawn)
            {
                if (ExtendedDataStorage.isMounted.Contains(pawn.thingIDNumber))
                {
                    float mountSpeed = pawn.GetGUData().mount.GetStatValue(StatDefOf.MoveSpeed);
                    sb.AppendLine("GUC_StatPart_Mounted".Translate());
                    sb.AppendLine("    " + "GUC_StatPart_MountMoveSpeed".Translate() + ": " + mountSpeed.ToStringByStyle(ToStringStyle.FloatTwo));
                }

                if (pawn.jobs != null && pawn.jobs.curDriver is JobDriver_Mounted jobDriver)
                {
                    sb.AppendLine("GUC_StatPart_Mounted".Translate());
                    float adjustedLevel = 0;
                    if (jobDriver.rider.skills != null)
                    {
                        var skill = jobDriver.rider.skills.GetSkill(SkillDefOf.Animals);
                        adjustedLevel = skill.levelInt - (int)Math.Round(pawn.GetStatValue(StatDefOf.MinimumHandlingSkill, true));
                        float animalHandlingOffset = 1f + (adjustedLevel * ModSettings_GiddyUp.handlingMovementImpact) / 100f;
                        sb.AppendLine("    " + "GUC_StatPart_HandlingMultiplier".Translate() + ": " + animalHandlingOffset.ToStringByStyle(ToStringStyle.PercentOne, ToStringNumberSense.Factor));
                        sb.AppendLine("        " + "GUC_StatPart_HandlingSkill".Translate() + ": " + skill.levelInt);
                        sb.AppendLine("        " + "GUC_StatPart_SkillReq".Translate() + ": " + (int)Math.Round(pawn.GetStatValue(StatDefOf.MinimumHandlingSkill, true)));
                        sb.AppendLine("        " + "GUC_StatPart_LevelsAbove".Translate() + ": " + adjustedLevel);
                        sb.AppendLine("        " + "GUC_StatPart_HandlingMovementImpact".Translate() + ": " + (ModSettings_GiddyUp.handlingMovementImpact / 100f).ToStringByStyle(ToStringStyle.PercentOne));
                    }
                    var modExt = pawn.def.GetModExtension<CustomStats>();
                    if (modExt != null)
                    {
                        sb.AppendLine("    " + "GUC_StatPart_MountTypeMultiplier".Translate() + ": " + modExt.speedModifier.ToStringByStyle(ToStringStyle.PercentOne, ToStringNumberSense.Factor));
                    }
                }
            }
            return sb.ToString();
        }
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.Thing is Pawn pawn)
            {
                if (ExtendedDataStorage.isMounted.Contains(pawn.thingIDNumber))
                {
                    val = pawn.GetGUData().mount.GetStatValue(StatDefOf.MoveSpeed);
                }
                else if (pawn.jobs != null && pawn.jobs.curDriver is JobDriver_Mounted jdMounted)
                {
                    val = GetRidingSpeed(val, pawn, jdMounted.rider.skills);
                }
            }
            return;
        }
        public static float GetRidingSpeed(float baseValue, Pawn animal, Pawn_SkillTracker skills)
        {
            float adjustedLevel = 0;
            if (skills != null)
            {
                var skill = skills.GetSkill(SkillDefOf.Animals);
                adjustedLevel = skill.levelInt - (int)Math.Round(animal.GetStatValue(StatDefOf.MinimumHandlingSkill, true));
            }
            float animalHandlingOffset = 1f + (adjustedLevel * Settings.handlingMovementImpact) / 100f;
            baseValue *= animalHandlingOffset;
            var modExt = animal.def.GetModExtension<CustomStats>();
            if (modExt != null)
            {
                float customSpeedModifier = modExt.speedModifier;
                baseValue *= customSpeedModifier;
            }
            return baseValue;
        }
    }

    class StatPart_Armor : StatPart
    {
        public override string ExplanationPart(StatRequest req)
        {
            StringBuilder sb = new StringBuilder();
            if (req.Thing is Pawn pawn && pawn.jobs != null && pawn.jobs.curDriver is JobDriver_Mounted)
            {
                var modExt = pawn.def.GetModExtension<CustomStats>();
                if (modExt != null && modExt.armorModifier != 1.0f)
                {
                    sb.AppendLine("GUC_StatPart_Mounted".Translate());
                    sb.AppendLine("    " + "GUC_StatPart_MountTypeMultiplier".Translate() + ": " + (modExt.armorModifier).ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Factor));
                }  
            }
            return sb.ToString();
        }
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.Thing is Pawn pawn && pawn.jobs != null && pawn.jobs.curDriver is JobDriver_Mounted)
            {
                var modExt = pawn.def.GetModExtension<CustomStats>();
                if (modExt != null && modExt.armorModifier != 1.0f)
                {
                    val *= modExt.armorModifier;
                }
            }
        }
    }
}