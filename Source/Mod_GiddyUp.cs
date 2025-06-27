using GiddyUpRideAndRoll;
using GiddyUpCaravan;
using GiddyUp.Jobs;
using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using GiddyUpCore.RideAndRoll;
using Verse;
using UnityEngine;
using static GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp;

[StaticConstructorOnStartup]
public static class Setup
{
    public static readonly List<ThingDef?> AllAnimals = []; //Only used during setup and for the mod options UI
    
    private static readonly HashSet<ushort> DefEditLedger = [];
    private static readonly HashSet<int> PatchLedger = [];

    private static readonly SimpleCurve SizeFactor =
    [
        new CurvePoint(1.2f, 0.9f),
        new CurvePoint(1.5f, 1f),
        new CurvePoint(2.4f, 1.15f),
        new CurvePoint(4f, 1.25f)
    ];

    private static readonly SimpleCurve SpeedFactor =
    [
        new CurvePoint(4.3f, 1f),
        new CurvePoint(5.8f, 1.2f),
        new CurvePoint(8f, 1.4f)
    ];

    private static readonly SimpleCurve ValueFactor =
    [
        new CurvePoint(300f, 1f),
        new CurvePoint(550f, 1.15f),
        new CurvePoint(5000f, 1.3f)
    ];

    private static readonly SimpleCurve WildnessFactor =
    [
        new CurvePoint(0.2f, 1f),
        new CurvePoint(0.6f, 0.9f),
        new CurvePoint(1f, 0.85f)
    ];

    static Setup()
    {
        var harmony = new HarmonyLib.Harmony("GiddyUp");
        harmony.PatchAll();

        BuildAllowedJobsCache();
        BuildMountCache();
        BuildAnimalBiomeCache();
        if (!rideAndRollEnabled)
            RemoveRideAndRoll();
        if (!caravansEnabled)
            RemoveCaravans();

        ProcessPawnKinds(harmony);
        if (disableSlavePawnColumn)
            DefDatabase<PawnTableDef>.GetNamed("Animals").columns.RemoveAll(x => x.defName == "MountableBySlaves");

        //VE Classical mod support
        var type = HarmonyLib.AccessTools.TypeByName("AnimalBehaviours.AnimalCollectionClass");
        if (type != null)
            ExtendedDataStorage.noFleeingAnimals = HarmonyLib.Traverse.Create(type).Field("nofleeing_animals")
                ?.GetValue<HashSet<Thing>>();
    }

    public static void BuildAllowedJobsCache()
    {
        JobDriver_Mounted.allowedJobs = new Dictionary<JobDef, bool>();
        var list = DefDatabase<JobDef>.defsList;
        for (var i = list.Count; i-- > 0;)
        {
            var def = list[i];
            if (def.GetModExtension<CanDoMounted>() is CanDoMounted canDoMounted)
                JobDriver_Mounted.allowedJobs.Add(def, canDoMounted.checkTargets);
        }

        if (!noMountedHunting)
            JobDriver_Mounted.allowedJobs.AddDistinct(JobDefOf.Hunt, false);
    }

    //Responsible for caching which animals are mounted, draw layering behavior, and calling caravan speed bonuses
    public static void BuildMountCache()
    {
        //Setup collections
        var workingList = new List<ThingDef>();
        if (invertMountingRules == null)
            invertMountingRules = new HashSet<string>();
        mountableCache = new HashSet<ushort>();

        if (invertDrawRules == null)
            invertDrawRules = new HashSet<string>();
        drawRulesCache = new HashSet<ushort>();

        var list = DefDatabase<ThingDef>.AllDefsListForReading;
        var length = list.Count;
        for (var i = 0; i < length; i++)
        {
            var def = list[i];
            if (def.race is not { Animal: true })
                continue;
            
            workingList.Add(def);

            var setting = def.race.baseBodySize > ResourceBank.DefaultSizeThreshold;
            if (def.HasModExtension<NotMountable>())
                setting = false;
            else if (def.HasModExtension<Mountable>())
                setting = true;
            if (invertMountingRules.Contains(def.defName))
                setting = !setting; //Player customization says to invert rule.

            if (setting)
            {
                mountableCache.Add(def.shortHash);
                CalculateCaravanSpeed(def);
            }
            else
            {
                mountableCache.Remove(def.shortHash);
            }

            //Handle the draw front/behind draw instruction cache
            setting = def.HasModExtension<DrawInFront>();
            if (invertDrawRules.Contains(def.defName))
                setting = !setting;

            if (setting)
                drawRulesCache.Add(def.shortHash);
            else
                drawRulesCache.Remove(def.shortHash);
        }

        workingList.SortBy(x => x.label);
        AllAnimals.AddRange(workingList);
    }

