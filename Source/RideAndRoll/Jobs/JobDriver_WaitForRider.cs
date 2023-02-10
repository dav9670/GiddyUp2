using GiddyUp;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace GiddyUpRideAndRoll.Jobs
{
    class JobDriver_WaitForRider : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public Pawn ReservedBy
        {
            get
            {
                if (TargetA.Thing is Pawn pawn) { return pawn; }
                else { return null; }
            }
        }
        int moveInterval = Rand.Range(300, 1200);
        JobDef initialJob; //Keep track of the pawn's job

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => pawn.Map == null || this.ReservedBy == null);
            initialJob = ReservedBy.CurJobDef;
            yield return new Toil { initAction = () => WalkRandomNearby(), defaultCompleteMode = ToilCompleteMode.Instant };
            Toil toil = new Toil
            {
                tickAction = delegate
                {
                    var reservedBy = ReservedBy;
                    if (reservedBy.Map == null ||
                       reservedBy.Dead ||
                       reservedBy.Downed ||
                       reservedBy.InMentalState ||
                       reservedBy.CurJobDef == ResourceBank.JobDefOf.Mount ||
                       reservedBy.InBed() ||
                       pawn.health.HasHediffsNeedingTend() ||
                       (pawn.needs.food != null && pawn.needs.food.CurCategory >= HungerCategory.UrgentlyHungry) ||
                       (pawn.needs.rest != null && pawn.needs.rest.CurCategory >= RestCategory.VeryTired))
                    {
                        //One last check - if the animal is in a barn, they needn't wait.
                        var room = pawn.GetRoom();
                        if (room == null || room.Role == ResourceBank.RoomRoleDefOf.Barn)
                        {
                            this.EndJobWith(JobCondition.Succeeded);
                            return;
                        }
                    }   
                    
                    if (pawn.IsHashIntervalTick(moveInterval) && !this.pawn.pather.Moving)
                    {
                        WalkRandomNearby();
                        moveInterval = Rand.Range(300, 600);
                    }
                    if (TimeUntilExpire(pawn.CurJob) < 10 && ReservedBy.CurJobDef == initialJob)
                    {
                        pawn.CurJob.expiryInterval += 1000;
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };
            toil.AddFinishAction(() =>
            {
                var reservedBy = ReservedBy;
                if (reservedBy.CurJobDef != ResourceBank.JobDefOf.Mount)
                {
                    var pen = AnimalPenUtility.GetPenAnimalShouldBeTakenTo(reservedBy, this.pawn, out string failReason, true);
                    if (pen != null)
                    {
                        reservedBy.jobs.jobQueue.EnqueueFirst(new Job(JobDefOf.RopeToPen, this.pawn, AnimalPenUtility.FindPlaceInPenToStand(pen, reservedBy)));
                    }
                }

                UnsetOwnership();
            });
            yield return toil;
        }
        void UnsetOwnership()
        {
            ExtendedPawnData animalData = pawn.GetGUData();
            ExtendedPawnData riderData = animalData.reservedBy.GetGUData();
            if (riderData.reservedMount == this.pawn) riderData.ReservedMount = null;
            animalData.ReservedBy = null;
        }
        void WalkRandomNearby()
        {
            var room = ReservedBy.GetRoom();
            if (room == null || room.Role == RoomRoleDefOf.None)
            {
                IntVec3 target = RCellFinder.RandomWanderDestFor(ReservedBy, this.ReservedBy.Position, 8, ((Pawn p, IntVec3 loc, IntVec3 root) => true), Danger.Some);
                this.pawn.pather.StartPath(target, PathEndMode.Touch);
            }
        }
        int TimeUntilExpire(Job job)
        {
            return job.expiryInterval - (Find.TickManager.TicksGame - job.startTick);
        }
    }
}