using HarmonyLib;
using System;
using UnityEngine;
using Verse;

namespace GiddyUp
{
    public class Area_GU : Area
    {
        String label;
        Color color = Color.magenta;
        public Area_GU() { }
        public Area_GU(AreaManager areaManager, string label) : base(areaManager)
        {
            this.color = new Color(Rand.Value, Rand.Value, Rand.Value);
            this.label = label;
        }
        public override string Label
        {
            get
            {
                return label;
            }
        }
        public override Color Color
        {
            get
            {
                return color;
            }
        }
        public override int ListPriority
        {
            get
            {
                return 300;
            }
        }
        public override string GetUniqueLoadID()
        {
            return label; //only one such area, so label is sufficient. 
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.label, "label", null, false);
            Scribe_Values.Look<Color>(ref this.color, "color", default(Color), false);
        }
    }
    
    [HarmonyPatch(typeof(Area), nameof(Area.Set))]
    static class Patch_AreaSet
    {
        static void Postfix(Area __instance)
        {
			var label = __instance.Label;
			if (label == ResourceBank.AreaDropMount || label == ResourceBank.AreaDropMount)
			{
				var map = __instance.Map;
            	__instance.Map.UpdateAreaCache();
			}
        }
    }
	[HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
    static class Patch_MapFinalizeInit
    {
        static void Postfix(Map __instance)
        {
			__instance.UpdateAreaCache(true);
        }
    }
}