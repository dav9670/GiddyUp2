using RimWorld;
using UnityEngine;
using Verse;

namespace GiddyUp.Utilities
{
    public class TicksPerMoveUtility
    {
        public static int AdjustedTicksPerMove(Pawn pawn, Pawn mount, bool diagonal)
        {
            float adjustedLevel = 5;
            if (pawn.skills != null && pawn.skills.GetSkill(SkillDefOf.Animals) is SkillRecord skill)
            {
                adjustedLevel = skill.levelInt - Mathf.RoundToInt(mount.GetStatValue(StatDefOf.MinimumHandlingSkill, true));
            }

            float animalHandlingOffset = 1f - (adjustedLevel * ModSettings_GiddyUp.handlingMovementImpact) / 100f;
            float customSpeedModifier = 1f;
            if (mount.def.GetModExtension<CustomStatsPatch>() is CustomStatsPatch modExt)
            {
                customSpeedModifier = 1f/modExt.speedModifier;
            }
            float factor = animalHandlingOffset * customSpeedModifier;
            if (diagonal)
            {
                return Mathf.RoundToInt((float)mount.TicksPerMoveDiagonal * factor);
            }
            else
            {
                return Mathf.RoundToInt((float)mount.TicksPerMoveCardinal * factor);
            }
        }
    }
}
