using System;
using System.Collections.Generic;
using GiddyUp.Storage;
using Verse;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;
using static GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp
{
    [StaticConstructorOnStartup]
    public static class Setup
    {
        public const string DROPANIMAL_LABEL = "Gu_Area_DropMount";
        public const string NOMOUNT_LABEL = "Gu_Area_NoMount";
        public static ExtendedDataStorage _extendedDataStorage;
        public static HashSet<int> isMounted = new HashSet<int>();
        public static bool GiddyUpMechanoidsLoaded, facialStuffLoaded;
        internal static HashSet<PawnKindDef> animalsWithBiome = new HashSet<PawnKindDef>();
        internal static HashSet<PawnKindDef> animalsWithoutBiome = new HashSet<PawnKindDef>();
        public static ThingDef[] allAnimals;
        const float defaultSizeThreshold = 1.2f;

        static Setup()
        {
            new Harmony("GiddyUp").PatchAll();

            GiddyUpMechanoidsLoaded = AssemblyExists("GiddyUpMechanoids");
            facialStuffLoaded = AssemblyExists("FacialStuff");

            BuildCache();

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

        public static void BuildCache(bool reset = false)
        {
            //Setup collections
            List<ThingDef> workingList = new List<ThingDef>();
            if (invertMountingRules == null) invertMountingRules = new HashSet<string>();
            _animalSelecter = new HashSet<ushort>();

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
                    
                    if (setting) _animalSelecter.Add(def.shortHash);
                    else _animalSelecter.Remove(def.shortHash);

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
    
        //Mod names sometimes change when Rimworld changes its version. Checking for the assembly name, which probably won't change is therefore a better idea than using ModLister.HasActiveModWithName
        static bool AssemblyExists(string assemblyName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.StartsWith(assemblyName))
                    return true;
            }
            return false;
        }
    
        [HarmonyPatch(typeof(World), nameof(World.FinalizeInit))]
        static class Patch_WorldLoaded
        {
            static void Postfix()
            {
                _extendedDataStorage = Find.World.GetComponent<ExtendedDataStorage>();
                _extendedDataStorage.Cleanup();
                LessonAutoActivator.TeachOpportunity(ResourceBank.ConceptDefOf.GUC_Animal_Handling, OpportunityType.GoodToKnow);
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
            //minAutoMountDistance = Settings.GetHandle<int>("minAutoMountDistance", "GU_RR_MinAutoMountDistance_Title".Translate(), "GU_RR_MinAutoMountDistance_Description".Translate(), 16, Validators.IntRangeValidator(0, 500));
            //noMountedHunting = Settings.GetHandle<bool>("noMountedHunting", "GU_RR_NoMountedHunting_Title".Translate(), "GU_RR_NoMountedHunting_Description".Translate(), false);
            //minAutoMountDistanceFromAnimal = Settings.GetHandle<int>("minAutoMountDistanceFromAnimal", "GU_RR_MinAutoMountDistanceFromAnimal_Title".Translate(), "GU_RR_MinAutoMountDistanceFromAnimal_Description".Translate(), 12, Validators.IntRangeValidator(0, 500));

            Rect rect = new Rect(0f, 32f, inRect.width, inRect.height - 32f);
			Listing_Standard options = new Listing_Standard();
			options.Begin(inRect);

            options.Label("GUC_HandlingMovementImpact_Title".Translate("0", "10", "2.5", handlingMovementImpact.ToString()), -1f, "GUC_HandlingMovementImpact_Description".Translate());
			handlingMovementImpact = options.Slider((float)Math.Round(handlingMovementImpact, 1), 0f, 10f);

            options.Label("GUC_AccuracyPenalty_Title".Translate("0", "100", "10", accuracyPenalty.ToString()), -1f, "GUC_AccuracyPenalty_Description".Translate());
			accuracyPenalty = (int)options.Slider(accuracyPenalty, 0f, 100f);

            options.Label("GUC_HandlingAccuracyImpact_Title".Translate("0", "2", "0.5", handlingAccuracyImpact.ToString()), -1f, "GUC_HandlingAccuracyImpact_Description".Translate());
			handlingAccuracyImpact = options.Slider((float)Math.Round(handlingAccuracyImpact, 1), 0f, 2f);
            
            //Record positioning before closing out the lister...
            Rect mountableFilterRect = inRect;
            mountableFilterRect.y = options.curY + 90f;
            mountableFilterRect.height = inRect.height - options.curY - 90f; //Use remaining space

            options.End();

            //========Setup tabs=========
            var tabs = new List<TabRecord>();
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
            Rect mountableFilterInnerRect = new Rect(0f, 0f, mountableFilterRect.width - 30f, (_animalSelecter.Count + 2) * 22f);
            Widgets.BeginScrollView(mountableFilterRect, ref scrollPos, mountableFilterInnerRect , true);
                options.Begin(mountableFilterInnerRect);
                  options.DrawList(inRect);
                options.End();
            Widgets.EndScrollView();
        }

        public override string SettingsCategory()
		{
			return "Giddy-Up";
		}
		public override void WriteSettings()
		{
			base.WriteSettings();
		}
        
    }
    public class ModSettings_GiddyUp : ModSettings
	{
		public override void ExposeData()
		{
			Scribe_Values.Look(ref handlingMovementImpact, "handlingMovementImpact", 2.5f);
            Scribe_Values.Look(ref bodySizeFilter, "bodySizeFilter", 1.2f);
            Scribe_Values.Look(ref handlingAccuracyImpact, "handlingAccuracyImpact", 0.5f);
            Scribe_Values.Look(ref accuracyPenalty, "accuracyPenalty", 10);
            Scribe_Values.Look(ref minAutoMountDistance, "minAutoMountDistance", 16);
            Scribe_Values.Look(ref minAutoMountDistanceFromAnimal, "minAutoMountDistanceFromAnimal", 12);
            Scribe_Values.Look(ref tabsHandler, "tabsHandler");
            Scribe_Values.Look(ref rideAndRollEnabled, "rideAndRollEnabled", true);
            Scribe_Values.Look(ref noMountedHunting, "noMountedHunting");
            Scribe_Collections.Look(ref invertMountingRules, "invertMountingRules", LookMode.Value);
            Scribe_Collections.Look(ref invertDrawRules, "invertDrawRules", LookMode.Value);
			
			base.ExposeData();
		}

		public static float handlingMovementImpact = 2.5f, bodySizeFilter = 1.2f, handlingAccuracyImpact = 0.5f;
        public static int accuracyPenalty = 10, minAutoMountDistance = 16, minAutoMountDistanceFromAnimal = 12;
        public static bool rideAndRollEnabled = true, noMountedHunting;
        public static HashSet<string> invertMountingRules, invertDrawRules; //These are only used on game start to setup the below, fast cache collections
        public static HashSet<ushort> _animalSelecter, _drawSelecter;
        public static string tabsHandler;
        public static Vector2 scrollPos;
        public static SelectedTab selectedTab = SelectedTab.bodySize;
        public enum SelectedTab { bodySize, drawBehavior };
	}
}
