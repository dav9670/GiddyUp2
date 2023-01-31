using GiddyUp.Jobs;
using GiddyUp.Storage;
using RimWorld;
using System.Text;
using UnityEngine;
using Verse;

namespace GiddyUp.Stats
{
    public class StatPart_Riding : StatPart
    {
        public override string ExplanationPart(StatRequest req)
        {
            StringBuilder sb = new StringBuilder();

            if (req.Thing is Pawn pawn)
            {
                if (Setup.isMounted.Contains(pawn.thingIDNumber))
                {
                    ExtendedPawnData pawnData = Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
                    float mountSpeed = pawnData.mount.GetStatValue(StatDefOf.MoveSpeed);
                    sb.AppendLine("GUC_GiddyUp".Translate());
                    sb.AppendLine("    " + "GUC_StatPart_MountMoveSpeed".Translate() + ": " + mountSpeed.ToStringByStyle(ToStringStyle.FloatTwo));
                }

                if(pawn.jobs != null && pawn.jobs.curDriver is JobDriver_Mounted jobDriver)
                {
                    sb.AppendLine("Giddy up!");
                    float adjustedLevel = 0;
                    if (jobDriver.Rider.skills != null && jobDriver.Rider.skills.GetSkill(SkillDefOf.Animals) is SkillRecord skill)
                    {
                        adjustedLevel = skill.levelInt - Mathf.RoundToInt(pawn.GetStatValue(StatDefOf.MinimumHandlingSkill, true));
                        float animalHandlingOffset = 1f + (adjustedLevel * ModSettings_GiddyUp.handlingMovementImpact) / 100f;
                        sb.AppendLine("    " + "GUC_StatPart_HandlingMultiplier".Translate() + ": " + animalHandlingOffset.ToStringByStyle(ToStringStyle.PercentOne, ToStringNumberSense.Factor));
                        sb.AppendLine("        " + "GUC_StatPart_HandlingSkill".Translate() + ": " + skill.levelInt);
                        sb.AppendLine("        " + "GUC_StatPart_SkillReq".Translate() + ": " + Mathf.RoundToInt(pawn.GetStatValue(StatDefOf.MinimumHandlingSkill, true)));
                        sb.AppendLine("        " + "GUC_StatPart_LevelsAbove".Translate() + ": " + adjustedLevel);
                        sb.AppendLine("        " + "GUC_StatPart_HandlingMovementImpact".Translate() + ": " + (ModSettings_GiddyUp.handlingMovementImpact / 100f).ToStringByStyle(ToStringStyle.PercentOne));
                    }
                    if (pawn.def.GetModExtension<CustomStatsPatch>() is CustomStatsPatch modExt)
                    {
                        sb.AppendLine("    " + "GUC_StatPart_MountTypeMultiplier".Translate() + ": " + modExt.speedModifier.ToStringByStyle(ToStringStyle.PercentOne, ToStringNumberSense.Factor));
                    }
                }
            }
            return sb.ToString();
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if(req.Thing is Pawn pawn)
            {
                if (Setup.isMounted.Contains(pawn.thingIDNumber))
                {
                    ExtendedPawnData pawnData = Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
                    float mountSpeed = pawnData.mount.GetStatValue(StatDefOf.MoveSpeed);
                    val = mountSpeed;
                    return;
                }
                if (pawn.jobs != null)
                {
                    if (pawn.jobs.curDriver is JobDriver_Mounted jdMounted) val = GetRidingSpeed(val, pawn, jdMounted.Rider);
                    /// Set speed of mount so it always matches the speed of the pawn the animal is waiting for. 
                    else if (ModSettings_GiddyUp.rideAndRollEnabled && pawn.jobs.curDriver is GiddyUpRideAndRoll.Jobs.JobDriver_WaitForRider jobDriver) val = jobDriver.Followee.GetStatValue(StatDefOf.MoveSpeed);
                }
            }
        }

        public static float GetRidingSpeed(float baseValue, Pawn pawn, Pawn rider)
        {
            float adjustedLevel = 0;
            if (rider.skills != null && rider.skills.GetSkill(SkillDefOf.Animals) is SkillRecord skill)
            {
                adjustedLevel = skill.levelInt - Mathf.RoundToInt(pawn.GetStatValue(StatDefOf.MinimumHandlingSkill, true));
            }
            float animalHandlingOffset = 1f + (adjustedLevel * ModSettings_GiddyUp.handlingMovementImpact) / 100f;
            baseValue *= animalHandlingOffset;
            if (pawn.def.GetModExtension<CustomStatsPatch>() is CustomStatsPatch modExt)
            {
                float customSpeedModifier = modExt.speedModifier;
                baseValue *= customSpeedModifier;
            }
            return baseValue;
        }
    }
}
