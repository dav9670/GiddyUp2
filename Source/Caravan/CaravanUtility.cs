using GiddyUp;
using Verse;

namespace GiddyUpCaravan
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