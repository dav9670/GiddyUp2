using GiddyUp.Jobs;
using RimWorld;
using System;
using System.Text;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp;

public class StatPart_Riding : StatPart
{
    public override string ExplanationPart(StatRequest req)
    {
        var sb = new StringBuilder();

        if (req.Thing is Pawn pawn)
        {
            if (ExtendedDataStorage.isMounted.Contains(pawn.thingIDNumber))
            {
                var mountSpeed = pawn.GetGUData().mount.GetStatValue(StatDefOf.MoveSpeed);
                sb.AppendLine("GUC_StatPart_Mounted".Translate());
                sb.AppendLine("    " + "GUC_StatPart_MountMoveSpeed".Translate() + ": " +
                              mountSpeed.ToStringByStyle(ToStringStyle.FloatTwo));
            }

            if (pawn.jobs != null && pawn.jobs.curDriver is JobDriver_Mounted jobDriver)
            {
                sb.AppendLine("GUC_StatPart_Mounted".Translate());
                float adjustedLevel = 0;
                if (jobDriver.rider.skills != null)
                {
                    var skill = jobDriver.rider.skills.GetSkill(SkillDefOf.Animals).Level;
                    adjustedLevel = skill - (int)Math.Round(pawn.GetStatValue(StatDefOf.MinimumHandlingSkill, true));
                    var animalHandlingOffset = 1f + adjustedLevel * Settings.handlingMovementImpact / 100f;
                    sb.AppendLine("    " + "GUC_StatPart_HandlingMultiplier".Translate() + ": " +
                                  animalHandlingOffset.ToStringByStyle(ToStringStyle.PercentOne,
                                      ToStringNumberSense.Factor));
                    sb.AppendLine("        " + "GUC_StatPart_HandlingSkill".Translate() + ": " + skill);
                    sb.AppendLine("        " + "GUC_StatPart_SkillReq".Translate() + ": " +
                                  (int)Math.Round(pawn.GetStatValue(StatDefOf.MinimumHandlingSkill, true)));
                    sb.AppendLine("        " + "GUC_StatPart_LevelsAbove".Translate() + ": " + adjustedLevel);
                    sb.AppendLine("        " + "GUC_StatPart_HandlingMovementImpact".Translate() + ": " +
                                  (Settings.handlingMovementImpact / 100f).ToStringByStyle(ToStringStyle.PercentOne));
                }

                var modExt = pawn.kindDef.GetModExtension<CustomStats>();
                if (modExt != null)
                    sb.AppendLine("    " + "GUC_StatPart_MountTypeMultiplier".Translate() + ": " +
                                  modExt.speedModifier.ToStringByStyle(ToStringStyle.PercentOne,
                                      ToStringNumberSense.Factor));
            }
        }

        return sb.ToString();
    }

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.Thing is Pawn pawn)
        {
            if (ExtendedDataStorage.isMounted.Contains(pawn.thingIDNumber))
                val = pawn.GetGUData().mount.GetStatValue(StatDefOf.MoveSpeed);
            else if (pawn.IsMountedAnimal(out var thing) && thing is Pawn rider)
                val = GetRidingSpeed(val, pawn, rider.skills);
        }

        return;
    }

    public static float GetRidingSpeed(float baseValue, Pawn animal, Pawn_SkillTracker skills)
    {
        float adjustedLevel = 0;
        if (skills != null)
            adjustedLevel = skills.GetSkill(SkillDefOf.Animals).Level -
                            (int)Math.Round(animal.GetStatValue(StatDefOf.MinimumHandlingSkill, true));
        var animalHandlingOffset = 1f + adjustedLevel * Settings.handlingMovementImpact / 100f;
        baseValue *= animalHandlingOffset;
        var modExt = animal.kindDef.GetModExtension<CustomStats>();
        if (modExt != null) baseValue *= modExt.speedModifier;
        return baseValue;
    }
}

internal class StatPart_Armor : StatPart
{
    public override string ExplanationPart(StatRequest req)
    {
        var sb = new StringBuilder();
        if (req.Thing is Pawn pawn && pawn.jobs != null && pawn.jobs.curDriver is JobDriver_Mounted)
        {
            var modExt = pawn.kindDef.GetModExtension<CustomStats>();
            if (modExt != null && modExt.armorModifier != 1.0f)
            {
                sb.AppendLine("GUC_StatPart_Mounted".Translate());
                sb.AppendLine("    " + "GUC_StatPart_MountTypeMultiplier".Translate() + ": " +
                              modExt.armorModifier.ToStringByStyle(ToStringStyle.PercentZero,
                                  ToStringNumberSense.Factor));
            }
        }

        return sb.ToString();
    }

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.Thing is Pawn pawn && pawn.IsMountedAnimal())
        {
            var modExt = pawn.kindDef.GetModExtension<CustomStats>();
            if (modExt != null) val *= modExt.armorModifier;
        }
    }
}