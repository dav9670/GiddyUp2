using GiddyUp;
using RimWorld;
using Verse;
//using Multiplayer.API;

namespace GiddyUpRideAndRoll
{
    class PawnColumnWorker_RR_Mountable_Anyone : PawnColumnWorker_Checkbox
    {
        public override bool HasCheckbox(Pawn pawn)
        {
            return pawn.IsEverMountable();
        }
        public override bool GetValue(Pawn pawn)
        {
            return pawn.GetGUData().automount;
        }

        //[SyncMethod]
        public override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            pawn.GetGUData().automount = value;
        }
    }
}
