using GiddyUp.Storage;
using GiddyUp.Utilities;
using RimWorld;
using Verse;
//using Multiplayer.API;

namespace GiddyUpRideAndRoll.PawnColumns
{
    class PawnColumnWorker_RR_Mountable_Anyone : PawnColumnWorker_Checkbox
    {
        protected override bool HasCheckbox(Pawn pawn)
        {
            return IsMountableUtility.isMountable(pawn);
        }
        protected override bool GetValue(Pawn pawn)
        {
            return GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber).mountableByAnyone;
        }

        //[SyncMethod]
        protected override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            ExtendedPawnData pawnData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
            pawnData.mountableByAnyone = value;
        }
    }
}
