using GiddyUp.Storage;
using GiddyUp.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
//using Multiplayer.API;

namespace GiddyUpRideAndRoll.PawnColumns
{
    class PawnColumnWorker_RR_Mountable_Master : PawnColumnWorker_Checkbox
    {
        protected override bool HasCheckbox(Pawn pawn)
        {
            return IsMountableUtility.isMountable(pawn) && pawn.playerSettings != null && pawn.playerSettings.Master != null;
        }

        protected override bool GetValue(Pawn pawn)
        {
            return GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber).mountableByMaster;
        }

        //[SyncMethod]
        protected override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            ExtendedPawnData pawnData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
            pawnData.mountableByMaster = value;
        }
       
    }
}
