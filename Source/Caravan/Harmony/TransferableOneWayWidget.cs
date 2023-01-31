using GiddyUp.Storage;
using GiddyUp.Utilities;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
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
            float buttonWidth = 150f;

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

            return num - buttonWidth;
        }

        static void SetSelectedForCaravan(Pawn pawn, TransferableOneWay trad)
        {
            ExtendedPawnData pawnData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);

            if (trad.CountToTransfer == 0) //unset pawndata when pawn is not selected for caravan. 
            {
                pawnData.selectedForCaravan = false;
                if (pawnData.caravanMount != null)
                {
                    UnsetDataForRider(pawnData);
                }
                if (pawnData.caravanRider != null)
                {
                    UnsetDataForMount(pawnData);
                }
            }
            if(pawnData.caravanMount != null && (pawnData.caravanMount.Dead || pawnData.caravanMount.Downed))
            {
                UnsetDataForRider(pawnData);
            }

            if (trad.CountToTransfer > 0)
            {
                pawnData.selectedForCaravan = true;
            }
        }

        static void UnsetDataForRider(ExtendedPawnData pawnData)
        {
            GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawnData.caravanMount.thingIDNumber).caravanRider = null;
            pawnData.caravanMount = null;
        }

        static void UnsetDataForMount(ExtendedPawnData pawnData)
        {
            GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawnData.caravanRider.thingIDNumber).caravanMount = null;
            pawnData.caravanRider = null;
        }

        static void HandleAnimal(float num, Rect buttonRect, Pawn animal, List<Pawn> pawns, TransferableOneWay trad)
        {
            ExtendedPawnData animalData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(animal.thingIDNumber);
            Text.Anchor = TextAnchor.MiddleLeft;

            List<FloatMenuOption> list = new List<FloatMenuOption>();

            string buttonText = "GU_Car_Set_Rider".Translate();

            bool canMount = true;

            if (!animalData.selectedForCaravan)
            {
                buttonText = "GU_Car_AnimalNotSelected".Translate();
                canMount = false;
            }

            bool isMountable = IsMountableUtility.isMountable(animal, out IsMountableUtility.Reason reason);
            if (!isMountable)
            {
                if (reason == IsMountableUtility.Reason.NotFullyGrown)
                {
                    buttonText = "GU_Car_NotFullyGrown".Translate();
                    canMount = false;
                }
                if (reason == IsMountableUtility.Reason.NotInModOptions)
                {
                    buttonText = "GU_Car_NotInModOptions".Translate();
                    canMount = false;
                }
            }

            if (animalData.caravanRider != null)
            {
                ExtendedPawnData riderData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(animalData.caravanRider.thingIDNumber);
                if (riderData.selectedForCaravan)
                {
                    buttonText = animalData.caravanRider.Name.ToStringShort;
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
                        ExtendedPawnData pawnData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
                        if (!pawnData.selectedForCaravan)
                        {
                            list.Add(new FloatMenuOption(pawn.Name.ToStringShort + " (" + "GU_Car_PawnNotSelected".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            continue;
                        }

                        if (pawnData.caravanMount != null)
                        {
                            continue;
                        }
                        list.Add(new FloatMenuOption(pawn.Name.ToStringShort, delegate
                        {
                            {
                                SelectMountRider(animalData, pawnData, animal, pawn);
                                trad.CountToTransfer = -1; //Setting this to -1 will make sure total weight is calculated again. it's set back to 1 shortly after
                            }
                        }, MenuOptionPriority.High, null, null, 0f, null, null));
                    }
                }
                list.Add(new FloatMenuOption("GU_Car_No_Rider".Translate(), delegate
                {
                    {
                        ClearMountRider(animalData);
                        trad.CountToTransfer = -1; //Setting this to -1 will make sure total weight is calculated again. it's set back to 1 shortly after
                    }
                }, MenuOptionPriority.Low, null, null, 0f, null, null));
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }

        //[SyncMethod]
        static void SelectMountRider(ExtendedPawnData animalData, ExtendedPawnData pawnData, Pawn animal, Pawn pawn)
        {
            if (animalData.caravanRider != null)
            {
                ExtendedPawnData riderData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(animalData.caravanRider.thingIDNumber);
                riderData.caravanMount = null;
            }

            pawnData.caravanMount = animal;
            animalData.caravanRider = pawn;

            animalData.selectedForCaravan = true;
        }

        //[SyncMethod]
        static void ClearMountRider(ExtendedPawnData animalData)
        {
            if (animalData.caravanRider != null)
            {
                ExtendedPawnData riderData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(animalData.caravanRider.thingIDNumber);
                riderData.caravanMount = null;
            }
            animalData.caravanRider = null;

            animalData.selectedForCaravan = true;
        }

    }

    //This code makes sure total pack weight is refreshed after a rider is set for an animal. 
    //CountToTransfer with -1 is used as a flag here, indicating that weight should be recalculated. Unfortunately I couldn't come up with a cleaner way to do this without completely disabling caching. 
    [HarmonyPatch(typeof(TransferableOneWayWidget), nameof(TransferableOneWayWidget.FillMainRect))]
    static class TransferableOneWayWidget_FillMainRect
    {
        static void Postfix(TransferableOneWayWidget __instance, ref bool anythingChanged)
        {
            if (__instance.sections.Count < 4) return;

            List<TransferableOneWay> cachedTransferables = __instance.sections[3].cachedTransferables;
            if (cachedTransferables != null)
            {
                foreach (TransferableOneWay tow in cachedTransferables)
                {
                    Pawn towPawn = tow.AnyThing as Pawn;
                    if (towPawn == null)
                    {
                        continue;
                    }
                    if (tow.CountToTransfer == -1)
                    {
                        ExtendedPawnData PawnData = GiddyUp.Setup._extendedDataStorage.GetExtendedDataFor(towPawn.thingIDNumber);
                        if (PawnData.selectedForCaravan == true)
                        {
                            anythingChanged = true;
                            tow.CountToTransfer = 1;
                        }
                    }
                }
            }
        }
    }
}