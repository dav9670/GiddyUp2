using GiddyUp.Storage;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace GiddyUpCaravan.Harmony
{
    //This patch makes sure that mounted animals won't attempt to follow the caravan after they are parked
    [HarmonyPatch(typeof(TraderCaravanUtility), nameof(TraderCaravanUtility.GetTraderCaravanRole))]
    static class Patch_GetTraderCaravanRole
    {
        static void Postfix(Pawn p, ref TraderCaravanRole __result)
        {
            if (p.RaceProps.Animal)
            {
                ExtendedPawnData pawnData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(p.thingIDNumber);

                if (pawnData.ownedBy != null)
                {
                    __result = TraderCaravanRole.Guard;
                }
            }
        }
    }
}
