using GiddyUp;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;
using static GiddyUp.IsMountableUtility;

//using Multiplayer.API;

namespace GiddyUpCaravan.Harmony;

[HarmonyPatch(typeof(TransferableOneWayWidget), nameof(TransferableOneWayWidget.DoRow))]
internal static class Patch_TransferableOneWayWidget
{
    private static bool Prepare()
    {
        return Settings.caravansEnabled;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var flag = false;
        foreach (var code in instructions)
        {
            yield return code;

            if (code.OperandIs(AccessTools.Method(typeof(TooltipHandler), nameof(TooltipHandler.TipRegion),
                    new Type[] { typeof(Rect), typeof(TipSignal) }))) flag = true;
            if (flag && code.opcode == OpCodes.Stloc_0)
            {
                //TODO: Improve this to be less fragile
                yield return new CodeInstruction(OpCodes.Ldarg_0); //Load instance
                yield return new CodeInstruction(OpCodes.Ldloc_0); //load count local variable
                yield return new CodeInstruction(OpCodes.Ldarg_1); //Load rectangle
                yield return new CodeInstruction(OpCodes.Ldarg_2); //Load trad
                yield return new CodeInstruction(OpCodes.Call,
                    typeof(Patch_TransferableOneWayWidget).GetMethod(nameof(AddMountSelector)));
                yield return new CodeInstruction(OpCodes.Stloc_0); //store count local variable
                flag = false;
            }
        }
    }

    public static float AddMountSelector(TransferableOneWayWidget widget, float num, Rect rect, TransferableOneWay trad)
    {
        var buttonWidth = 75f;

        if (trad.AnyThing is not Pawn pawn) return num; //not an animal, return; 

        var buttonRect = new Rect(num - buttonWidth, 0f, buttonWidth, rect.height);
        var cachedTransferables = new List<TransferableOneWay>();
        foreach (var section in widget.sections)
        {
            var title = section.title;
            //This is mainly for mods that add new sections such as Colony Groups
            if (title != "Capture" && title != "Prisoners" && title != "Animals" && title != "Mechanoids")
                cachedTransferables.AddRange(section.cachedTransferables);
        }

        var pawns = new List<Pawn>();

        if (cachedTransferables != null)
            foreach (var tow in cachedTransferables)
            {
                var towPawn = tow.AnyThing as Pawn;
                if (towPawn != null) pawns.Add(tow.AnyThing as Pawn);
            }

        //It quacks like a duck, so it is one!
        SetSelectedForCaravan(pawn, trad);
        if (pawn.RaceProps.Animal && pawns.Count > 0)
            HandleAnimal(num, buttonRect, pawn, pawns, trad);
        else return num;

        return num - (buttonWidth - 25f);
    }

    private static void SetSelectedForCaravan(Pawn pawn, TransferableOneWay trad)
    {
        var pawnData = pawn.GetGUData();
        var reservedMount = pawnData.reservedMount;

        if (trad.CountToTransfer == 0) //unset pawndata when pawn is not selected for caravan. 
        {
            pawnData.selectedForCaravan = false;
            if (reservedMount != null) UnsetDataForRider(pawnData);
            if (pawnData.reservedBy != null) UnsetDataForMount(pawnData);
        }

        if (reservedMount != null && (reservedMount.Dead || reservedMount.Downed)) UnsetDataForRider(pawnData);

        if (trad.CountToTransfer > 0) pawnData.selectedForCaravan = true;
    }

    private static void UnsetDataForRider(ExtendedPawnData pawnData)
    {
        pawnData.reservedMount.GetGUData().ReservedBy = null;
        pawnData.ReservedMount = null;
    }

    private static void UnsetDataForMount(ExtendedPawnData pawnData)
    {
        pawnData.reservedBy.GetGUData().ReservedMount = null;
        pawnData.ReservedBy = null;
    }

