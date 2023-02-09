using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace GiddyUp.Jobs
{
    //This job is only used by non-hostile guests when they want to go use the guest dismount spots
    class JobDriver_Dismount : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            yield return Dismount();
        }
        Toil Dismount()
        {
            Toil toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.initAction = delegate
            {
                Pawn mount = ExtendedDataStorage.GUComp[pawn.thingIDNumber].mount;
                mount?.jobs.EndCurrentJob(JobCondition.InterruptForced);
                ReadyForNextToil();
            };
            return toil;
        }
    }
}