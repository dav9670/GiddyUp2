using GiddyUp.Storage;
using Verse;

namespace GiddyUpCaravan.Utilities
{
    static class CaravanUtility
    {
        public static bool RidingCaravanMount(this Pawn pawn)
        {
            ExtendedPawnData pawndata = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
            if (pawn.IsColonist && pawndata != null && pawndata.caravanMount != null)
            {
                return true;

            }
            return false;
        }
    }
}
