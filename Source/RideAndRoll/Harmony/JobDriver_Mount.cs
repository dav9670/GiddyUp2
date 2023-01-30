using GiddyUp.Jobs;
using GiddyUp.Storage;
using HarmonyLib;

namespace GiddyUpRideAndRoll.Harmony
{
    [HarmonyPatch(typeof(JobDriver_Mount), nameof(JobDriver_Mount.TryMakePreToilReservations))]
    class JobDriver_Mount_TryMakePreToilReservations
    {
        static bool Postfix(bool __result, JobDriver_Mounted __instance)
        {
            if (GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(__instance.pawn.thingIDNumber).targetJob == null)
            {
                return true;
            }
            return __result;
        }
    }
    [HarmonyPatch(typeof(JobDriver_Mount), nameof(JobDriver_Mount.FinishAction))]
    class JobDriver_Mount_FinishAction
    {
        static void Postfix(JobDriver_Mount __instance)
        {
            ExtendedDataStorage store = GiddyUp.Setup._extendedDataStorage;
            var mount = __instance.Mount;
            ExtendedPawnData pawnData = store.GetExtendedDataFor(__instance.pawn.thingIDNumber);
            ExtendedPawnData animalData = store.GetExtendedDataFor(mount.thingIDNumber);
            pawnData.owning = mount;
            animalData.ownedBy = __instance.pawn;
        }
    }
    
}
