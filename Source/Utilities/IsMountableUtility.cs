using GiddyUp.Jobs;
using GiddyUp.Storage;
using RimWorld;
using Verse;

namespace GiddyUp.Utilities
{
    public static class IsMountableUtility
    {
        public enum Reason{NotFullyGrown, NotInModOptions, CanMount, IsRoped, NeedsTraining};

        public static Pawn CurMount(this Pawn pawn)
        {
            return Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber).mount;
        }
        public static bool IsMountable(ThingDef thingDef)
        {
            return isMountable(thingDef.GetConcreteExample() as Pawn);
        }
        public static bool isMountable(Pawn pawn)
        {
            return isMountable(pawn, out Reason reason);
        }

        public static bool IsCurrentlyMounted(Pawn animal)
        {
            if(animal.CurJob == null || animal.CurJob.def != ResourceBank.JobDefOf.Mounted)
            {
                return false;
            }
            JobDriver_Mounted mountedDriver = (JobDriver_Mounted)animal.jobs.curDriver;
            Pawn rider = mountedDriver.Rider;
            return Setup.isMounted.Contains(rider.thingIDNumber);
        }

        public static bool isMountable(Pawn animal, out Reason reason)
        {
            reason = Reason.CanMount;
            if (!isAllowedInModOptions(animal.def.shortHash))
            {
                reason = Reason.NotInModOptions;
                return false;
            }
            if (animal.ageTracker.CurLifeStageIndex != animal.RaceProps.lifeStageAges.Count - 1)
            {
                if (!animal.def.HasModExtension<AllowedLifeStagesPatch>())
                {
                    reason = Reason.NotFullyGrown;
                    return false;
                }
                else //Use custom life stages instead of last life stage if a patch exists for that
                {
                    AllowedLifeStagesPatch customLifeStages = animal.def.GetModExtension<AllowedLifeStagesPatch>();
                    if (!customLifeStages.getAllowedLifeStagesAsList().Contains(animal.ageTracker.CurLifeStageIndex))
                    {
                        reason = Reason.NotFullyGrown;
                        return false;
                    }
                }
            }
            if (animal.training == null || (animal.training != null && !animal.training.HasLearned(TrainableDefOf.Tameness)))
            {
                reason = Reason.NeedsTraining;
                return false;
            }
            if (animal.roping != null && animal.roping.IsRoped)
            {
                reason = Reason.IsRoped;
                return false;
            }
            return true;
        }

        public static bool isAllowedInModOptions(ushort hash)
        {
            return ModSettings_GiddyUp._animalSelecter.Contains(hash);
        }
    }
}
