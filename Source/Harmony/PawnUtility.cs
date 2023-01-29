using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace GiddyUp.HarmonyPlaceholder
{
    [HarmonyPatch(typeof(PawnUtility), "TrySpawnHatchedOrBornPawn")]
    class PawnUtility_TrySpawnHatchedOrBornPawn
    {
        static void Postfix(Pawn pawn, Thing motherOrEgg)
        {
            if(motherOrEgg is Pawn mother)
            {
                var pawnData = Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
                var motherData = Setup._extendedDataStorage.GetExtendedDataFor(mother.thingIDNumber);
                pawnData.mountableByAnyone = motherData.mountableByAnyone;
                pawnData.mountableByMaster = motherData.mountableByMaster;
            }
        }
    }
}
