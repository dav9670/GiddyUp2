using System.Collections.Generic;
using Verse.AI;

namespace GiddyUp.Jobs;

//This job is only used by non-hostile guests when they want to go use the guest dismount spots
internal class JobDriver_Dismount : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return new Toil
        {
            defaultCompleteMode = ToilCompleteMode.Never,
            initAction = delegate
            {
                var pawnData = pawn.GetExtendedPawnData();
                pawn.Dismount(pawnData.Mount, pawnData, false, job.GetFirstTarget(TargetIndex.A));
                ReadyForNextToil();
            }
        };
    }
}