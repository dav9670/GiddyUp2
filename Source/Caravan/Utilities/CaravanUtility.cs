using GiddyUp.Storage;
using Verse;

namespace GiddyUpCaravan.Utilities
{
    static class CaravanUtility
    {
        public static bool RidingCaravanMount(this Pawn pawn, ExtendedPawnData pawndata)
        {
            if (pawn.IsColonist && pawndata.caravanMount != null)
            {
                return true;
            }
            return false;
        }
    }
}
