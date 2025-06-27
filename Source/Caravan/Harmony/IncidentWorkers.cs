using GiddyUp;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using System.Reflection;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpCaravan.Harmony;

[HarmonyPatch]
internal static class Patch_IncidentWorkers
{
    private static bool Prepare()
    {
        return Settings.caravansEnabled;
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(IncidentWorker_TraderCaravanArrival),
            nameof(IncidentWorker_TraderCaravanArrival.TryExecuteWorker));
        yield return AccessTools.Method(typeof(IncidentWorker_TravelerGroup),
            nameof(IncidentWorker_TravelerGroup.TryExecuteWorker));
        yield return AccessTools.Method(typeof(IncidentWorker_VisitorGroup),
            nameof(IncidentWorker_VisitorGroup.TryExecuteWorker));
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return instructions.MethodReplacer(
            AccessTools.Method(typeof(IncidentWorker_NeutralGroup), nameof(IncidentWorker_NeutralGroup.SpawnPawns)),
            AccessTools.Method(typeof(Patch_IncidentWorkers), nameof(MountAnimals)));
    }

    private static List<Pawn> MountAnimals(IncidentWorker_TraderCaravanArrival instance, IncidentParms parms)
    {
        var pawns = instance.SpawnPawns(parms);
        if (!pawns.NullOrEmpty())
            MountUtility.GenerateMounts(ref pawns, parms);
        return pawns;
    }
}

//Animals can't be turned into traders so should be stripped from the list
[HarmonyPatch(typeof(IncidentWorker_VisitorGroup), nameof(IncidentWorker_VisitorGroup.TryConvertOnePawnToSmallTrader))]
internal static class Patch_TryConvertOnePawnToSmallTrader
{
    private static bool Prepare()
    {
        return Settings.caravansEnabled;
    }

    private static void Prefix(List<Pawn> pawns)
    {
        pawns.RemoveAll(x => x.RaceProps.Animal);
    }
}