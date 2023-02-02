using GiddyUp.Utilities;
using HarmonyLib;
using Verse;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(ArmorUtility), nameof(ArmorUtility.ApplyArmor))]
    class ArmorUtility_ApplyArmor
    {
        static void Postfix(ref float armorRating, Pawn pawn)
        {
            if (IsMountableUtility.IsCurrentlyMounted(pawn))
            {
                var modExt = pawn.def.GetModExtension<CustomStatsPatch>();
                if (modExt != null) armorRating *= modExt.armorModifier;
            }
        }
        static void Postfix(ref float damAmount, Pawn pawn, ref bool metalArmor)
        {
            if (IsMountableUtility.IsCurrentlyMounted(pawn))
            {
                var modExt = pawn.def.GetModExtension<CustomStatsPatch>();
                if (modExt != null)
                {
                    damAmount /= modExt.armorModifier;
                    metalArmor = modExt.useMetalArmor;
                }
            }
        }
    }
}
