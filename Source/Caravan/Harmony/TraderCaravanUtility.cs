using GiddyUp;
using HarmonyLib;
using RimWorld;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpCaravan.Harmony
{
    //This patch makes sure that mounted animals won't attempt to follow the caravan after they are parked
    [HarmonyPatch(typeof(TraderCaravanUtility), nameof(TraderCaravanUtility.GetTraderCaravanRole))]
    static class Patch_GetTraderCaravanRole
    {
        static bool Prepare()
        {
            return Settings.caravansEnabled;
        }
        static void Postfix(Pawn p, ref TraderCaravanRole __result)
        {
            if (p.RaceProps.Animal)
            {
                if (p.GetGUData().reservedBy != null) __result = TraderCaravanRole.Guard;
            }
        }
    }
}