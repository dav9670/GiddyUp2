using GiddyUp;
using RimWorld;
using Verse;
using static GiddyUp.ExtendedPawnData;
using Settings = GiddyUp.ModSettings_GiddyUp;
//using Multiplayer.API;

namespace GiddyUpRideAndRoll
{
    class PawnColumnWorker_Mountable_Colonists : PawnColumnWorker_Checkbox
    {
        public override bool HasCheckbox(Pawn pawn)
        {
            return pawn.IsEverMountable();
        }
        public override bool GetValue(Pawn pawn)
        {
            var isChecked = pawn.GetGUData().automount;
            return isChecked == Automount.Anyone || isChecked == Automount.Colonists;
        }

        //[SyncMethod]
        public override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            var pawnData = pawn.GetGUData();
            //Enabling?
            if (value)
            {
                if (pawnData.automount == Automount.Slaves) pawnData.automount = Automount.Anyone;
                else pawnData.automount = Automount.Colonists;
            }
            else
            {
                if (pawnData.automount == Automount.Anyone) pawnData.automount = Automount.Slaves;
                else pawnData.automount = Automount.False;
            }
        }
    }
    class PawnColumnWorker_Mountable_Slaves : PawnColumnWorker_Checkbox
    {
        public override bool HasCheckbox(Pawn pawn)
        {
            return pawn.IsEverMountable();
        }
        public override bool GetValue(Pawn pawn)
        {
            var isChecked = pawn.GetGUData().automount;
            return isChecked == Automount.Anyone || isChecked == Automount.Slaves;
        }

        //[SyncMethod]
        public override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            var pawnData = pawn.GetGUData();
            //Enabling?
            if (value)
            {
                if (pawnData.automount == Automount.Colonists) pawnData.automount = Automount.Anyone;
                else pawnData.automount = Automount.Slaves;
            }
            else
            {
                if (pawnData.automount == Automount.Anyone) pawnData.automount = Automount.Colonists;
                else pawnData.automount = Automount.False;
            }
        }
    }

    class PawnColumnWorker_AllowedToRide : PawnColumnWorker_Checkbox
    {
        public override bool HasCheckbox(Pawn pawn)
        {
            return true;
        }
        public override bool GetValue(Pawn pawn)
        {
            return pawn.CanRide();
        }

        //[SyncMethod]
        public override void SetValue(Pawn pawn, bool value, PawnTable table)
        {
            pawn.GetGUData().canRide = !pawn.CanRide();
        }
    }
}