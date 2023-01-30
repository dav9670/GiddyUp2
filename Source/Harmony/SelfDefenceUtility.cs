using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(SelfDefenseUtility), nameof(SelfDefenseUtility.ShouldFleeFrom))]
    class SelfDefenceUtility_ShouldFleeFrom
    {
        static bool Prefix(Pawn pawn, ref bool __result)
        {
            if(pawn.CurJob != null && pawn.CurJob.def == ResourceBank.JobDefOf.Mounted)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
