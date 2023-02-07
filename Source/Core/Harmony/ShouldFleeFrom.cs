using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(SelfDefenseUtility), nameof(SelfDefenseUtility.ShouldFleeFrom))]
    class Patch_ShouldFleeFrom
    {
        static bool Prefix(Pawn pawn, ref bool __result)
        {
            if (pawn.IsMounted())
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}