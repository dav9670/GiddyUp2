using GiddyUp.Utilities;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace Battlemounts.Harmony
{
    [HarmonyPatch(typeof(IncidentWorker_Raid), nameof(IncidentWorker_Raid.TryGenerateRaidInfo))]
    static class Patch_TryGenerateRaidInfo
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.battleMountsEnabled;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in instructions)
            {
                if (code.operand as MethodInfo == AccessTools.Method(typeof(PawnsArrivalModeWorker), nameof(PawnsArrivalModeWorker.Arrive)))
                {
                    yield return new CodeInstruction(OpCodes.Call, typeof(Patch_TryGenerateRaidInfo).GetMethod(nameof(Patch_TryGenerateRaidInfo.MountAnimals)));
                    continue;
                }
                yield return code;
            }
        }

        public static void MountAnimals(PawnsArrivalModeWorker instance, List<Pawn> pawns, IncidentParms parms)
        {
            if (pawns.Count == 0) return;

            parms.raidArrivalMode.Worker.Arrive(pawns, parms);
            if (!(parms.raidArrivalMode == null || 
                parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkIn) || 
                (parms.raidStrategy != null && 
                parms.raidStrategy.workerClass == typeof(RaidStrategyWorker_Siege)))
            {
                return;
            }
            NPCMountUtility.generateMounts(ref pawns, parms, Settings.inBiomeWeight, Settings.outBiomeWeight, Settings.nonWildWeight, Settings.enemyMountChance, Settings.enemyMountChanceTribal);

            foreach (Pawn pawn in pawns)
            {
                if (pawn.equipment == null)
                {
                    pawn.equipment = new Pawn_EquipmentTracker(pawn);
                }
            }
            foreach(Pawn pawn in pawns) //Moved this code here so we can check if the pawn actually has apparel. 
            {
                if (pawn.apparel != null && pawn.apparel.WornApparel != null && pawn.apparel.WornApparel.Any(ap => ap.def == ThingDefOf.Apparel_ShieldBelt))
                {
                    LessonAutoActivator.TeachOpportunity(ConceptDefOf.ShieldBelts, OpportunityType.Critical);
                    break;
                }
            }
        }
    }
}
