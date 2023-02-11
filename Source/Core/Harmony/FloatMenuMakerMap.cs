using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static GiddyUp.IsMountableUtility;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp.Harmony
{
	[HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.ChoicesAtFor))]
	static class FloatMenuMakerMap_ChoicesAtFor
	{
		static bool Prepare()
		{
			return Settings.rideAndRollEnabled;
		}
		static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> __result)
		{
			if (DebugSettings.godMode)
            {
                var godModeTargetting = new TargetingParameters() 
                {
                    canTargetAnimals = true, 
                    canTargetHumans = true, 
                    canTargetPawns = true,
                    validator = null,
                    onlyTargetColonists = false,
                    mustBeSelectable = false
                };
                foreach (LocalTargetInfo current in GenUI.TargetsAt(clickPos, godModeTargetting, true))
                {
                    if (current.Thing is Pawn target) FloatMenuUtility.AddMountingOptions(target, pawn, __result);
                }
				return;
            }
			foreach (LocalTargetInfo current in GenUI.TargetsAt(clickPos, TargetingParameters.ForAttackHostile(), true))
			{
				if (current.Thing is Pawn target && !pawn.Drafted && target.RaceProps.Animal) FloatMenuUtility.AddMountingOptions(target, pawn, __result);
			}
		}
	}

	[HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.AddDraftedOrders), new Type[] { typeof(Vector3), typeof(Pawn), typeof(List<FloatMenuOption>), typeof(bool) })]
	static class Patch_AddDraftedOrders
	{
		static bool Prepare()
		{
			return Settings.battleMountsEnabled;
		}
		static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
		{
			foreach (LocalTargetInfo current in GenUI.TargetsAt(clickPos, TargetingParameters.ForAttackHostile(), true))
			{
				if (current.Thing is Pawn target) FloatMenuUtility.AddMountingOptions(target, pawn, opts);
			}
		}
	}

	static class FloatMenuUtility
	{
		public static void AddMountingOptions(Pawn animal, Pawn pawn, List<FloatMenuOption> opts)
		{
			if (pawn.IsWorkTypeDisabledByAge(WorkTypeDefOf.Handling, out int ageNeeded)) return;
			var pawnData = pawn.GetGUData();
			if (pawnData.mount == null)
			{
				if (animal.IsMountable(out Reason reason, pawn, true, true))
				{
					opts.Add(new FloatMenuOption("GUC_Mount".Translate() + " " + animal.Name, () => pawn.GoMount(animal, MountUtility.GiveJobMethod.Try), MenuOptionPriority.Low));
				}
				else if (DebugSettings.godMode)
				{
					opts.Add(new FloatMenuOption("GUC_Mount_GodMode".Translate() + " " + animal.Name, () => pawn.GoMount(animal, MountUtility.GiveJobMethod.Try), MenuOptionPriority.Low));
				}
				else
				{
					if (Settings.logging) Log.Message("[Giddy-Up] " + pawn.Name.ToString() + " could not mount " + animal.thingIDNumber.ToString() + " because: " + reason.ToString());
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
				Action action = delegate { pawn.Dismount(animal, pawnData, true); };
				opts.Add(new FloatMenuOption("GUC_Dismount".Translate(), action, MenuOptionPriority.High));
			}
		}
	}
}
