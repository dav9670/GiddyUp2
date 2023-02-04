using GiddyUp.Storage;
using System.Collections.Generic;
using Verse.AI;

namespace GiddyUp.Jobs
{
    class JobDriver_Dismount : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
            yield return Dismount();
        }
        Toil Dismount()
        {
            Toil toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.initAction = delegate {
                ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
                if(pawnData.mount != null)
                {
                    pawnData.mount.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
                ReadyForNextToil();
            };
            return toil;
        }
    }
}
