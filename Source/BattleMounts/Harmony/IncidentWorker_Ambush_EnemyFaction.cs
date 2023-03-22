using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using GiddyUp;
using Verse;
//using Multiplayer.API;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace BattleMounts.Harmony
{
    [HarmonyPatch(typeof(IncidentWorker_Ambush), nameof(IncidentWorker_Ambush.DoExecute))]
    static class Patch_IncidentWorker_Ambush
    {
        static bool Prepare()
        {
            return Settings.battleMountsEnabled;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool done = false;
            foreach (var code in instructions)
            {
                yield return code;
                if (!done && code.operand as MethodInfo == AccessTools.Method(typeof(IncidentWorker_Ambush), nameof(IncidentWorker_Ambush.PostProcessGeneratedPawnsAfterSpawning)))
                {
                    yield return new CodeInstruction(OpCodes.Ldarga_S, 2); //load generated pawns as parameter
                    yield return new CodeInstruction(OpCodes.Ldarg_1); //load incidentparms as parameter
                    yield return new CodeInstruction(OpCodes.Call, typeof(Patch_IncidentWorker_Ambush).GetMethod(nameof(Patch_IncidentWorker_Ambush.MountAnimals)));
                    done = true;
                }
            }
        }

        public static void MountAnimals(ref List<Pawn> list, IncidentParms parms)
        {
            //Only allow raids tha are edge walk ins, except sieges
            if (list.Count == 0 || !(parms.raidArrivalMode == null || parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkIn) ||
                (parms.raidStrategy != null && parms.raidStrategy.workerClass == ResourceBank.RaidStrategyWorker_Siege))
            {
                return;
            }
            MountUtility.GenerateMounts(ref list, parms);
        }
    }
}