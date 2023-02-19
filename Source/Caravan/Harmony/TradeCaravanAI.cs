using GiddyUp;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;
using System.Collections.Generic;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpCaravan.Harmony
{
    //This patch makes sure that mounted animals won't attempt to follow the caravan after they are parked
    //TODO: There's probably a more elegant way to handle this, investigate further
    [HarmonyPatch]
    static class Patch_TradeCaravanAI
    {
        static bool Prepare()
        {
            return Settings.caravansEnabled;
        }
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(JobGiver_AIFollowPawn), nameof(JobGiver_AIFollowPawn.TryGiveJob));
            yield return AccessTools.Method(typeof(JobGiver_AIDefendEscortee), nameof(JobGiver_AIDefendEscortee.TryGiveJob));
        }
        static bool Prefix(Job __result, Pawn pawn)
        {
            if (pawn.RaceProps.Animal && pawn.GetGUData().reservedBy != null)
            {
                __result = null;
                return false;
            }
            return true;
        }
    }
    //Have them follow the leader instead of considering their own mount as something to guard
    //TODO: The logic could be improved if turned into a transpiler
    [HarmonyPatch(nameof(LordToil_ExitMapAndEscortCarriers), nameof(LordToil_ExitMapAndEscortCarriers.GetClosestCarrier))]
    static class Patch_GetClosestCarrier
    {
        static bool Prepare()
        {
            return Settings.caravansEnabled;
        }
        static Pawn Postfix(Pawn __result, LordToil_ExitMapAndEscortCarriers __instance, Pawn closestTo)
        {
            if (__result != null)
            {
                var animalData = __result.GetGUData();
                if (animalData.reservedBy != null)
                {
                    Pawn trader = TraderCaravanUtility.FindTrader(__instance.lord);
                    if (trader != null) return trader;
                    else animalData.reservedBy.Dismount(__result, null, true, ropeIfNeeded: false);
                }
            }
            return __result;
        }
    }
}