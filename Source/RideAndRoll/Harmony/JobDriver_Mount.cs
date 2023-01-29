using GiddyUp.Jobs;
using GiddyUp.Storage;
using GiddyUpRideAndRoll.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace GiddyUpRideAndRoll.Harmony
{
    [HarmonyPatch(typeof(JobDriver_Mount), "TryMakePreToilReservations")]
    class JobDriver_Mount_TryMakePreToilReservations
    {

        static void Postfix(JobDriver_Mounted __instance, ref bool __result)
        {
            ExtendedDataStorage store = GiddyUp.Setup._extendedDataStorage;
            if (store == null)
            {
                __result = true;
                return;
            }
            ExtendedPawnData pawnData = store.GetExtendedDataFor(__instance.pawn.thingIDNumber);
            if (pawnData.targetJob == null)
            {
                __result = true;
                return;
            }
            //__result = ReserveUtility.ReserveEveryThingOfJob(pawnData.targetJob, __instance);
        }
    }
    [HarmonyPatch(typeof(JobDriver_Mount), "FinishAction")]
    class JobDriver_Mount_FinishAction
    {
        static void Postfix(JobDriver_Mount __instance)
        {
            ExtendedDataStorage store = GiddyUp.Setup._extendedDataStorage;
            ExtendedPawnData pawnData = store.GetExtendedDataFor(__instance.pawn.thingIDNumber);
            ExtendedPawnData animalData = store.GetExtendedDataFor(__instance.Mount.thingIDNumber);
            pawnData.owning = __instance.Mount;
            animalData.ownedBy = __instance.pawn;
        }
    }
    
}