    //Responsible for setting up the draw offsets and custom stat overrides
    public static void ProcessPawnKinds(HarmonyLib.Harmony? harmony = null)
    {
        var newEntries = false;
        var usingCustomStats = false;
        if (offsetCache == null)
            offsetCache = new Dictionary<string, float>();
        var list = DefDatabase<PawnKindDef>.AllDefsListForReading;
        var length = list.Count;
        for (var i = 0; i < length; i++)
        {
            var pawnKindDef = list[i];
            if (pawnKindDef.race == null)
                continue;
            if (!usingCustomStats && pawnKindDef.HasModExtension<CustomStats>())
                usingCustomStats = true;

            //Only process animals that can be mounted
            if (mountableCache.Contains(pawnKindDef.race.shortHash))
            {
                //Determine which life stages are considered mature enough to ride
                var lifeStages = pawnKindDef.lifeStages;
                var lifeIndexes = lifeStages?.Count;
                AllowedLifeStages? customLifeStages;
                if (lifeIndexes > 0)
                    customLifeStages = pawnKindDef.race.GetModExtension<AllowedLifeStages>();
                else
                    customLifeStages = null;

                //Go through each life stage for this animal
                for (var lifeIndex = 0; lifeIndex < lifeIndexes; lifeIndex++)
                {
                    //Convert the def and age into a key string used for storage between sessions
                    if (lifeIndex != lifeIndexes - 1 &&
                        (customLifeStages == null || !customLifeStages.IsAllowedAge(lifeIndex)))
                        continue;
                    var key = TextureUtility.FormatKey(pawnKindDef, lifeIndex);

                    //Skip if already set
                    if (offsetCache.ContainsKey(key))
                        continue;

                    //Build out...
                    var offset = TextureUtility.SetDrawOffset(lifeStages[lifeIndex]);
                    offsetCache.Add(key, offset);
                    newEntries = true;
                }
            }
        }

        //Write to settings file
        if (newEntries)
            LoadedModManager.GetMod<Mod_GiddyUp>().modSettings.Write();

        //Only bother applying this harmony patch if using a mod that utilizes this extension
        if (usingCustomStats && harmony != null && !PatchLedger.Add(1))
            harmony.Patch(HarmonyLib.AccessTools.Method(typeof(ArmorUtility), nameof(ArmorUtility.ApplyArmor)),
                postfix: new HarmonyLib.HarmonyMethod(typeof(Harmony.Patch_ApplyArmor),
                    nameof(Harmony.Patch_ApplyArmor.Postfix)));
    }

    //Processes biome information to determine where animals come from, used for NPC mount spawning
    private static void BuildAnimalBiomeCache()
    {
        for (var i = DefDatabase<BiomeDef>.DefCount; i-- > 0;)
        {
            var biomeDef = DefDatabase<BiomeDef>.defsList[i];
            try
            {
                foreach (var animalKind in biomeDef.AllWildAnimals)
                    MountUtility.allWildAnimals.Add(animalKind);
            }
            catch (Exception ex)
            {
                Log.Error(
                    "[Giddy-Up] An error occured calling AllWildAnimals. This may happen if a mod has malformed PawnKindDef when the game is trying to process the def database for the first time. Skipping...\n" +
                    ex);
            }
        }

        for (var i = DefDatabase<PawnKindDef>.DefCount; i-- > 0;)
        {
            var pawnKindDef = DefDatabase<PawnKindDef>.defsList[i];
            if (pawnKindDef.race != null && pawnKindDef.race.GetStatValueAbstract(StatDefOf.Wildness) <= 0.6f &&
                pawnKindDef.race.tradeTags != null &&
                (pawnKindDef.race.tradeTags.Contains("AnimalFighter") ||
                 pawnKindDef.race.tradeTags.Contains("AnimalFarm")))
                MountUtility.allDomesticAnimals.Add(pawnKindDef);
        }
    }

