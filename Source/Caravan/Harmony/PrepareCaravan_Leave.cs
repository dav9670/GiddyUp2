using GiddyUp;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;
using System.Reflection;
using System.Collections.Generic;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUpCaravan.Harmony;

[HarmonyPatch]
internal static class Patch_PrepareCaravan_Leave
{
    private static bool Prepare()
    {
        return Settings.caravansEnabled;
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_Leave),
            nameof(LordToil_PrepareCaravan_Leave.UpdateAllDuties));
        //yield return AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherDownedPawns), nameof(LordToil_PrepareCaravan_GatherDownedPawns.UpdateAllDuties));
    }

    private static void Postfix(Lord ___lord)
    {
        foreach (var pawn in ___lord.ownedPawns)
        {
            if (pawn.RaceProps.Animal)
                continue;
            var pawnData = pawn.GetGUData();
            if (pawnData.reservedMount != null)
            {
                if (pawnData.reservedMount.IsStillMountable(pawn, out var reason))
                    pawn.GoMount(pawnData.reservedMount);
                else if (Settings.logging)
                    Log.Message("[Giddy-Up] " + pawn.thingIDNumber.ToString() +
                                " cannot mount their assigned caravan animal. Reason: " + reason.ToString());
            }
        }
    }
}