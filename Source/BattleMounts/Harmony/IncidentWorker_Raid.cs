﻿using GiddyUp;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace BattleMounts.Harmony;

[HarmonyPatch(typeof(IncidentWorker_Raid), nameof(IncidentWorker_Raid.TryGenerateRaidInfo))]
internal static class Patch_TryGenerateRaidInfo
{
    private static bool Prepare()
    {
        return Settings.battleMountsEnabled;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return instructions.MethodReplacer(
            AccessTools.Method(typeof(PawnsArrivalModeWorker), nameof(PawnsArrivalModeWorker.Arrive)),
            AccessTools.Method(typeof(Patch_TryGenerateRaidInfo), nameof(MountAnimals)));
    }

    public static void MountAnimals(PawnsArrivalModeWorker instance, List<Pawn> pawns, IncidentParms parms)
    {
        if (pawns.Count == 0) return;

        parms.raidArrivalMode.Worker.Arrive(pawns, parms);
        if (!(parms.raidArrivalMode == null ||
              parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkIn) ||
            (parms.raidStrategy != null &&
             parms.raidStrategy.workerClass == ResourceBank.RaidStrategyWorker_Siege))
            return;
        MountUtility.GenerateMounts(ref pawns, parms);
    }
}