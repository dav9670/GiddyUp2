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
	static class Lordtoil_PrepareCaravan_Leave_UpdateAllDuties
	{
		static bool Prepare()
		{
			return Settings.caravansEnabled;
		}
		static void Prefix(LordToil_PrepareCaravan_Leave __instance)
		{
			AddMissingPawnsToLord(__instance);
			foreach (Pawn pawn in __instance.lord.ownedPawns)
			{
				if (pawn.RaceProps.Animal) continue;
				var pawnData = pawn.GetGUData();
				if (pawnData.reservedMount != null)
				{
					pawn.GoMount(pawnData.reservedMount, MountUtility.GiveJobMethod.Inject);   
				}
			}
		}

		//For compatibility with other mods (Save our ship 2), add any missing mounts to the lord. 
		static void AddMissingPawnsToLord(LordToil_PrepareCaravan_Leave __instance)
		{
			foreach (Pawn pawn in __instance.lord.ownedPawns.ToList())
			{
				Pawn reservedMount = pawn.GetGUData().reservedMount;
				if (reservedMount == null) continue;
				if (!__instance.lord.ownedPawns.Contains(reservedMount))
				{
					__instance.lord.ownedPawns.Add(pawn);
					pawn.mindState.duty = new PawnDuty(DutyDefOf.TravelOrWait, __instance.exitSpot);    
				}
			}
		}
	}
}