    private static void HandleAnimal(float num, Rect buttonRect, Pawn animal, List<Pawn> pawns, TransferableOneWay trad)
    {
        var animalData = animal.GetGUData();
        Text.Anchor = TextAnchor.MiddleLeft;

        var list = new List<FloatMenuOption>();

        string buttonText;
        bool canMount;

        if (!animalData.selectedForCaravan ||
            (!animal.IsMountable(out var reason, null) &&
             (reason == Reason.NotInModOptions || reason == Reason.NotFullyGrown)))
        {
            buttonText = "";
            canMount = false;
        }
        else
        {
            buttonText = animalData.reservedBy != null && animalData.reservedBy.GetGUData().selectedForCaravan
                ? animalData.reservedBy.Name.ToStringShort
                : "GU_Car_Set_Rider".Translate();
            canMount = true;
        }

        if (!canMount)
        {
            Widgets.ButtonText(buttonRect, buttonText, false, false, false);
        }
        else if (Widgets.ButtonText(buttonRect, buttonText, true, false, true))
        {
            var length = pawns.Count;
            for (var i = 0; i < length; i++)
            {
                var rider = pawns[i];
                if (rider.IsColonist)
                {
                    var pawnData = rider.GetGUData();
                    if (!pawnData.selectedForCaravan)
                    {
                        list.Add(new FloatMenuOption(
                            rider.Name.ToStringShort + " (" + "GU_Car_PawnNotSelected".Translate() + ")", null,
                            MenuOptionPriority.Default, null, null, 0f, null, null));
                        continue;
                    }

                    if (pawnData.reservedMount != null) continue;
                    if (!rider.IsCapableOfRiding(out var riderReason))
                    {
                        string cannotRideReason;
                        if (riderReason == Reason.TooYoung)
                            cannotRideReason = rider.Name.ToStringShort + " (" + "GU_TooYoung".Translate() + ")";
                        else cannotRideReason = rider.Name.ToStringShort + " (" + "GU_CannotRide".Translate() + ")";

                        list.Add(new FloatMenuOption(cannotRideReason, null, MenuOptionPriority.Default, null, null, 0f,
                            null, null));
                        continue;
                    }

                    if (rider.IsTooHeavy(animal))
                    {
                        list.Add(new FloatMenuOption(rider.Name.ToStringShort + " (" + "GUC_TooHeavy".Translate() + ")",
                            null, MenuOptionPriority.Default, null, null, 0f, null, null));
                        continue;
                    }

                    list.Add(new FloatMenuOption(rider.Name.ToStringShort, delegate
                    {
                        {
                            SelectMountRider(animalData, pawnData, animal, rider);
                            trad.CountToTransfer = 1;
                        }
                    }, MenuOptionPriority.High, null, null, 0f, null, null));
                }
            }

            //Selected created without rider
            list.Add(new FloatMenuOption("GU_Car_No_Rider".Translate(), delegate
            {
                {
                    ClearMountRider(animalData);
                    trad.CountToTransfer = 1;
                }
            }, MenuOptionPriority.Low, null, null, 0f, null, null));

            //Print list
            Find.WindowStack.Add(new FloatMenu(list));
        }
    }

    //[SyncMethod]
    private static void SelectMountRider(ExtendedPawnData animalData, ExtendedPawnData pawnData, Pawn animal, Pawn pawn)
    {
        if (animalData.reservedBy != null) animalData.reservedBy.GetGUData().ReservedMount = null;

        pawnData.ReservedMount = animal;
        animalData.ReservedBy = pawn;

        animalData.selectedForCaravan = true;
    }

    //[SyncMethod]
    private static void ClearMountRider(ExtendedPawnData animalData)
    {
        if (animalData.reservedBy != null)
        {
            var riderData = animalData.reservedBy.GetGUData();
            riderData.ReservedMount = null;
        }

        animalData.ReservedBy = null;
        animalData.selectedForCaravan = true;
    }
}

[HarmonyPatch(typeof(TransferableUtility), nameof(TransferableUtility.TransferAsOne))]
internal static class Patch_TransferableUtility
{
    private static bool Prepare()
    {
        return Settings.caravansEnabled;
    }

    private static bool Postfix(bool __result, Thing a, Thing b)
    {
        if (__result && a.def.category == ThingCategory.Pawn && b.def.category == ThingCategory.Pawn &&
            (IsMountableUtility.IsEverMountable(a as Pawn) || IsMountableUtility.IsEverMountable(b as Pawn)))
            return false;
        return __result;
    }
}