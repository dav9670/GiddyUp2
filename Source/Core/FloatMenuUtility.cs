using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using static GiddyUp.IsMountableUtility;
using Settings = GiddyUp.ModSettings_GiddyUp;
//using Multiplayer.API;

namespace GiddyUp
{
    public static class GUC_FloatMenuUtility
    {
        public static void AddMountingOptions(Pawn animal, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (pawn.IsWorkTypeDisabledByAge(WorkTypeDefOf.Handling, out int ageNeeded)) return;
            var pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
            if (pawnData.mount == null)
            {
                if (animal.IsMountable(out Reason reason, pawn, true, true))
                {
                    Action action = delegate
                    {
                        Job jobRider = new Job(ResourceBank.JobDefOf.Mount, animal);
                        jobRider.count = 1;
                        pawn.jobs.TryTakeOrderedJob(jobRider);
                    };
                    opts.Add(new FloatMenuOption("GUC_Mount".Translate() + " " + animal.Name, action, MenuOptionPriority.Low));
                }
                else
                {
                    if (Settings.logging) Log.Message("[Giddy-up] " + pawn.Name.ToString() + " could not mount " + animal.thingIDNumber.ToString() + " because: " + reason.ToString());
                    switch (reason)
                    {
                        case Reason.NotAnimal: return;
                        case Reason.WrongFaction: return;
                        case Reason.IsBusy: opts.Add(new FloatMenuOption("GUC_AnimalBusy".Translate(), null, MenuOptionPriority.Low)); break;
                        case Reason.NotInModOptions: opts.Add(new FloatMenuOption("GUC_NotInModOptions".Translate(), null, MenuOptionPriority.Low)); break;
                        case Reason.NotFullyGrown: opts.Add(new FloatMenuOption("GUC_NotFullyGrown".Translate(), null, MenuOptionPriority.Low)); break;
                        case Reason.NeedsTraining: opts.Add(new FloatMenuOption("GUC_NeedsObedience".Translate(), null, MenuOptionPriority.Low)); break;
                        case Reason.IsRoped: opts.Add(new FloatMenuOption("GUC_IsRoped".Translate(), null, MenuOptionPriority.Low)); break;
                        default: return;
                    }
                }
            }
            else if (animal == pawnData.mount)
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