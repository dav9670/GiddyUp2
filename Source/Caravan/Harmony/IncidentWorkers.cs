using GiddyUp;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Reflection;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpCaravan.Harmony
{
    [HarmonyPatch]
    static class Patch_IncidentWorkers
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.caravansEnabled;
        }
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(IncidentWorker_TraderCaravanArrival), nameof(IncidentWorker_TraderCaravanArrival.TryExecuteWorker));
            yield return AccessTools.Method(typeof(IncidentWorker_TravelerGroup), nameof(IncidentWorker_TravelerGroup.TryExecuteWorker));
            yield return AccessTools.Method(typeof(IncidentWorker_VisitorGroup), nameof(IncidentWorker_VisitorGroup.TryExecuteWorker));
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(AccessTools.Method(typeof(IncidentWorker_NeutralGroup), nameof(IncidentWorker_NeutralGroup.SpawnPawns)),
				AccessTools.Method(typeof(Patch_IncidentWorkers), nameof(Patch_IncidentWorkers.MountAnimals)));
        }
        static List<Pawn> MountAnimals(IncidentWorker_TraderCaravanArrival instance, IncidentParms parms)
        {
            var pawns = instance.SpawnPawns(parms);
            if (!pawns.NullOrEmpty())
            {
                MountUtility.GenerateMounts(ref pawns, parms, Settings.inBiomeWeight, Settings.outBiomeWeight, Settings.nonWildWeight, Settings.visitorMountChance, Settings.visitorMountChanceTribal);
            }
            return pawns;
        }
    }

    //Animals can't be turned into traders so should be stripped from the list
    [HarmonyPatch(typeof(IncidentWorker_VisitorGroup), nameof(IncidentWorker_VisitorGroup.TryConvertOnePawnToSmallTrader))]
    static class Patch_TryConvertOnePawnToSmallTrader
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.caravansEnabled;
        }
        static void Prefix(List<Pawn> pawns)
        {
            pawns.RemoveAll(x => x.RaceProps.Animal);
        }
    }
}