    //TODO: It may be possible to fold this into th BuildCache method
    public static void RebuildInversions()
    {
        //Reset
        invertMountingRules = new HashSet<string>();
        invertDrawRules = new HashSet<string>();

        foreach (var animalDef in AllAnimals)
        {
            var hash = animalDef.shortHash;
            //Search for abnormalities, meaning the player wants to invert the rules
            if (animalDef.HasModExtension<NotMountable>())
            {
                if (mountableCache.Contains(hash))
                    invertMountingRules.Add(animalDef.defName);
            }
            else if (animalDef.HasModExtension<Mountable>())
            {
                if (!mountableCache.Contains(hash))
                    invertMountingRules.Add(animalDef.defName);
            }
            else if (animalDef.race.baseBodySize <= ResourceBank.DefaultSizeThreshold)
            {
                if (mountableCache.Contains(hash))
                    invertMountingRules.Add(animalDef.defName);
            }
            else
            {
                if (!mountableCache.Contains(hash))
                    invertMountingRules.Add(animalDef.defName);
            }

            //And now draw rules
            var drawFront = false;
            var modExt = animalDef.GetModExtension<DrawInFront>();
            if (modExt != null)
                drawFront = true;

            if (drawFront && !drawRulesCache.Contains(hash) || !drawFront && drawRulesCache.Contains(hash))
                invertDrawRules.Add(animalDef.defName);
        }
    }

    private static void RemoveRideAndRoll()
    {
        //Remove jobs
        DefDatabase<JobDef>.Remove(ResourceBank.JobDefOf.WaitForRider);

        //Remove pawn columns (UI icons in the pawn table)
        DefDatabase<PawnTableDef>.GetNamed("Animals").columns.RemoveAll(x =>
            x.defName == "MountableByColonists" || x.defName == "MountableBySlaves");

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
            if (designator is Designator_GU_DropAnimal_Expand ||
                designator is Designator_GU_DropAnimal_Clear ||
                designator is Designator_GU_NoMount_Expand ||
                designator is Designator_GU_NoMount_Clear)
                designationCategoryDef.resolvedDesignators.Remove(designator);
    }

    private static void RemoveCaravans()
    {
        //Remove area designators
        var designationCategoryDef = DefDatabase<DesignationCategoryDef>.GetNamed("Zone");
        designationCategoryDef.specialDesignatorClasses.RemoveAll(x =>
            x == typeof(Designator_GU_DropAnimal_NPC_Clear) ||
            x == typeof(Designator_GU_DropAnimal_NPC_Expand)
        );
        var workingList = new List<Designator>(designationCategoryDef.resolvedDesignators);
        foreach (var designator in workingList)
            if (designator is Designator_GU_DropAnimal_NPC_Clear ||
                designator is Designator_GU_DropAnimal_NPC_Expand)
                designationCategoryDef.resolvedDesignators.Remove(designator);
    }

