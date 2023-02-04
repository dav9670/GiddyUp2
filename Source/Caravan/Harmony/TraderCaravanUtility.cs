using GiddyUp;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GiddyUpCaravan.Harmony
{
    //This patch makes sure that mounted animals won't attempt to follow the caravan after they are parked
    [HarmonyPatch(typeof(TraderCaravanUtility), nameof(TraderCaravanUtility.GetTraderCaravanRole))]
    static class Patch_GetTraderCaravanRole
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.caravansEnabled;
        }
        static void Postfix(Pawn p, ref TraderCaravanRole __result)
        {
            if (p.RaceProps.Animal)
            {
                ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[p.thingIDNumber];

                if (pawnData.ownedBy != null)
                {
                    __result = TraderCaravanRole.Guard;
                }
            }
        }
    }
}
