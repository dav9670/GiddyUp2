using HarmonyLib;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace GiddyUp.Harmony;

//This just moves their label down so it's below the mounted animal
[HarmonyPatch(typeof(PawnUIOverlay), nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
internal class Patch_DrawPawnGUIOverlay
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var label = generator.DefineLabel();
        var labelNext = false;
        foreach (var code in instructions)
            if (!labelNext && code.opcode == OpCodes.Call && code.OperandIs(AccessTools.Method(typeof(GenMapUI),
                    nameof(GenMapUI.DrawPawnLabel), new System.Type[]
                    {
                        typeof(Pawn),
                        typeof(Vector2),
                        typeof(float),
                        typeof(float),
                        typeof(Dictionary<string, string>),
                        typeof(GameFont),
                        typeof(bool),
                        typeof(bool)
                    })))
            {
                labelNext = true;
            }
            else if (labelNext)
            {
                code.labels.Add(label);
                break;
            }

        yield return new CodeInstruction(OpCodes.Ldarg_0);
        yield return new CodeInstruction(OpCodes.Call, typeof(Patch_DrawPawnGUIOverlay).GetMethod(nameof(OffsetLabel)));
        yield return new CodeInstruction(OpCodes.Brfalse_S, label);

        foreach (var code in instructions) yield return code;
    }

    public static bool OffsetLabel(PawnUIOverlay __instance)
    {
        if (!__instance.pawn.IsMounted()) return true;
        var data = __instance.pawn.GetGUData();

        var pos = GenMapUI.LabelDrawPosFor(__instance.pawn, -(data.drawOffset + 0.75f));
        GenMapUI.DrawPawnLabel(__instance.pawn, pos, 1f, 9999f, null, GameFont.Tiny, true, true);
        return false;
    }
}