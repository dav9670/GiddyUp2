using GiddyUp.Jobs;
using RimWorld;
using System.Linq;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

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
            return IsMountable(thingDef.GetConcreteExample() as Pawn);
        }
        public static bool IsMountable(Pawn pawn)
        {
            return IsMountable(pawn, out Reason reason);
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

        public static bool IsMountable(Pawn animal, out Reason reason)
        {
            reason = Reason.CanMount;
            if (!Settings.mountableCache.Contains(animal.def.shortHash))
            {
                reason = Reason.NotInModOptions;
                return false;
            }
            if (animal.ageTracker.CurLifeStageIndex != animal.RaceProps.lifeStageAges.Count - 1)
            {
                var customLifeStages = animal.def.GetModExtension<AllowedLifeStagesPatch>();
                if (customLifeStages == null || !customLifeStages.GetAllowedLifeStagesAsList().Contains(animal.ageTracker.CurLifeStageIndex))
                {
                    reason = Reason.NotFullyGrown;
                    return false;
                }
            }
            if (animal.training == null || !animal.training.HasLearned(TrainableDefOf.Tameness))
            {
                reason = Reason.NeedsTraining;
                return false;
            }
            if (animal.roping?.IsRoped ?? false)
            {
                reason = Reason.IsRoped;
                return false;
            }
            return true;
        }
    }
}