using GiddyUp;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
//using Multiplayer.API;

namespace GiddyUpCaravan.Harmony
{
    [HarmonyPatch(typeof(TransferableOneWayWidget), nameof(TransferableOneWayWidget.DoRow))]
    static class TransferableOneWayWidget_DoRow
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.caravansEnabled;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool flag = false;
            foreach (var code in instructions)
            {
                yield return code;

                if (code.OperandIs(AccessTools.Method(typeof(TooltipHandler), nameof(TooltipHandler.TipRegion), new Type[] { typeof(Rect), typeof(TipSignal) } ) ) )
                {
                    flag = true;
                }
                if (flag && code.opcode == OpCodes.Stloc_0)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); //Load instance
                    yield return new CodeInstruction(OpCodes.Ldloc_0); //load count local variable
                    yield return new CodeInstruction(OpCodes.Ldarg_1); //Load rectangle
                    yield return new CodeInstruction(OpCodes.Ldarg_2); //Load trad
                    yield return new CodeInstruction(OpCodes.Call, typeof(TransferableOneWayWidget_DoRow).GetMethod(nameof(AddMountSelector)));
                    yield return new CodeInstruction(OpCodes.Stloc_0); //store count local variable
                    flag = false;
                }
            }
        }
        public static float AddMountSelector(TransferableOneWayWidget widget, float num, Rect rect, TransferableOneWay trad)
        {
            float buttonWidth = 75f;

            if (trad.AnyThing is not Pawn pawn) return num; //not an animal, return; 
            
            Rect buttonRect = new Rect(num - buttonWidth, 0f, buttonWidth, rect.height);
            List<TransferableOneWay> cachedTransferables = widget.sections[0].cachedTransferables;
            List<Pawn> pawns = new List<Pawn>();

            if (cachedTransferables != null)
            {
                foreach (TransferableOneWay tow in cachedTransferables)
                {
                    Pawn towPawn = tow.AnyThing as Pawn;
                    if (towPawn != null) pawns.Add(tow.AnyThing as Pawn);
                }
                //It quacks like a duck, so it is one!
            }
            SetSelectedForCaravan(pawn, trad);
            if (pawn.RaceProps.Animal && pawns.Count > 0)
            {
                HandleAnimal(num, buttonRect, pawn, pawns, trad);
            }
            else return num;

            return num - (buttonWidth - 25f);
        }
        static void SetSelectedForCaravan(Pawn pawn, TransferableOneWay trad)
        {
            ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
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
        static void UnsetDataForRider(ExtendedPawnData pawnData)
        {
            ExtendedDataStorage.GUComp[pawnData.reservedMount.thingIDNumber].reservedBy = null;
            pawnData.ReserveMount = null;
        }
        static void UnsetDataForMount(ExtendedPawnData pawnData)
        {
            ExtendedDataStorage.GUComp[pawnData.reservedBy.thingIDNumber].ReserveMount = null;
            pawnData.reservedBy = null;
        }
        static void HandleAnimal(float num, Rect buttonRect, Pawn animal, List<Pawn> pawns, TransferableOneWay trad)
        {
            ExtendedPawnData animalData = ExtendedDataStorage.GUComp[animal.thingIDNumber];
            Text.Anchor = TextAnchor.MiddleLeft;

            List<FloatMenuOption> list = new List<FloatMenuOption>();

            string buttonText = "GU_Car_Set_Rider".Translate();

            bool canMount = true;

            if (!animalData.selectedForCaravan)
            {
                buttonText = "";
                canMount = false;
            }

            bool isMountable = animal.IsMountable(out IsMountableUtility.Reason reason, null);
            if (!isMountable && (reason == IsMountableUtility.Reason.NotInModOptions || reason == IsMountableUtility.Reason.NotFullyGrown))
            {
                buttonText = "";
                canMount = false;
            }

            if (animalData.reservedBy != null)
            {
                ExtendedPawnData riderData = ExtendedDataStorage.GUComp[animalData.reservedBy.thingIDNumber];
                if (riderData.selectedForCaravan)
                {
                    buttonText = animalData.reservedBy.Name.ToStringShort;
                }
            }
            if (!canMount)
            {
                Widgets.ButtonText(buttonRect, buttonText, false, false, false);
            }
            else if (Widgets.ButtonText(buttonRect, buttonText, true, false, true))
            {
                foreach (Pawn pawn in pawns)
                {
                    if (pawn.IsColonist)
                    {
                        ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
                        if (!pawnData.selectedForCaravan)
                        {
                            list.Add(new FloatMenuOption(pawn.Name.ToStringShort + " (" + "GU_Car_PawnNotSelected".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            continue;
                        }

                        if (pawnData.reservedMount != null)
                        {
                            continue;
                        }
                        if (pawn.IsWorkTypeDisabledByAge(WorkTypeDefOf.Handling, out int age))
                        {
                            list.Add(new FloatMenuOption(pawn.Name.ToStringShort + " (" + "GU_Car_TooYoung".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            continue;
                        }
                        list.Add(new FloatMenuOption(pawn.Name.ToStringShort, delegate
                        {
                            {
                                SelectMountRider(animalData, pawnData, animal, pawn);
                                trad.CountToTransfer = 1; //Setting this to -1 will make sure total weight is calculated again. it's set back to 1 shortly after
                            }
                        }, MenuOptionPriority.High, null, null, 0f, null, null));
                    }
                }
                list.Add(new FloatMenuOption("GU_Car_No_Rider".Translate(), delegate
                {
                    {
                        ClearMountRider(animalData);
                        trad.CountToTransfer = 1; //Setting this to -1 will make sure total weight is calculated again. it's set back to 1 shortly after
                    }
                }, MenuOptionPriority.Low, null, null, 0f, null, null));
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }
        //[SyncMethod]
        static void SelectMountRider(ExtendedPawnData animalData, ExtendedPawnData pawnData, Pawn animal, Pawn pawn)
        {
            if (animalData.reservedBy != null)
            {
                ExtendedPawnData riderData = ExtendedDataStorage.GUComp[animalData.reservedBy.thingIDNumber];
                riderData.ReserveMount = null;
            }

            pawnData.reservedMount = animal;
            animalData.reservedBy = pawn;

            animalData.selectedForCaravan = true;
        }
        //[SyncMethod]
        static void ClearMountRider(ExtendedPawnData animalData)
        {
            if (animalData.reservedBy != null)
            {
                ExtendedPawnData riderData = ExtendedDataStorage.GUComp[animalData.reservedBy.thingIDNumber];
                riderData.ReserveMount = null;
            }
            animalData.ReserveMount = null;

            animalData.selectedForCaravan = true;
        }
    }
}