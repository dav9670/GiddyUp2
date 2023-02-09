using System.Collections.Generic;
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
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
            yield return new Toil()
            {
                defaultCompleteMode = ToilCompleteMode.Never,
                initAction = delegate
                {
                    var pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
                    pawn.Dismount(pawnData.mount, pawnData, false, false, this.job.GetFirstTarget(TargetIndex.A));
                    ReadyForNextToil();
                }
            };
        }
    }
}