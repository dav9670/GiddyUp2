using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using System.Collections.Generic;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp;

public static class IsMountableUtility
{
    public enum Reason
    {
        False,
        NotFullyGrown,
        NotInModOptions,
        CanMount,
        IsRoped,
        NeedsTraining,
        MountedByAnother,
        IsBusy,
        IsBusyWithCaravan,
        IsPoorCondition,
        WrongFaction,
        NotAnimal,
        IsReserved,
        MissingResearch,
        TooYoung,
        IncompatibleEquipment,
        TooHeavy,
        Forbidden
    };

    private static HashSet<JobDef> busyJobs = new()
    {
        ResourceBank.JobDefOf.Mounted, JobDefOf.LayEgg, JobDefOf.Nuzzle, JobDefOf.Lovin, JobDefOf.Vomit,
        JobDefOf.Wait_Downed
    };

    public static bool IsMountedAnimal(this Pawn animal)
    {
        return IsMountedAnimal(animal, out var rider);
    }

    public static bool IsMountedAnimal(this Pawn animal, out Thing rider)
    {
        if (animal.CurJobDef != ResourceBank.JobDefOf.Mounted)
        {
            rider = null;
            return false;
        }

        rider = animal.jobs.curDriver.job.targetA.Thing;
        return rider.IsMounted();
    }

    public static bool IsEverMountable(this Pawn pawn)
    {
        return IsEverMountable(pawn, out var reason);
    }

    public static bool IsEverMountable(this Pawn pawn, out Reason reason)
    {
        return IsMountable(pawn, out reason, null, false, false, false);
    }

    public static bool IsMountable(this Pawn animal, out Reason reason, Pawn rider, bool checkState = true,
        bool checkFaction = false, bool checkTraining = true,
        List<ReservationManager.Reservation> reservationsToCheck = null)
    {
        reason = Reason.CanMount;
        //Is even an animal?
        if (!animal.RaceProps.Animal || animal == rider)
        {
            reason = Reason.NotAnimal;
            return false;
        }

        //Check faction
        if (checkFaction && animal.Faction != rider.Faction)
        {
            reason = Reason.WrongFaction;
            return false;
        }

        //Check mod options
        if (animal == null || !Settings.mountableCache.Contains(animal.def.shortHash))
        {
            reason = Reason.NotInModOptions;
            return false;
        }

        //Check animal's jobs to see if busy
        if (checkState)
        {
            //Animal is busy with a job?
            if (busyJobs.Contains(animal.CurJobDef))
            {
                //Is that job mounted?
                if (animal.CurJobDef == ResourceBank.JobDefOf.Mounted)
                {
                    //Is the rider of this mounted job this same pawn? Skip
                    var animalData = animal.GetGUData();
                    if (animalData.reservedBy != null &&
                        animalData.reservedBy.CurJobDef == ResourceBank.JobDefOf.Mount &&
                        animalData.reservedBy.GetGUData().reservedMount == animal)
                    {
                        goto RiderSkip;
                    }
                    //If its another pawn, fail
                    else
                    {
                        reason = Reason.MountedByAnother;
                        return false;
                    }
                }
                else
                {
                    reason = Reason.IsBusy;
                    return false;
                }
            }

            RiderSkip:
            //Check if roped
            if (animal.roping?.IsRopedByPawn ?? false)
            {
                reason = Reason.IsRoped;
                return false;
            }

            //animal forming caravan?
            var animalLord = animal.GetLord();
            if (animalLord != null)
                if (animalLord.LordJob != null && animalLord.LordJob is LordJob_FormAndSendCaravan &&
                    animal.GetGUData().reservedBy != rider)
                {
                    reason = Reason.IsBusyWithCaravan;
                    return false;
                }

            //TODO maybe add some logic to check if involved with a ritual
            //Check health
            if (animal.Dead || animal.Downed || animal.InMentalState || !animal.Spawned ||
                (animal.health != null &&
                 animal.health.summaryHealth.SummaryHealthPercent < Settings.injuredThreshold) ||
                animal.health.HasHediffsNeedingTend() ||
                animal.HasAttachment(ThingDefOf.Fire) ||
                (animal.Faction.def
                     .isPlayer && //Need checks only apply to colonist animals because guest caravans ride this value down very low before leaving
                 ((animal.needs.food != null && animal.needs.food.CurCategory >= HungerCategory.UrgentlyHungry) ||
                  (animal.needs.rest != null && animal.needs.rest.CurCategory >= RestCategory.VeryTired)))
               )
            {
                reason = Reason.IsPoorCondition;
                return false;
            }

            var modExt = animal.def.GetModExtension<ResearchRestrictions>();
            if (modExt != null)
                foreach (var researchProjectDef in modExt.researchProjectDefs)
                    if (!researchProjectDef.IsFinished)
                    {
                        reason = Reason.MissingResearch;
                        return false;
                    }
        }

        //Check age
        if (!animal.ageTracker.Adult)
        {
            var customLifeStages = animal.def.GetModExtension<AllowedLifeStages>();
            if (customLifeStages == null || !customLifeStages.IsAllowedAge(animal.ageTracker.CurLifeStageIndex))
            {
                reason = Reason.NotFullyGrown;
                return false;
            }
        }

        //Check training
        if (checkTraining && (animal.training == null || !animal.training.HasLearned(TrainableDefOf.Tameness)))
        {
            reason = Reason.NeedsTraining;
            return false;
        }

        if (rider != null)
        {
            //Is the pawn too much of a hefty lad?
            if (rider.IsTooHeavy(animal))
            {
                reason = Reason.TooHeavy;
                return false;
            }

            //Can reserve? Null check as this may be a non-specific check like the UI
            if (ReserversOfFast(animal, reservationsToCheck, out var claimants))
                for (var i = claimants.Count; i-- > 0;)
                {
                    var claimant = claimants[i];
                    if (claimant.CurJobDef != JobDefOf.RopeToPen)
                    {
                        reason = Reason.IsReserved;
                        return false;
                    }
                }
        }

        return true;

        static bool ReserversOfFast(Pawn animal, List<ReservationManager.Reservation> reservations,
            out List<Pawn> claimants)
        {
            claimants = new List<Pawn>();
            if (reservations == null) reservations = animal.Map.FetchReservedAnimals();
            for (var i = reservations.Count; i-- > 0;)
            {
                var item = reservations[i];
                if (item.Target == animal) claimants.Add(item.Claimant);
            }

            return claimants.Count > 0;
        }
    }

