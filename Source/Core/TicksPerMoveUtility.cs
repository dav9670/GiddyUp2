using RimWorld;
using Verse;

namespace GiddyUp
{
    public class TicksPerMoveUtility
    {
        public static int AdjustedTicksPerMove(Pawn_SkillTracker skills, Pawn mount, bool diagonal)
        {
            float adjustedLevel = 5;
            if (skills != null)
            {
                adjustedLevel = skills.GetSkill(SkillDefOf.Animals).levelInt - (int)System.Math.Round(mount.GetStatValue(StatDefOf.MinimumHandlingSkill, true));
            }

            float animalHandlingOffset = 1f - (adjustedLevel * ModSettings_GiddyUp.handlingMovementImpact) / 100f;
            float customSpeedModifier = 1f;
            var modExt = mount.def.GetModExtension<CustomStatsPatch>();
            if (modExt != null) customSpeedModifier = 1f / modExt.speedModifier;
            float factor = animalHandlingOffset * customSpeedModifier;
            if (diagonal)
            {
                return (int)System.Math.Round((float)mount.TicksPerMoveDiagonal * factor);
            }
            else
            {
                return (int)System.Math.Round((float)mount.TicksPerMoveCardinal * factor);
            }
        }
    }
}
