using GiddyUp;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUpCaravan.Harmony
{
    [HarmonyPatch(typeof(TransferableUtility), nameof(TransferableUtility.TransferAsOne))]
    static class TransferableUtility_TransferAsOne
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.caravansEnabled;
        }
        static bool Postfix(bool __result, Thing a, Thing b)
        {
            if (__result && a.def.category == ThingCategory.Pawn && b.def.category == ThingCategory.Pawn &&
                (IsMountableUtility.IsEverMountable(a as Pawn) || IsMountableUtility.IsEverMountable(b as Pawn)) )
            {
                return false;
            }
            return __result;
        }
    }
}
