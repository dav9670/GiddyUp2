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
            ExtendedPawnData pawnData = Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
            yield return Dismount();
        }
        Toil Dismount()
        {
            ExtendedDataStorage store = Setup._extendedDataStorage;

            Toil toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.initAction = delegate {
                ExtendedPawnData pawnData = store.GetExtendedDataFor(pawn.thingIDNumber);
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
