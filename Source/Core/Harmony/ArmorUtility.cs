using Verse;

namespace GiddyUp.Harmony
{
    //[HarmonyPatch(typeof(ArmorUtility), nameof(ArmorUtility.ApplyArmor))] //Manually patched if needed
    class Patch_ApplyArmor
    {
        public static void Postfix(ref float armorRating, ref float damAmount, ref bool metalArmor, Pawn pawn)
        {
            if (pawn.IsAnimalMounted())
            {
                var modExt = pawn.def.GetModExtension<CustomStats>();
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