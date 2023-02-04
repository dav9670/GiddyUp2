using GiddyUp;
using HarmonyLib;
using Verse;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(ArmorUtility), nameof(ArmorUtility.ApplyArmor))]
    class ArmorUtility_ApplyArmor
    {
        static void Postfix(ref float armorRating, ref float damAmount, ref bool metalArmor, Pawn pawn)
        {
            if (IsMountableUtility.IsCurrentlyMounted(pawn))
            {
                var modExt = pawn.def.GetModExtension<CustomStatsPatch>();
                if (modExt != null)
                {
                    armorRating *= modExt.armorModifier;
                    damAmount /= modExt.armorModifier;
                    metalArmor = modExt.useMetalArmor;
                }
            }
        }
    }
}