using Battlemounts.Utilities;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Battlemounts.Harmony
{
    [HarmonyPatch(typeof(IncidentWorker_Ambush), nameof(IncidentWorker_Ambush.DoExecute))]
    static class IncidentWorker_Ambush_DoExecute
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.battleMountsEnabled;
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in instructions)
            {
                yield return code;
                if (code.operand as MethodInfo == AccessTools.Method(typeof(IncidentWorker_Ambush), nameof(IncidentWorker_Ambush.PostProcessGeneratedPawnsAfterSpawning)))
                {
                    yield return new CodeInstruction(OpCodes.Ldarga_S, 2); //load generated pawns as parameter
                    yield return new CodeInstruction(OpCodes.Ldarg_1); //load incidentparms as parameter
                    yield return new CodeInstruction(OpCodes.Call, typeof(EnemyMountUtility).GetMethod(nameof(EnemyMountUtility.MountAnimals)));
                }
            }
        }
    }
}