    public static void CalculateCaravanSpeed(ThingDef def, bool check = false)
    {
        //Horse		2.4 size	5.8 speed	packAnimal	550 value	0.35 wildeness	= 1.6
        //Thrumbo	4.0 size	5.5 speed	!packAnimal	4000 value	0.985 wildness	= 1.5
        //Dromedary	2.1 size	4.3 speed	packAnimal	300 value	0.25 wildeness	= 1.3

        //Muffalo	2.4 size	4.5 speed	packAnimal	300 value	0.6 wildness	= ???

        float speed;

        //This would pass if the animal has an XML-defined bonus that we didn't apply, leave it alone
        if (def.StatBaseDefined(StatDefOf.CaravanRidingSpeedFactor) && !DefEditLedger.Contains(def.shortHash))
        {
            return;
        }
        //This would pass if mod options are changed, the mount is no longer rideable, and it was once given a bonus
        else if (check && !mountableCache.Contains(def.shortHash) && DefEditLedger.Contains(def.shortHash))
        {
            DefEditLedger.Remove(def.shortHash);
            speed = 1f;
        }
        //Give the bonus
        else if (giveCaravanSpeed)
        {
            DefEditLedger.Add(def.shortHash);
            speed = SizeFactor.Evaluate(def.race.baseBodySize) *
                    SpeedFactor.Evaluate(def.GetStatValueAbstract(StatDefOf.MoveSpeed)) *
                    ValueFactor.Evaluate(def.BaseMarketValue) *
                    WildnessFactor.Evaluate(def.GetStatValueAbstract(StatDefOf.Wildness)) *
                    (def.race.packAnimal ? 1.1f : 0.95f);
            if (speed < 1.00001f)
                speed = 1.00001f;
        }
        //Don't give a bonus and instead just set the value to be above 1f so the game thinks it's a rideable mount on the caravan UI, but low enough to render as 100%
        else
        {
            DefEditLedger.Remove(def.shortHash);
            speed = speed = 1.00001f;
        }

        StatUtility.SetStatValueInList(ref def.statBases, StatDefOf.CaravanRidingSpeedFactor, speed);
    }
}

