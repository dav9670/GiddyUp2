using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
//using Multiplayer.API;

namespace GiddyUp
{
    public static class GUC_FloatMenuUtility
    {
        public static void AddMountingOptions(Pawn target, Pawn pawn, List<FloatMenuOption> opts)
        {
            var pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];

            if (target.Faction != null && !target.factionInt.def.isPlayer) return;
            if (pawn.IsWorkTypeDisabledByAge(WorkTypeDefOf.Handling, out int ageNeeded)) return;

            if (pawnData.mount == null)
            {
                bool canMount = false;
                if (target.RaceProps.Animal)
                {
                    var animalCurJob = target.CurJob?.def;
                    if (animalCurJob != null && (target.InMentalState ||
                        animalCurJob == JobDefOf.LayEgg ||
                        animalCurJob == JobDefOf.Nuzzle ||
                        animalCurJob == JobDefOf.Lovin ||
                        animalCurJob == JobDefOf.Wait_Downed ||
                        animalCurJob == ResourceBank.JobDefOf.Mounted ||
                        target.HasAttachment(ThingDefOf.Fire)
                        ))
                    {
                        opts.Add(new FloatMenuOption("GUC_AnimalBusy".Translate(), null, MenuOptionPriority.Low));
                        return;
                    }
                    canMount = IsMountableUtility.IsMountable(target, out IsMountableUtility.Reason reason);

                    if (canMount)
                    {
                        Action action = delegate
                        {
                            Job jobRider = new Job(ResourceBank.JobDefOf.Mount, target);
                            jobRider.count = 1;
                            pawn.jobs.TryTakeOrderedJob(jobRider);
                        };
                        opts.Add(new FloatMenuOption("GUC_Mount".Translate() + " " + target.Name, action, MenuOptionPriority.Low));
                    }
                    else
                    {
                        if (reason == IsMountableUtility.Reason.NotInModOptions)
                        {
                            opts.Add(new FloatMenuOption("GUC_NotInModOptions".Translate(), null, MenuOptionPriority.Low));
                        }
                        else if (reason == IsMountableUtility.Reason.NotFullyGrown)
                        {
                            opts.Add(new FloatMenuOption("GUC_NotFullyGrown".Translate(), null, MenuOptionPriority.Low));
                        }
                        else if (reason == IsMountableUtility.Reason.NeedsTraining)
                        {
                            opts.Add(new FloatMenuOption("GUC_NeedsObedience".Translate(), null, MenuOptionPriority.Low));
                        }
                        else if (reason == IsMountableUtility.Reason.IsRoped)
                        {
                            opts.Add(new FloatMenuOption("GUC_IsRoped".Translate(), null, MenuOptionPriority.Low));
                        }
                        return;
                    }
                }
            }
            else if (target == pawnData.mount)
            {
                Action action = delegate { ResetPawnData(pawnData); };
                opts.Add(new FloatMenuOption("GUC_Dismount".Translate(), action, MenuOptionPriority.High));
            }
        }

        //[SyncMethod]
        static void ResetPawnData(ExtendedPawnData pawnData)
        {
            pawnData.Reset();
        }
    }
}