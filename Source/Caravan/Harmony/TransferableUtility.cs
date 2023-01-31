using GiddyUp.Utilities;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUpCaravan.Harmony
{
    [HarmonyPatch(typeof(TransferableUtility), nameof(TransferableUtility.TransferAsOne))]
    static class TransferableUtility_TransferAsOne
    {
        static bool Postfix(bool __result, Thing a, Thing b)
        {
            if (__result && a.def.category == ThingCategory.Pawn && b.def.category == ThingCategory.Pawn &&
                (IsMountableUtility.isMountable(a as Pawn) || IsMountableUtility.isMountable(b as Pawn)) )
            {
                return false;
            }
            return __result;
        }
    }
}
