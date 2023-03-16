using HarmonyLib;
using RimWorld.Planet;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp
{
    [HarmonyPatch(typeof(AnimalPenUtility), nameof(AnimalPenUtility.NeedsToBeManagedByRope))]
    class Patch_NeedsToBeManagedByRope
    {
        static bool Prepare()
        {
            return Settings.caravansEnabled || Settings.rideAndRollEnabled;
        }
        static bool Postfix(bool __result, Pawn pawn)
        {
            if (__result)
            {
                if ((pawn.IsMountedAnimal() && pawn.jobs != null && pawn.jobs.curDriver is Jobs.JobDriver_Mounted mounted && !mounted.isParking) || 
                    pawn.CurJobDef == ResourceBank.JobDefOf.WaitForRider) return false;
                else
                {
                    var reservedBy = pawn.GetGUData().reservedBy;
                    if (reservedBy != null && reservedBy.IsFormingCaravan()) return false;
                }
            }
            return __result;
        }
    }
}