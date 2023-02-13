using GiddyUpRideAndRoll;
using GiddyUpCaravan;
using GiddyUp.Jobs;
using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using static GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp
{
	[StaticConstructorOnStartup]
	public static class Setup
	{
		public static ThingDef[] allAnimals; //Only used during setup and for the mod options UI
		static HashSet<ushort> defEditLedger = new HashSet<ushort>();
		static HashSet<int> patchLedger = new HashSet<int>();
		//TODO: could make these not static to save some memory
		static SimpleCurve sizeFactor = new SimpleCurve
		{
			{ new CurvePoint(1.2f, 0.9f) },
			{ new CurvePoint(1.5f, 1f) },
			{ new CurvePoint(2.4f, 1.15f) },
			{ new CurvePoint(4f, 1.25f) }
		};
		static SimpleCurve speedFactor = new SimpleCurve
		{
			{ new CurvePoint(4.3f, 1f) },
			{ new CurvePoint(5.8f, 1.2f) },
			{ new CurvePoint(8f, 1.4f) }
		};
		static SimpleCurve valueFactor = new SimpleCurve
		{
			{ new CurvePoint(300f, 1f) },
			{ new CurvePoint(550f, 1.15f) },
			{ new CurvePoint(5000f, 1.3f) }
		};
		static SimpleCurve wildnessFactor = new SimpleCurve
		{
			{ new CurvePoint(0.2f, 1f) },
			{ new CurvePoint(0.6f, 0.9f) },
			{ new CurvePoint(1f, 0.85f) }
		};
	
		static Setup()
		{
			var harmony = new HarmonyLib.Harmony("GiddyUp");
			harmony.PatchAll();

			BuildCache();
			BuildAnimalBiomeCache();
			if (!rideAndRollEnabled) RemoveRideAndRoll();
			if (!caravansEnabled) RemoveCaravans();

			var modExt = ResourceBank.JobDefOf.Mounted.GetModExtension<AllowedJobDefs>();
			if (modExt != null)
			{
				JobDriver_Mounted.allowedJobs = modExt.allowedJobDefs.ToHashSet();
			}
			else JobDriver_Mounted.allowedJobs = new HashSet<JobDef>();

			if (noMountedHunting) JobDriver_Mounted.allowedJobs.Add(JobDefOf.Hunt);
			ProcessPawnKinds(harmony);
			if (disableSlavePawnColumn) DefDatabase<PawnTableDef>.GetNamed("Animals").columns.RemoveAll(x => x.defName == "MountableBySlaves");

			//VE Classical mod support
			var type = HarmonyLib.AccessTools.TypeByName("AnimalBehaviours.AnimalCollectionClass");
			if (type != null) ExtendedDataStorage.nofleeingAnimals = HarmonyLib.Traverse.Create(type).Field("nofleeing_animals")?.GetValue<HashSet<Thing>>();
		}
		//Responsible for caching which animals are mounted, draw layering behavior, and calling caravan speed bonuses
		public static void BuildCache()
		{
			//Setup collections
			List<ThingDef> workingList = new List<ThingDef>();
			if (invertMountingRules == null) invertMountingRules = new HashSet<string>();
			mountableCache = new HashSet<ushort>();

			if (invertDrawRules == null) invertDrawRules = new HashSet<string>();
			drawRulesCache = new HashSet<ushort>();

			var list = DefDatabase<ThingDef>.AllDefsListForReading;
			var length = list.Count;
			for (int i = 0; i < length; i++)
			{
				var def = list[i];
				if (def.race != null && def.race.Animal)
				{
					workingList.Add(def);

					bool setting = def.race.baseBodySize > ResourceBank.defaultSizeThreshold;
					if (def.HasModExtension<NotMountable>()) setting = false;
					else if (def.HasModExtension<Mountable>()) setting = true;
					if (invertMountingRules.Contains(def.defName)) setting = !setting; //Player customization says to invert rule.
					
					if (setting)
					{
						mountableCache.Add(def.shortHash);
						CalculateCaravanSpeed(def);
					}
					else mountableCache.Remove(def.shortHash);

					//Handle the draw front/behind draw instruction cache
					setting = def.HasModExtension<DrawInFront>();
					if (invertDrawRules.Contains(def.defName)) setting = !setting;
					
					if (setting) drawRulesCache.Add(def.shortHash);
					else drawRulesCache.Remove(def.shortHash);
				}
			}
			workingList.SortBy(x => x.label);
			allAnimals = workingList.ToArray();
		}
		//Responsible for setting up the draw offsets and custom stat overrides
		public static void ProcessPawnKinds(HarmonyLib.Harmony harmony = null)
		{
			bool newEntries = false;
			bool usingCustomStats = false;
			if (offsetCache == null) offsetCache = new Dictionary<string, float>();
			var list = DefDatabase<PawnKindDef>.AllDefsListForReading;
			var length = list.Count;
			for (int i = 0; i < length; i++)
			{
				var pawnKindDef = list[i];
				if (pawnKindDef.race == null) continue;
				if (!usingCustomStats && pawnKindDef.HasModExtension<CustomStats>()) usingCustomStats = true;

				//Only process animals that can be mounted
				if (mountableCache.Contains(pawnKindDef.race.shortHash))
				{
					//Determine which life stages are considered mature enough to ride
					var lifeStages = pawnKindDef.lifeStages;
					var lifeIndexes = lifeStages?.Count;
					AllowedLifeStages customLifeStages;
					if (lifeIndexes > 0) customLifeStages = pawnKindDef.race.GetModExtension<AllowedLifeStages>();
					else customLifeStages = null;

					//Go through each life stage for this animal
					for (int lifeIndex = 0; lifeIndex < lifeIndexes; lifeIndex++)
					{
						//Convert the def and age into a key string used for storage between sessions
						if (lifeIndex != lifeIndexes -1 && (customLifeStages == null || !customLifeStages.IsAllowedAge(lifeIndex))) continue;
						string key = TextureUtility.FormatKey(pawnKindDef, lifeIndex);

						//Skip if already set
						if (offsetCache.ContainsKey(key)) continue;

						//Build out...
						var offset = TextureUtility.SetDrawOffset(lifeStages[lifeIndex]);
						offsetCache.Add(key, offset);
						newEntries = true;
					}
				}
			}

			//Write to settings file
			if (newEntries) LoadedModManager.GetMod<Mod_GiddyUp>().modSettings.Write();
		
			//Only bother applying this harmony patch if using a mod that utilizes this extension
			if (usingCustomStats && !patchLedger.Add(1))
			{
				harmony.Patch(HarmonyLib.AccessTools.Method(typeof(ArmorUtility), nameof(ArmorUtility.ApplyArmor) ), 
						postfix: new HarmonyLib.HarmonyMethod(typeof(Harmony.Patch_ApplyArmor), nameof(Harmony.Patch_ApplyArmor.Postfix)));
			} 
		}
		//Processes biome information to determine where animals come from, used for NPC mount spawning
		static void BuildAnimalBiomeCache()
		{
			var biomeDefs = DefDatabase<BiomeDef>.AllDefsListForReading;
			var length = biomeDefs.Count;
			for (int i = 0; i < length; i++)
			{
				var biomeDef = biomeDefs[i];
				foreach(PawnKindDef animalKind in biomeDef.AllWildAnimals) MountUtility.allWildAnimals.Add(animalKind);
			}
			
			var pawnKindDefs = DefDatabase<PawnKindDef>.AllDefsListForReading;
			length = pawnKindDefs.Count;
			for (int i = 0; i < length; i++)
			{
				var pawnKindDef = pawnKindDefs[i];
				if (pawnKindDef.RaceProps != null && pawnKindDef.RaceProps.wildness <= 0.6f && pawnKindDef.race != null && pawnKindDef.race.tradeTags != null &&
					 (pawnKindDef.race.tradeTags.Contains("AnimalFighter") || pawnKindDef.race.tradeTags.Contains("AnimalFarm") ))
				{
					MountUtility.allDomesticAnimals.Add(pawnKindDef);
				}
			}
		}
		//ToDo? It may be possible to fold this into th BuildCache method
		public static void RebuildInversions()
		{
			//Reset
			invertMountingRules = new HashSet<string>();
			invertDrawRules = new HashSet<string>();

			foreach (var animalDef in Setup.allAnimals)
			{
				var hash = animalDef.shortHash;
				//Search for abnormalities, meaning the player wants to invert the rules
				if (animalDef.HasModExtension<NotMountable>())
				{
					if (mountableCache.Contains(hash)) invertMountingRules.Add(animalDef.defName);
				}
				else if (animalDef.HasModExtension<Mountable>())
				{
					if (!mountableCache.Contains(hash)) invertMountingRules.Add(animalDef.defName);
				}
				else if (animalDef.race.baseBodySize <= ResourceBank.defaultSizeThreshold)
				{
					if (mountableCache.Contains(hash)) invertMountingRules.Add(animalDef.defName);
				}
				else
				{
					if (!mountableCache.Contains(hash)) invertMountingRules.Add(animalDef.defName);
				}

				//And now draw rules
				bool drawFront = false;
				var modExt = animalDef.GetModExtension<DrawInFront>();
				if (modExt != null) drawFront = true;
				
				if (drawFront && !drawRulesCache.Contains(hash)) invertDrawRules.Add(animalDef.defName);
				else if (!drawFront && drawRulesCache.Contains(hash)) invertDrawRules.Add(animalDef.defName);
			}
		}
		static void RemoveRideAndRoll()
		{
			//Remove jobs
			DefDatabase<JobDef>.Remove(ResourceBank.JobDefOf.WaitForRider);

			//Remove pawn columns (UI icons in the pawn table)
			DefDatabase<PawnTableDef>.GetNamed("Animals").columns.RemoveAll(x => x.defName == "MountableByColonists" || x.defName == "MountableBySlaves");
			
			//Remove area designators
			var designationCategoryDef = DefDatabase<DesignationCategoryDef>.GetNamed("Zone");
			designationCategoryDef.specialDesignatorClasses.RemoveAll(x => 
				x == typeof(Designator_GU_DropAnimal_Expand) ||
				x == typeof(Designator_GU_DropAnimal_Clear) ||
				x == typeof(Designator_GU_NoMount_Expand) ||
				x == typeof(Designator_GU_NoMount_Clear)
			 );
			var workingList = new List<Designator>(designationCategoryDef.resolvedDesignators);
			foreach (var designator in workingList)
			{
				if (designator is Designator_GU_DropAnimal_Expand ||
					designator is Designator_GU_DropAnimal_Clear || 
					designator is Designator_GU_NoMount_Expand || 
					designator is Designator_GU_NoMount_Clear) designationCategoryDef.resolvedDesignators.Remove(designator);
			}
		}
		static void RemoveCaravans()
		{            
			//Remove area designators
			var designationCategoryDef = DefDatabase<DesignationCategoryDef>.GetNamed("Zone");
			designationCategoryDef.specialDesignatorClasses.RemoveAll(x => 
				x == typeof(Designator_GU_DropAnimal_NPC_Clear) ||
				x == typeof(Designator_GU_DropAnimal_NPC_Expand)
			 );
			var workingList = new List<Designator>(designationCategoryDef.resolvedDesignators);
			foreach (var designator in workingList)
			{
				if (designator is Designator_GU_DropAnimal_NPC_Clear ||
					designator is Designator_GU_DropAnimal_NPC_Expand) designationCategoryDef.resolvedDesignators.Remove(designator);
			}
		}
		public static void CalculateCaravanSpeed(ThingDef def, bool check = false)
		{
			//Horse		2.4 size	5.8 speed	packAnimal	550 value	0.35 wildeness	= 1.6
			//Thrumbo	4.0 size	5.5 speed	!packAnimal	4000 value	0.985 wildness	= 1.5
			//Dromedary	2.1 size	4.3 speed	packAnimal	300 value	0.25 wildeness	= 1.3

			//Muffalo	2.4 size	4.5 speed	packAnimal	300 value	0.6 wildness	= ???
									
			float speed;
			
			//This would pass if the animal has an XML-defined bonus that we didn't apply, leave it alone
			if (def.StatBaseDefined(StatDefOf.CaravanRidingSpeedFactor) && !defEditLedger.Contains(def.shortHash)) return;
			//This would pass if mod options are changed, the mount is no longer rideable, and it was once given a bonus
			else if (check && !mountableCache.Contains(def.shortHash) && defEditLedger.Contains(def.shortHash))
			{
				defEditLedger.Remove(def.shortHash);
				speed = 1f;
			}
			//Give a the bonus
			else if (giveCaravanSpeed)
			{
				defEditLedger.Add(def.shortHash);
				speed = sizeFactor.Evaluate(def.race.baseBodySize) * 
					speedFactor.Evaluate(def.GetStatValueAbstract(StatDefOf.MoveSpeed)) * 
					valueFactor.Evaluate(def.BaseMarketValue) * 
					wildnessFactor.Evaluate(def.race.wildness) * 
					(def.race.packAnimal ? 1.1f : 0.95f);
				if (speed < 1.00001f) speed = 1.00001f;
			}
			//Don't give a bonus and instead just set the value to be above 1f so the game thinks it's a rideable mount on the caravan UI, but low enough to render as 100%
			else
			{
				defEditLedger.Remove(def.shortHash);
				speed = speed = 1.00001f;
			}
			StatUtility.SetStatValueInList(ref def.statBases, StatDefOf.CaravanRidingSpeedFactor, speed);
		}
	}
	public class Mod_GiddyUp : Mod
	{
		public Mod_GiddyUp(ModContentPack content) : base(content)
		{
			base.GetSettings<ModSettings_GiddyUp>();
		}
		public override void DoSettingsWindowContents(Rect inRect)
		{
			//========Setup tabs=========
			GUI.BeginGroup(inRect);
			var tabs = new List<TabRecord>();
			tabs.Add(new TabRecord("GUC_Core_Tab".Translate(), delegate { selectedTab = SelectedTab.core; }, selectedTab == SelectedTab.core || selectedTab == SelectedTab.bodySize || selectedTab == SelectedTab.drawBehavior));
			tabs.Add(new TabRecord("GUC_RnR_Tab".Translate(), delegate { selectedTab = SelectedTab.rnr; }, selectedTab == SelectedTab.rnr));
			tabs.Add(new TabRecord("GUC_BattleMounts_Tab".Translate(), delegate { selectedTab = SelectedTab.battlemounts; }, selectedTab == SelectedTab.battlemounts));
			tabs.Add(new TabRecord("GUC_Caravans_Tab".Translate(), delegate { selectedTab = SelectedTab.caravans; }, selectedTab == SelectedTab.caravans));

			Rect rect = new Rect(0f, 32f, inRect.width, inRect.height - 32f);
			Widgets.DrawMenuSection(rect);
			TabDrawer.DrawTabs(new Rect(0f, 32f, inRect.width, Text.LineHeight), tabs);

			if (selectedTab == SelectedTab.core || selectedTab == SelectedTab.bodySize || selectedTab == SelectedTab.drawBehavior) DrawCore();
			else if (selectedTab == SelectedTab.rnr) DrawRnR();
			else if (selectedTab == SelectedTab.battlemounts) DrawBattleMounts();
			else DrawCaravan();
			GUI.EndGroup();
			
			void DrawRnR()
			{
				Listing_Standard options = new Listing_Standard();   
				options.Begin(rect.ContractedBy(15f));

				options.CheckboxLabeled("GU_Enable_RnR".Translate(), ref rideAndRollEnabled, "GU_Enable_RnR_Description".Translate());
				if (rideAndRollEnabled)
				{
					options.Gap();
					options.GapLine(); //=============================
					options.Gap();
					
					options.Label("GU_RR_MinAutoMountDistance_Title".Translate("0", "500", "16", minAutoMountDistance.ToString()), -1f, "GU_RR_MinAutoMountDistance_Description".Translate());
					minAutoMountDistance = (int)options.Slider(minAutoMountDistance, 0f, 500f);

					options.Label("GU_RR_AutoHitchDistance_Title".Translate("0", "200", "50", autoHitchDistance.ToString()), -1f, "GU_RR_AutoHitchDistance_Description".Translate());
					autoHitchDistance = (int)options.Slider(autoHitchDistance, 0f, 200f);

					options.CheckboxLabeled("GU_RR_NoMountedHunting_Title".Translate(), ref noMountedHunting, "GU_RR_NoMountedHunting_Description".Translate());
					options.CheckboxLabeled("GU_RR_DisableSlavePawnColumn_Title".Translate(), ref disableSlavePawnColumn, "GU_RR_DisableSlavePawnColumn_Description".Translate());
					if (Prefs.DevMode) options.CheckboxLabeled("Enable dev mode logging", ref logging);
				}
				
				options.End();
			}
			void DrawBattleMounts()
			{
				Listing_Standard options = new Listing_Standard();   
				options.Begin(rect.ContractedBy(15f));

				options.CheckboxLabeled("GU_Enable_BattleMounts".Translate(), ref battleMountsEnabled, "GU_Enable_BattleMounts_Description".Translate());
				if (battleMountsEnabled)
				{
					options.Gap();
					options.GapLine(); //=============================
					options.Gap();
					
					options.Label("BM_MinHandlingLevel_Title".Translate("0", "20", "3", minHandlingLevel.ToString()), -1f, "BM_MinHandlingLevel_Description".Translate());
					minHandlingLevel = (int)options.Slider(minHandlingLevel, 0f, 20f);
					
					options.Label("BM_EnemyMountChance_Title".Translate("0", "100", "15", enemyMountChance.ToString()), -1f, "BM_EnemyMountChance_Description".Translate());
					enemyMountChance = (int)options.Slider(enemyMountChance, 0f, 100f);

					options.Label("BM_EnemyMountChanceTribal_Title".Translate("0", "100", "33", enemyMountChancePreInd.ToString()), -1f, "BM_EnemyMountChanceTribal_Description".Translate());
					enemyMountChancePreInd = (int)options.Slider(enemyMountChancePreInd, 0f, 100f);

					options.Label("BM_InBiomeWeight_Title".Translate("0", "100", "20", inBiomeWeight.ToString()), -1f, "BM_InBiomeWeight_Description".Translate());
					inBiomeWeight = options.Slider((float)Math.Round(inBiomeWeight), 0f, 100f);

					options.Label("BM_OutBiomeWeight_Title".Translate("0", "100", "10", outBiomeWeight.ToString()), -1f, "BM_OutBiomeWeight_Description".Translate());
					outBiomeWeight = (int)options.Slider((float)Math.Round(outBiomeWeight), 0f, 100f);

					options.Label("BM_NonWildWeight_Title".Translate("0", "100", "70", nonWildWeight.ToString()), -1f, "BM_NonWildWeight_Description".Translate());
					nonWildWeight = (int)options.Slider((float)Math.Round(nonWildWeight), 0f, 100f);
				}
				
				options.End();
			}
			void DrawCaravan()
			{
				Listing_Standard options = new Listing_Standard();   
				options.Begin(rect.ContractedBy(15f));

				options.CheckboxLabeled("GU_Enable_Caravans".Translate(), ref caravansEnabled, "GU_Enable_Caravans_Description".Translate());
				if (caravansEnabled)
				{
					options.Gap();
					options.GapLine(); //=============================
					options.Gap();

					options.Label("GU_Car_visitorMountChance_Title".Translate("0", "100", "15", visitorMountChance.ToString()), -1f, "GU_Car_visitorMountChance_Description".Translate());
					visitorMountChance = (int)options.Slider(visitorMountChance, 0f, 100f);

					options.Label("GU_Car_visitorMountChanceTribal_Title".Translate("0", "100", "33", visitorMountChancePreInd.ToString()), -1f, "GU_Car_visitorMountChanceTribal_Description".Translate());
					visitorMountChancePreInd = (int)options.Slider(visitorMountChancePreInd, 0f, 100f);

					options.CheckboxLabeled("GU_Car_GiveCaravanSpeed_Title".Translate(), ref giveCaravanSpeed, "GU_Car_GiveCaravanSpeed_Description".Translate());
				}
				
				options.End();
			}
			void DrawCore()
			{
				if (selectedTab == SelectedTab.core) selectedTab = SelectedTab.bodySize;

				Listing_Standard options = new Listing_Standard();
				options.Begin(inRect.ContractedBy(15f));

				options.Label("GUC_HandlingMovementImpact_Title".Translate("0", "10", "2.5", handlingMovementImpact.ToString()), -1f, "GUC_HandlingMovementImpact_Description".Translate());
				handlingMovementImpact = options.Slider((float)Math.Round(handlingMovementImpact, 1), 0f, 10f);

				options.Label("GUC_AccuracyPenalty_Title".Translate("0", "100", "10", accuracyPenalty.ToString()), -1f, "GUC_AccuracyPenalty_Description".Translate());
				accuracyPenalty = (int)options.Slider(accuracyPenalty, 0f, 100f);

				options.Label("GUC_HandlingAccuracyImpact_Title".Translate("0", "2", "0.5", handlingAccuracyImpact.ToString()), -1f, "GUC_HandlingAccuracyImpact_Description".Translate());
				handlingAccuracyImpact = options.Slider((float)Math.Round(handlingAccuracyImpact, 1), 0f, 2f);

				options.Gap();

				if (options.ButtonText("GU_Reset_Cache".Translate()))
				{
					offsetCache = null;
				}
				
				//Record positioning before closing out the lister...
				Rect mountableFilterRect = inRect.ContractedBy(15f);
				mountableFilterRect.y = options.curY + 90f;
				mountableFilterRect.height = inRect.height - options.curY - 105f; //Use remaining space

				options.End();

				//========Setup tabs=========
				tabs = new List<TabRecord>();
				tabs.Add(new TabRecord("GUC_Mountable_Tab".Translate(), delegate { selectedTab = SelectedTab.bodySize; }, selectedTab == SelectedTab.bodySize));
				tabs.Add(new TabRecord("GUC_DrawBehavior_Tab".Translate(), delegate { selectedTab = SelectedTab.drawBehavior; }, selectedTab == SelectedTab.drawBehavior));
				
				Widgets.DrawMenuSection(mountableFilterRect); //Used to make the background light grey with white border
				TabDrawer.DrawTabs(new Rect(mountableFilterRect.x, mountableFilterRect.y, mountableFilterRect.width, Text.LineHeight), tabs);

				//========Between tabs and scroll body=========
				options.Begin(new Rect (mountableFilterRect.x + 10, mountableFilterRect.y + 10, mountableFilterRect.width - 10f, mountableFilterRect.height - 10f));
					if (selectedTab == SelectedTab.bodySize)
					{
						options.Label("GUC_BodySizeFilter_Title".Translate("0", "5", "1.2", bodySizeFilter.ToString()), -1f, "GUC_BodySizeFilter_Description".Translate());
						bodySizeFilter = options.Slider((float)Math.Round(bodySizeFilter, 1), 0f, 5f);
					}
					else
					{
						options.Label("GUC_DrawBehavior_Description".Translate());
					}
				options.End();
				//========Scroll area=========
				mountableFilterRect.y += 60f;
				mountableFilterRect.yMax -= 60f;
				Rect mountableFilterInnerRect = new Rect(0f, 0f, mountableFilterRect.width - 30f, (OptionsDrawUtility.lineNumber + 2) * 22f);
				Widgets.BeginScrollView(mountableFilterRect, ref scrollPos, mountableFilterInnerRect , true);
					options.Begin(mountableFilterInnerRect);
					options.DrawList(inRect);
					options.End();
				Widgets.EndScrollView();   
			}
		}
		public override string SettingsCategory()
		{
			return "Giddy-Up";
		}
		public override void WriteSettings()
		{            
			try
			{
				Setup.RebuildInversions();
				Setup.ProcessPawnKinds();
				if (giveCaravanSpeed) for (int i = 0; i < Setup.allAnimals.Length; i++) Setup.CalculateCaravanSpeed(Setup.allAnimals[i], true);

				//TODO: consider providing a list of all jobdefs users can add/remove to the allowed list
				if (noMountedHunting) JobDriver_Mounted.allowedJobs.Add(JobDefOf.Hunt);
				else JobDriver_Mounted.allowedJobs.Remove(JobDefOf.Hunt);
			}
			catch (System.Exception ex)
			{
				Log.Error("[Giddy-Up] Error writing Giddy-Up settings. Skipping...\n" + ex);   
			}
			base.WriteSettings();
		}   
	}
	public class ModSettings_GiddyUp : ModSettings
	{
		public override void ExposeData()
		{
			Scribe_Values.Look(ref handlingMovementImpact, "handlingMovementImpact", 2.5f);
			Scribe_Values.Look(ref handlingAccuracyImpact, "handlingAccuracyImpact", 0.5f);
			Scribe_Values.Look(ref accuracyPenalty, "accuracyPenalty", 10);
			Scribe_Values.Look(ref minAutoMountDistance, "minAutoMountDistanceNew", 120);
			Scribe_Values.Look(ref minHandlingLevel, "minHandlingLevel", 3);
			Scribe_Values.Look(ref enemyMountChance, "enemyMountChance", 15);
			Scribe_Values.Look(ref enemyMountChancePreInd, "enemyMountChancePreInd", 33);
			Scribe_Values.Look(ref inBiomeWeight, "inBiomeWeight", 20f);
			Scribe_Values.Look(ref outBiomeWeight, "outBiomeWeight", 10f);
			Scribe_Values.Look(ref nonWildWeight, "nonWildWeight", 70);
			Scribe_Values.Look(ref visitorMountChance, "visitorMountChance", 15);
			Scribe_Values.Look(ref visitorMountChancePreInd, "visitorMountChancePreInd", 33);
			Scribe_Values.Look(ref autoHitchDistance, "autoHitchThreshold", 50);
			Scribe_Values.Look(ref tabsHandler, "tabsHandler");
			Scribe_Values.Look(ref rideAndRollEnabled, "rideAndRollEnabled", true);
			Scribe_Values.Look(ref battleMountsEnabled, "battleMountsEnabled", true);
			Scribe_Values.Look(ref caravansEnabled, "caravansEnabled", true);
			Scribe_Values.Look(ref noMountedHunting, "noMountedHunting");
			Scribe_Values.Look(ref disableSlavePawnColumn, "disableSlavePawnColumn");
			Scribe_Values.Look(ref giveCaravanSpeed, "giveCaravanSpeed");
			Scribe_Collections.Look(ref invertMountingRules, "invertMountingRules", LookMode.Value);
			Scribe_Collections.Look(ref invertDrawRules, "invertDrawRules", LookMode.Value);
			Scribe_Collections.Look(ref offsetCache, "offsetCache", LookMode.Value);
			
			base.ExposeData();
		}

		public static float handlingMovementImpact = 2.5f,
			bodySizeFilter = 1.2f,
			handlingAccuracyImpact = 0.5f,
			inBiomeWeight = 20f, 
			outBiomeWeight = 10f,
			nonWildWeight = 70f;
		public static int accuracyPenalty = 10,
			minAutoMountDistance = 120,
			minHandlingLevel = 3,
			enemyMountChance = 15, 
			enemyMountChancePreInd = 33, 
			visitorMountChance = 15, 
			visitorMountChancePreInd = 33,
			autoHitchDistance = 50;
		public static bool rideAndRollEnabled = true, 
			battleMountsEnabled = true,
			caravansEnabled = true,
			noMountedHunting,
			logging,
			giveCaravanSpeed,
			disableSlavePawnColumn;
		public static HashSet<string> invertMountingRules, invertDrawRules; //These are only used on game start to setup the below, fast cache collections
		public static HashSet<ushort> mountableCache, drawRulesCache;
		public static string tabsHandler;
		public static Vector2 scrollPos;
		public static SelectedTab selectedTab = SelectedTab.bodySize;
		public enum SelectedTab { bodySize, drawBehavior, core, rnr, battlemounts, caravans };
		public static Dictionary<string, float> offsetCache;
	}
}