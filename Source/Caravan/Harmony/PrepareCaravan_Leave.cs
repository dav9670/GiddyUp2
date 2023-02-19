using GiddyUp;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using System.Linq;
using System.Collections.Generic;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpCaravan.Harmony
{	
	[HarmonyPatch(typeof(LordToil_PrepareCaravan_Leave), nameof(LordToil_PrepareCaravan_Leave.UpdateAllDuties))]
	static class Patch_PrepareCaravan_Leave
	{
		static bool Prepare()
		{
			return Settings.caravansEnabled;
		}
		static void Postfix(LordToil_PrepareCaravan_Leave __instance)
		{
			foreach (Pawn pawn in __instance.lord.ownedPawns)
			{
				if (pawn.RaceProps.Animal) continue;
				var pawnData = pawn.GetGUData();
				if (pawnData.reservedMount == null) continue;
				pawn.GoMount(pawnData.reservedMount, MountUtility.GiveJobMethod.Inject);   
			}
		}
	}
}