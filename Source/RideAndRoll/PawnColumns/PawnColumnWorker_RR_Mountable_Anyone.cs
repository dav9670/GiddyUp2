using GiddyUp.Utilities;
using RimWorld;
using Verse;
using GiddyUp.Storage;
//using Multiplayer.API;

namespace GiddyUpRideAndRoll.PawnColumns
{
    class PawnColumnWorker_RR_Mountable_Anyone : PawnColumnWorker_Checkbox
    {
        public override bool HasCheckbox(Pawn pawn)
        {
            return IsMountableUtility.IsMountable(pawn);
        }
        public override bool GetValue(Pawn pawn)
        {
            return ExtendedDataStorage.GUComp[pawn.thingIDNumber].mountableByAnyone;
        }

        //[SyncMethod]
        public override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            ExtendedDataStorage.GUComp[pawn.thingIDNumber].mountableByAnyone = value;
        }
    }
}