public class Mod_GiddyUp : Mod
{
    public Mod_GiddyUp(ModContentPack content) : base(content)
    {
        GetSettings<ModSettings_GiddyUp>();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        //========Setup tabs=========
        GUI.BeginGroup(inRect);
        var tabs = new List<TabRecord>();
        tabs.Add(new TabRecord("GUC_Core_Tab".Translate(), delegate { selectedTab = SelectedTab.Core; },
            selectedTab == SelectedTab.Core || selectedTab == SelectedTab.BodySize ||
            selectedTab == SelectedTab.DrawBehavior));
        tabs.Add(new TabRecord("GUC_RnR_Tab".Translate(), delegate { selectedTab = SelectedTab.Rnr; },
            selectedTab == SelectedTab.Rnr));
        tabs.Add(new TabRecord("GUC_BattleMounts_Tab".Translate(), delegate { selectedTab = SelectedTab.BattleMounts; },
            selectedTab == SelectedTab.BattleMounts));
        tabs.Add(new TabRecord("GUC_Caravans_Tab".Translate(), delegate { selectedTab = SelectedTab.Caravans; },
            selectedTab == SelectedTab.Caravans));

        var rect = new Rect(0f, 32f, inRect.width, inRect.height - 32f);
        Widgets.DrawMenuSection(rect);
        TabDrawer.DrawTabs(new Rect(0f, 32f, inRect.width, Text.LineHeight), tabs);

        switch (selectedTab)
        {
            case SelectedTab.Core:
            case SelectedTab.BodySize:
            case SelectedTab.DrawBehavior:
                DrawCore();
                break;
            case SelectedTab.Rnr:
                DrawRnR();
                break;
            case SelectedTab.BattleMounts:
                DrawBattleMounts();
                break;
            default:
                DrawCaravan();
                break;
        }
        GUI.EndGroup();

        void DrawRnR()
        {
            var options = new Listing_Standard();
            options.Begin(rect.ContractedBy(15f));

            options.CheckboxLabeled("GU_Enable_RnR".Translate(), ref rideAndRollEnabled,
                "GU_Enable_RnR_Description".Translate());
            if (rideAndRollEnabled)
            {
                options.Gap();
                options.GapLine(); //=============================
                options.Gap();

                options.Label(
                    "GU_RR_MinAutoMountDistance_Title".Translate("0", "500", "120", minAutoMountDistance.ToString()),
                    -1f, "GU_RR_MinAutoMountDistance_Description".Translate());
                minAutoMountDistance = (int)options.Slider(minAutoMountDistance, 20f, 500f);

                options.Label("GU_RR_AutoHitchDistance_Title".Translate("0", "200", "50", autoHitchDistance.ToString()),
                    -1f, "GU_RR_AutoHitchDistance_Description".Translate());
                autoHitchDistance = (int)options.Slider(autoHitchDistance, 0f, 200f);

                options.Label(
                    "GU_RR_InjuredThreshold_Title".Translate("0", "100", "75",
                        Math.Round(injuredThreshold * 100f).ToString()), -1f,
                    "GU_RR_InjuredThreshold_Description".Translate());
                injuredThreshold = options.Slider(injuredThreshold, 0f, 1f);

                options.Label(
                    "GU_RR_WaitForRiderTimer_Title".Translate("1000", "30000", "10000",
                        Math.Round(waitForRiderTimer / 2500f, 1)), -1f,
                    "GU_RR_WaitForRiderTimer_Description".Translate());
                waitForRiderTimer = (int)options.Slider(waitForRiderTimer, 0f, 30000f);

                options.CheckboxLabeled("GU_RR_NoMountedHunting_Title".Translate(), ref noMountedHunting,
                    "GU_RR_NoMountedHunting_Description".Translate());
                options.CheckboxLabeled("GU_RR_DisableSlavePawnColumn_Title".Translate(), ref disableSlavePawnColumn,
                    "GU_RR_DisableSlavePawnColumn_Description".Translate());
                options.CheckboxLabeled("GU_RR_AutomountDisabledByDefault_Title".Translate(),
                    ref automountDisabledByDefault, "GU_RR_AutomountDisabledByDefault_Description".Translate());
                if (Prefs.DevMode)
                    options.CheckboxLabeled("Enable dev mode logging", ref logging);
            }

            options.End();
        }

        void DrawBattleMounts()
        {
            var options = new Listing_Standard();
            options.Begin(rect.ContractedBy(15f));

            options.CheckboxLabeled("GU_Enable_BattleMounts".Translate(), ref battleMountsEnabled,
                "GU_Enable_BattleMounts_Description".Translate());
            if (battleMountsEnabled)
            {
                options.Gap();
                options.GapLine(); //=============================
                options.Gap();

                options.Label("BM_MinHandlingLevel_Title".Translate("0", "20", "3", minHandlingLevel.ToString()), -1f,
                    "BM_MinHandlingLevel_Description".Translate());
                minHandlingLevel = (int)options.Slider(minHandlingLevel, 0f, 20f);

                options.Label("BM_EnemyMountChance_Title".Translate("0", "100", "15", enemyMountChance.ToString()), -1f,
                    "BM_EnemyMountChance_Description".Translate());
                enemyMountChance = (int)options.Slider(enemyMountChance, 0f, 100f);

                options.Label(
                    "BM_EnemyMountChanceTribal_Title".Translate("0", "100", "33", enemyMountChancePreInd.ToString()),
                    -1f, "BM_EnemyMountChanceTribal_Description".Translate());
                enemyMountChancePreInd = (int)options.Slider(enemyMountChancePreInd, 0f, 100f);

                options.Label("BM_InBiomeWeight_Title".Translate("0", "100", "20", inBiomeWeight.ToString()), -1f,
                    "BM_InBiomeWeight_Description".Translate());
                inBiomeWeight = options.Slider((float)Math.Round(inBiomeWeight), 0f, 100f);

                options.Label("BM_OutBiomeWeight_Title".Translate("0", "100", "10", outBiomeWeight.ToString()), -1f,
                    "BM_OutBiomeWeight_Description".Translate());
                outBiomeWeight = (int)options.Slider((float)Math.Round(outBiomeWeight), 0f, 100f);

                options.Label("BM_NonWildWeight_Title".Translate("0", "100", "70", nonWildWeight.ToString()), -1f,
                    "BM_NonWildWeight_Description".Translate());
                nonWildWeight = (int)options.Slider((float)Math.Round(nonWildWeight), 0f, 100f);
            }

            options.End();
        }

        void DrawCaravan()
        {
            var options = new Listing_Standard();
            options.Begin(rect.ContractedBy(15f));

            options.CheckboxLabeled("GU_Enable_Caravans".Translate(), ref caravansEnabled,
                "GU_Enable_Caravans_Description".Translate());
            if (caravansEnabled)
            {
                options.Gap();
                options.GapLine(); //=============================
                options.Gap();

                options.Label(
                    "GU_Car_visitorMountChance_Title".Translate("0", "100", "15", visitorMountChance.ToString()), -1f,
                    "GU_Car_visitorMountChance_Description".Translate());
                visitorMountChance = (int)options.Slider(visitorMountChance, 0f, 100f);

                options.Label(
                    "GU_Car_visitorMountChanceTribal_Title".Translate("0", "100", "33",
                        visitorMountChancePreInd.ToString()), -1f,
                    "GU_Car_visitorMountChanceTribal_Description".Translate());
                visitorMountChancePreInd = (int)options.Slider(visitorMountChancePreInd, 0f, 100f);

                options.CheckboxLabeled("GU_Car_GiveCaravanSpeed_Title".Translate(), ref giveCaravanSpeed,
                    "GU_Car_GiveCaravanSpeed_Description".Translate());
                options.CheckboxLabeled("GU_Car_RidePackAnimals_Title".Translate(), ref ridePackAnimals,
                    "GU_Car_RidePackAnimals_Description".Translate());
            }

            options.End();
        }

        void DrawCore()
        {
            if (selectedTab == SelectedTab.Core)
                selectedTab = SelectedTab.BodySize;

            var options = new Listing_Standard();
            options.Begin(inRect.ContractedBy(15f));

            options.Label(
                "GUC_HandlingMovementImpact_Title".Translate("0", "10", "2.5", handlingMovementImpact.ToString()), -1f,
                "GUC_HandlingMovementImpact_Description".Translate());
            handlingMovementImpact = options.Slider((float)Math.Round(handlingMovementImpact, 1), 0f, 10f);

            options.Label("GUC_AccuracyPenalty_Title".Translate("0", "100", "10", accuracyPenalty.ToString()), -1f,
                "GUC_AccuracyPenalty_Description".Translate());
            accuracyPenalty = (int)options.Slider(accuracyPenalty, 0f, 100f);

            options.Label(
                "GUC_HandlingAccuracyImpact_Title".Translate("0", "2", "0.5", handlingAccuracyImpact.ToString()), -1f,
                "GUC_HandlingAccuracyImpact_Description".Translate());
            handlingAccuracyImpact = options.Slider((float)Math.Round(handlingAccuracyImpact, 1), 0f, 2f);

            options.Gap();

            if (options.ButtonText("GU_Reset_Cache".Translate()))
                offsetCache = null;

            //Record positioning before closing out the lister...
            var mountableFilterRect = inRect.ContractedBy(15f);
            mountableFilterRect.y = options.curY + 90f;
            mountableFilterRect.height = inRect.height - options.curY - 105f; //Use remaining space

            options.End();

            //========Setup tabs=========
            tabs = new List<TabRecord>();
            tabs.Add(new TabRecord("GUC_Mountable_Tab".Translate(), delegate { selectedTab = SelectedTab.BodySize; },
                selectedTab == SelectedTab.BodySize));
            tabs.Add(new TabRecord("GUC_DrawBehavior_Tab".Translate(),
                delegate { selectedTab = SelectedTab.DrawBehavior; }, selectedTab == SelectedTab.DrawBehavior));

            Widgets.DrawMenuSection(mountableFilterRect); //Used to make the background light grey with white border
            TabDrawer.DrawTabs(
                new Rect(mountableFilterRect.x, mountableFilterRect.y, mountableFilterRect.width, Text.LineHeight),
                tabs);

            //========Between tabs and scroll body=========
            options.Begin(new Rect(mountableFilterRect.x + 10, mountableFilterRect.y + 10,
                mountableFilterRect.width - 10f, mountableFilterRect.height - 10f));
            if (selectedTab == SelectedTab.BodySize)
            {
                options.Label("GUC_BodySizeFilter_Title".Translate("0", "5", "1.2", bodySizeFilter.ToString()), -1f,
                    "GUC_BodySizeFilter_Description".Translate());
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
            var mountableFilterInnerRect = new Rect(0f, 0f, mountableFilterRect.width - 30f,
                (OptionsDrawUtility.lineNumber + 2) * 22f);
            Widgets.BeginScrollView(mountableFilterRect, ref scrollPos, mountableFilterInnerRect, true);
            options.Begin(mountableFilterInnerRect);
            options.DrawList();
            options.End();
            Widgets.EndScrollView();
        }
    }

    public override string SettingsCategory() => "Giddy-Up";

    public override void WriteSettings()
    {
        try
        {
            Setup.RebuildInversions();
            Setup.ProcessPawnKinds();
            if (giveCaravanSpeed)
                for (var i = 0; i < Setup.AllAnimals.Count; i++)
                    Setup.CalculateCaravanSpeed(Setup.AllAnimals[i], true);

            //TODO: consider providing a list of all jobdefs users can add/remove to the allowed list
            if (!noMountedHunting)
                JobDriver_Mounted.allowedJobs.AddDistinct(JobDefOf.Hunt, false);
            else
                JobDriver_Mounted.allowedJobs.Remove(JobDefOf.Hunt);
        }
        catch (Exception ex)
        {
            Log.Error("[Giddy-Up] Error writing Giddy-Up settings. Skipping...\n" + ex);
        }

        base.WriteSettings();
    }
}

public class ModSettings_GiddyUp : ModSettings
{
    public static float handlingMovementImpact = 2.5f,
        bodySizeFilter = 0.2f,
        handlingAccuracyImpact = 0.5f,
        inBiomeWeight = 20f,
        outBiomeWeight = 10f,
        nonWildWeight = 70f,
        injuredThreshold = 0.75f;

