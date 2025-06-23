using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using static GiddyUp.IsMountableUtility;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp.Harmony;

public class FloatMenuOptionProvider_RidingOptions : FloatMenuOptionProvider
{
    public override bool Drafted => true;

    public override bool Undrafted => true;

    public override bool Multiselect => false;

    public override bool RequiresManipulation => true;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (!Settings.rideAndRollEnabled && !Settings.battleMountsEnabled)
            return [];

        var options = new List<FloatMenuOption>();

        foreach (var selectedPawn in context.ValidSelectedPawns)
            if (clickedPawn.RaceProps.Animal && ((!selectedPawn.Drafted && Settings.rideAndRollEnabled) ||
                                                 (selectedPawn.Drafted && Settings.battleMountsEnabled)))
                FloatMenuUtility.AddMountingOptions(clickedPawn, selectedPawn, options);

        return options;
    }
}

internal static class FloatMenuUtility
{
    public static bool AddMountingOptions(Pawn animal, Pawn pawn, List<FloatMenuOption> opts)
    {
        var pawnData = pawn.GetGUData();
        //Right click to dismount...
        if (animal == pawnData.mount)
        {
            if (animal.RaceProps.Roamer && AnimalPenUtility.GetCurrentPenOf(animal, true) == null)
                opts.GenerateFloatMenuOption("GUC_DismountWithoutHitching".Translate(), true,
                    () => pawn.Dismount(animal, pawnData, true, ropeIfNeeded: false));
            return opts.GenerateFloatMenuOption("GUC_Dismount".Translate(), true,
                () => pawn.Dismount(animal, pawnData, true));
        }
        //Right click to mount...
        else
        {
            pawn.IsCapableOfRiding(out var riderReason);
            if (animal.IsMountable(out var reason, pawn, true, true) && riderReason == Reason.False)
            {
                //New mount
                if (pawnData.mount == null)
                    return opts.GenerateFloatMenuOption("GUC_Mount".Translate(), true,
                        () => pawn.GoMount(animal, MountUtility.GiveJobMethod.Try));
                //Switch mount
                else
                    return opts.GenerateFloatMenuOption("GUC_SwitchMount".Translate(), true, delegate
                    {
                        pawn.Dismount(pawnData.mount, pawnData, true);
                        pawn.GoMount(animal, MountUtility.GiveJobMethod.Try);
                    });
            }
            /*
            else if (DebugSettings.godMode)
            {
                return opts.GenerateFloatMenuOption("GUC_Mount_GodMode".Translate(), true, () => pawn.GoMount(animal, MountUtility.GiveJobMethod.Try));
            }*/
            else
            {
                if (Settings.logging)
                    Log.Message("[Giddy-Up] " + pawn.Name.ToString() + " could not mount " +
                                animal.thingIDNumber.ToString() + " because: " + reason.ToString());
                switch (reason)
                {
                    case Reason.NotAnimal: return false;
                    case Reason.WrongFaction: return false;
                    case Reason.IsBusy: return opts.GenerateFloatMenuOption("GUC_AnimalBusy".Translate());
                    case Reason.NotInModOptions: return opts.GenerateFloatMenuOption("GUC_NotInModOptions".Translate());
                    case Reason.NotFullyGrown: return opts.GenerateFloatMenuOption("GUC_NotFullyGrown".Translate());
                    case Reason.NeedsTraining: return opts.GenerateFloatMenuOption("GUC_NeedsObedience".Translate());
                    case Reason.IsRoped: return opts.GenerateFloatMenuOption("GUC_IsRoped".Translate());
                    case Reason.IsPoorCondition: return opts.GenerateFloatMenuOption("GUC_IsPoorCondition".Translate());
                    case Reason.TooHeavy: return opts.GenerateFloatMenuOption("GUC_TooHeavy".Translate());
                    default:
                    {
                        if (riderReason == Reason.TooYoung)
                            return opts.GenerateFloatMenuOption("GU_Car_TooYoung".Translate());
                        else if (riderReason == Reason.IncompatibleEquipment)
                            return opts.GenerateFloatMenuOption("GU_IncompatibleEquipment".Translate());
                        return false;
                    }
                }
            }
        }
    }

    private static bool GenerateFloatMenuOption(this List<FloatMenuOption> list, string text, bool prefixType = false,
        Action action = null)
    {
        if (!prefixType) text = "GUC_CannotMount".Translate() + text;
        list.Add(new FloatMenuOption(text, action, MenuOptionPriority.Low));
        return true;
    }
}