using HarmonyLib;
using Verse;
using static GiddyUp.ExtendedDataStorage;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(Area), nameof(Area.Set))]
    class Patch_AreaSet
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
    class Patch_MapFinalizeInit
    {
        static void Postfix(Map __instance)
        {
			__instance.UpdateAreaCache(true);
        }
    }
}