    public static bool IsStillMountable(this Pawn animal, Pawn rider, out Reason reason)
    {
        if (!animal.IsMountable(out reason, rider))
        {
            var animalData = animal.GetGUData();
            if (animalData.reservedBy != null)
            {
                animalData.reservedBy.GetGUData().ReservedMount = null;
                animalData.ReservedBy = null;
            }

            return false;
        }

        return true;
    }

    public static bool IsAllowed(this Pawn rider, Pawn animal)
    {
        return rider.IsAllowed(animal.GetGUData());
    }

    public static bool IsAllowed(this Pawn rider, ExtendedPawnData animalData)
    {
        var automount = animalData.automount;
        if (automount == ExtendedPawnData.Automount.Anyone) return true;
        else if (automount == ExtendedPawnData.Automount.Colonists && rider.GuestStatus == null) return true;
        else if (automount == ExtendedPawnData.Automount.Slaves && rider.GuestStatus == GuestStatus.Slave) return true;
        return false;
    }

    public static bool IsCapableOfRiding(this Pawn pawn, out Reason reason)
    {
        if (pawn.IsWorkTypeDisabledByAge(WorkTypeDefOf.Handling, out var ageNeeded))
        {
            reason = Reason.TooYoung;
            return false;
        }

        if (pawn.apparel != null && pawn.apparel.WornApparel.Any(x => x.def.defName == "Wheelchair"))
        {
            reason = Reason.IncompatibleEquipment;
            return false;
        }

        reason = Reason.False;
        return true;
    }

    public static bool IsTooHeavy(this Pawn rider, Pawn animal)
    {
        return rider.GetStatValue(StatDefOf.Mass) > animal.GetStatValue(StatDefOf.CarryingCapacity);
    }

    public static List<ReservationManager.Reservation> FetchReservedAnimals(this Map map)
    {
        var workingList = new List<ReservationManager.Reservation>();
        var list = map.reservationManager.reservations;
        for (var i = list.Count; i-- > 0;)
        {
            var item = list[i];
            if (item.Target.Thing is Pawn pawn && pawn.RaceProps.Animal) workingList.Add(item);
        }

        return workingList;
    }
}