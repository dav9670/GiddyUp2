using GiddyUp;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;
using System.Reflection;
using System.Collections.Generic;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpCaravan.Harmony
{	
	[HarmonyPatch]
	static class Patch_PrepareCaravan_Leave
	{
		static bool Prepare()
		{
			return Settings.caravansEnabled;
		}
		static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_Leave), nameof(LordToil_PrepareCaravan_Leave.UpdateAllDuties));
            //yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherDownedPawns), nameof(LordToil_PrepareCaravan_GatherDownedPawns.UpdateAllDuties));
        }
		static void Postfix(Lord ___lord)
		{
			foreach (Pawn pawn in ___lord.ownedPawns)
			{
				if (pawn.RaceProps.Animal) continue;
				var pawnData = pawn.GetGUData();
				if (pawnData.reservedMount != null)
				{
					if (pawnData.reservedMount.IsStillMountable(pawn, out IsMountableUtility.Reason reason))
					{
						pawn.GoMount(pawnData.reservedMount);
					}
					else if (Settings.logging) Log.Message("[Giddy-Up] " + pawn.thingIDNumber.ToString() + " cannot mount their assigned caravan animal. Reason: " + reason.ToString());
				}
			}
		}
	}
}