using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUp.Harmony
{
    //Prevents animal from fleeing when mounted. TODO: consider tying in the animal handling skill?
    [HarmonyPatch(typeof(SelfDefenseUtility), nameof(SelfDefenseUtility.ShouldFleeFrom))]
    class Patch_ShouldFleeFrom
    {
        static bool Prefix(Pawn pawn, ref bool __result)
        {
            if (pawn.IsMountedAnimal())
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}