    public static int accuracyPenalty = 10,
        minAutoMountDistance = 120,
        minHandlingLevel = 3,
        enemyMountChance = 15,
        enemyMountChancePreInd = 33,
        visitorMountChance = 15,
        visitorMountChancePreInd = 33,
        autoHitchDistance = 50,
        waitForRiderTimer = 10000;

    public static bool rideAndRollEnabled = true,
        battleMountsEnabled = true,
        caravansEnabled = true,
        noMountedHunting,
        logging,
        giveCaravanSpeed,
        automountDisabledByDefault,
        disableSlavePawnColumn,
        ridePackAnimals = true;

    public static HashSet<string>?
        invertMountingRules,
        invertDrawRules; //These are only used on game start to setup the below, fast cache collections

    public static HashSet<ushort>? mountableCache, drawRulesCache;
    public static string? tabsHandler;
    public static Vector2 scrollPos;
    public static SelectedTab selectedTab = SelectedTab.BodySize;

    public enum SelectedTab
    {
        BodySize,
        DrawBehavior,
        Core,
        Rnr,
        BattleMounts,
        Caravans
    };
    
    public static Dictionary<string, float>? offsetCache;
    
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
        Scribe_Values.Look(ref automountDisabledByDefault, "automountDisabledByDefault");
        Scribe_Values.Look(ref giveCaravanSpeed, "giveCaravanSpeed");
        Scribe_Values.Look(ref ridePackAnimals, "ridePackAnimals", true);
        Scribe_Values.Look(ref injuredThreshold, "injuredThreshold", 0.75f);
        Scribe_Values.Look(ref waitForRiderTimer, "waitForRiderTimer", 10000);
        Scribe_Collections.Look(ref invertMountingRules, "invertMountingRules", LookMode.Value);
        Scribe_Collections.Look(ref invertDrawRules, "invertDrawRules", LookMode.Value);
        Scribe_Collections.Look(ref offsetCache, "offsetCache", LookMode.Value);

        base.ExposeData();
    }
}