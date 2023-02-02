using System;
using System.Collections.Generic;
using GiddyUp.Storage;
using Verse;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;
using GiddyUpRideAndRoll.Zones;
using GiddyUpCaravan.Zones;
using static GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp
{
    [StaticConstructorOnStartup]
    public static class Setup
    {
        public static ExtendedDataStorage _extendedDataStorage;
        public static HashSet<int> isMounted = new HashSet<int>();
        internal static HashSet<PawnKindDef> animalsWithBiome = new HashSet<PawnKindDef>(), animalsWithoutBiome = new HashSet<PawnKindDef>();
        public static ThingDef[] allAnimals;
        public const float defaultSizeThreshold = 1.2f;

        static Setup()
        {
            new HarmonyLib.Harmony("GiddyUp").PatchAll();

            BuildCache();
            BuildAnimalBiomeCache();
            if (!rideAndRollEnabled) RemoveRideAndRoll();
            if (!caravansEnabled) RemoveCaravans();
            
        }
        public static void BuildCache(bool reset = false)
        {
            //Setup collections
            List<ThingDef> workingList = new List<ThingDef>();
            if (invertMountingRules == null) invertMountingRules = new HashSet<string>();
            mountableCache = new HashSet<ushort>();

            if (invertDrawRules == null) invertDrawRules = new HashSet<string>();
            _drawSelecter = new HashSet<ushort>();

            var list = DefDatabase<ThingDef>.AllDefsListForReading;
            var length = list.Count;
            for (int i = 0; i < length; i++)
            {
                var def = list[i];
                if (def.race != null && def.race.Animal) 
                {
                    workingList.Add(def);

                    bool setting = def.race.baseBodySize > defaultSizeThreshold;
                    if (invertMountingRules.Contains(def.defName)) setting = !setting; //Player customization says to invert rule.
                    
                    if (setting) mountableCache.Add(def.shortHash);
                    else mountableCache.Remove(def.shortHash);

                    //Handle the mod extension
                    if (!reset)
                    {
                        var modX = def.GetModExtension<DrawingOffsetPatch>();
                        if (modX != null) modX.Init();
                    }

                    //Handle the draw front/behind draw instruction cache
                    setting = false;
                    var comp = def.GetCompProperties<CompProperties_Mount>();
                    if (comp != null) setting = comp.drawFront;
                    if (invertDrawRules.Contains(def.defName)) setting = !setting;
                    
                    if (setting) _drawSelecter.Add(def.shortHash);
                    else _drawSelecter.Remove(def.shortHash);
                }
            }
            workingList.SortBy(x => x.label);
            allAnimals = workingList.ToArray();
        }
        static void BuildAnimalBiomeCache()
        {
            var biomeDefs = DefDatabase<BiomeDef>.AllDefsListForReading;
            var length = biomeDefs.Count;
            for (int i = 0; i < length; i++)
            {
                var biomeDef = biomeDefs[i];
                foreach(PawnKindDef animalKind in biomeDef.AllWildAnimals)
                {
                    animalsWithBiome.Add(animalKind);
                }
            }
            
            var pawnKindDefs = DefDatabase<PawnKindDef>.AllDefsListForReading;
            length = biomeDefs.Count;
            for (int i = 0; i < length; i++)
            {
                var def = pawnKindDefs[i];
                if (def.RaceProps.Animal && !animalsWithBiome.Contains(def)) animalsWithoutBiome.Add(def);
            }
        }
        static void RemoveRideAndRoll()
        {
            //Remove jobs
            DefDatabase<JobDef>.Remove(ResourceBank.JobDefOf.WaitForRider);

            //Remove pawn columns (UI icons in the pawn table)
            DefDatabase<PawnTableDef>.GetNamed("Animals").columns.RemoveAll(x => x.defName == "MountableByAnyone");
            
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
    
        [HarmonyPatch(typeof(World), nameof(World.FinalizeInit))]
        static class Patch_WorldLoaded
        {
            static void Postfix()
            {
                _extendedDataStorage = Find.World.GetComponent<ExtendedDataStorage>();
                LessonAutoActivator.TeachOpportunity(ResourceBank.ConceptDefOf.GUC_Animal_Handling, OpportunityType.GoodToKnow);

                //Remove alert
                if (!rideAndRollEnabled)
                {
                    try { Find.Alerts.AllAlerts.RemoveAll(x => x.GetType() == typeof(GiddyUpRideAndRoll.Alerts.Alert_NoDropAnimal)); }
                    catch (System.Exception) { Log.Warning("[Giddy-up] Failed to remove Alert_NoDropAnimal instance."); }
                }

                //BM
                if (battleMountsEnabled)
                {
                    LessonAutoActivator.TeachOpportunity(ResourceBank.ConceptDefOf.BM_Mounting, OpportunityType.GoodToKnow);
                    LessonAutoActivator.TeachOpportunity(ResourceBank.ConceptDefOf.BM_Enemy_Mounting, OpportunityType.GoodToKnow);
                }
            }
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

                    options.CheckboxLabeled("GU_RR_NoMountedHunting_Title".Translate(), ref noMountedHunting, "GU_RR_NoMountedHunting_Description".Translate());
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
                    
                    options.Label("BM_EnemyMountChance_Title".Translate("0", "100", "20", enemyMountChance.ToString()), -1f, "BM_EnemyMountChance_Description".Translate());
                    enemyMountChance = (int)options.Slider(enemyMountChance, 0f, 100f);

                    options.Label("BM_EnemyMountChanceTribal_Title".Translate("0", "100", "40", enemyMountChanceTribal.ToString()), -1f, "BM_EnemyMountChanceTribal_Description".Translate());
                    enemyMountChanceTribal = (int)options.Slider(enemyMountChanceTribal, 0f, 100f);

                    options.Label("BM_InBiomeWeight_Title".Translate("0", "100", "70", inBiomeWeight.ToString()), -1f, "BM_InBiomeWeight_Description".Translate());
                    inBiomeWeight = (int)options.Slider(inBiomeWeight, 0f, 100f);

                    options.Label("BM_OutBiomeWeight_Title".Translate("0", "100", "15", outBiomeWeight.ToString()), -1f, "BM_OutBiomeWeight_Description".Translate());
                    outBiomeWeight = (int)options.Slider(outBiomeWeight, 0f, 100f);

                    options.Label("BM_NonWildWeight_Title".Translate("0", "100", "15", nonWildWeight.ToString()), -1f, "BM_NonWildWeight_Description".Translate());
                    nonWildWeight = (int)options.Slider(nonWildWeight, 0f, 100f);
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
                    
                    //options.Label("GU_Car_CompleteCaravanBonus_Title".Translate("0", "200", "60", completeCaravanBonus.ToString()), -1f, "GU_Car_CompleteCaravanBonus_Description".Translate());
                    //completeCaravanBonus = (int)options.Slider(completeCaravanBonus, 0f, 200f);

                    //options.Label("GU_Car_incompleteCaravanBonusCap_Title".Translate("0", "200", "25", incompleteCaravanBonusCap.ToString()), -1f, "GU_Car_incompleteCaravanBonusCap_Description".Translate());
                    //incompleteCaravanBonusCap = (int)options.Slider(incompleteCaravanBonusCap, 0f, 200f);

                    options.Label("GU_Car_visitorMountChance_Title".Translate("0", "100", "20", visitorMountChance.ToString()), -1f, "GU_Car_visitorMountChance_Description".Translate());
                    visitorMountChance = (int)options.Slider(visitorMountChance, 0f, 100f);

                    options.Label("GU_Car_visitorMountChanceTribal_Title".Translate("0", "100", "40", visitorMountChanceTribal.ToString()), -1f, "GU_Car_visitorMountChanceTribal_Description".Translate());
                    visitorMountChanceTribal = (int)options.Slider(visitorMountChanceTribal, 0f, 100f);
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
                        options.Label("GUC_BodySizeFilter_Title".Translate("0", "5", "1.1", bodySizeFilter.ToString()), -1f, "GUC_BodySizeFilter_Description".Translate());
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
                Rect mountableFilterInnerRect = new Rect(0f, 0f, mountableFilterRect.width - 30f, (DrawUtility.lineNumber + 2) * 22f);
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
                RebuildInversions();
            }
            catch (System.Exception ex)
            {
                Log.Error("[Giddy-up] Error writing Giddy-up settings. Skipping...\n" + ex);   
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
            Scribe_Values.Look(ref minAutoMountDistance, "minAutoMountDistanceNew", 200);
            Scribe_Values.Look(ref enemyMountChance, "enemyMountChance", 20);
            Scribe_Values.Look(ref enemyMountChanceTribal, "enemyMountChanceTribal", 40);
            Scribe_Values.Look(ref inBiomeWeight, "inBiomeWeight", 70);
            Scribe_Values.Look(ref outBiomeWeight, "outBiomeWeight", 15);
            Scribe_Values.Look(ref nonWildWeight, "nonWildWeight", 15);
            //Scribe_Values.Look(ref completeCaravanBonus, "completeCaravanBonus", 60);
            //Scribe_Values.Look(ref incompleteCaravanBonusCap, "incompleteCaravanBonusCap", 25);
            Scribe_Values.Look(ref visitorMountChance, "visitorMountChance", 20);
            Scribe_Values.Look(ref visitorMountChanceTribal, "visitorMountChanceTribal", 40);
            Scribe_Values.Look(ref tabsHandler, "tabsHandler");
            Scribe_Values.Look(ref rideAndRollEnabled, "rideAndRollEnabled", true);
            Scribe_Values.Look(ref battleMountsEnabled, "battleMountsEnabled", true);
            Scribe_Values.Look(ref caravansEnabled, "caravansEnabled", true);
            Scribe_Values.Look(ref noMountedHunting, "noMountedHunting");
            Scribe_Collections.Look(ref invertMountingRules, "invertMountingRules", LookMode.Value);
            Scribe_Collections.Look(ref invertDrawRules, "invertDrawRules", LookMode.Value);
			
			base.ExposeData();
		}
		//ToDo? It may be possible to fold this into th Setup.BuildCache method
        public static void RebuildInversions()
        {
            //Reset
            invertMountingRules = new HashSet<string>();
            invertDrawRules = new HashSet<string>();

            foreach (var animalDef in Setup.allAnimals)
            {
                var hash = animalDef.shortHash;
                //Search for abnormalities, meaning the player wants to invert the rules
                if (animalDef.race.baseBodySize <= Setup.defaultSizeThreshold)
                {
                    if (mountableCache.Contains(hash)) invertMountingRules.Add(animalDef.defName);
                }
                else
                {
                    if (!mountableCache.Contains(hash)) invertMountingRules.Add(animalDef.defName);
                }

                //And now draw rules
                bool drawFront = false;
                var comp = animalDef.GetCompProperties<CompProperties_Mount>();
                if (comp != null) drawFront = comp.drawFront;
                
                if (drawFront && !_drawSelecter.Contains(hash)) invertDrawRules.Add(animalDef.defName);
                else if (!drawFront && _drawSelecter.Contains(hash)) invertDrawRules.Add(animalDef.defName);
            } 
        }
        
        public static float handlingMovementImpact = 2.5f, bodySizeFilter = 1.2f, handlingAccuracyImpact = 0.5f;
        public static int accuracyPenalty = 10,
            minAutoMountDistance = 200,
            enemyMountChance = 20, 
            enemyMountChanceTribal = 40, 
            inBiomeWeight = 70, 
            outBiomeWeight = 15, 
            nonWildWeight = 15,
            //completeCaravanBonus = 60, 
            //incompleteCaravanBonusCap = 25, 
            visitorMountChance = 20, 
            visitorMountChanceTribal = 40;
        public static bool rideAndRollEnabled = true, battleMountsEnabled = true, caravansEnabled = true, noMountedHunting, logging;
        public static HashSet<string> invertMountingRules, invertDrawRules; //These are only used on game start to setup the below, fast cache collections
        public static HashSet<ushort> mountableCache, _drawSelecter;
        public static string tabsHandler;
        public static Vector2 scrollPos;
        public static SelectedTab selectedTab = SelectedTab.bodySize;
        public enum SelectedTab { bodySize, drawBehavior, core, rnr, battlemounts, caravans };
	}
}
