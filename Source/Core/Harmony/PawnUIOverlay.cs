using HarmonyLib;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace GiddyUp.Harmony
{
    //This just moves their label down so it's below the mounted animal
    [HarmonyPatch(typeof(PawnUIOverlay), nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
    
    class Patch_DrawPawnGUIOverlay
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var label = generator.DefineLabel();
            foreach (var code in instructions)
            {
                if (code.opcode == OpCodes.Ret)
                {
                    code.labels.Add(label);
                    break;
                }
            }
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Call, typeof(Patch_DrawPawnGUIOverlay).GetMethod(nameof(Patch_DrawPawnGUIOverlay.OffsetLabel)));
            yield return new CodeInstruction(OpCodes.Brfalse_S, label);

            foreach (var code in instructions) yield return code;
        }
        public static bool OffsetLabel(PawnUIOverlay __instance)
        {
            if (!__instance.pawn.IsMounted()) return true;
            var data = __instance.pawn.GetGUData();
           
            Vector2 pos = GenMapUI.LabelDrawPosFor(__instance.pawn, -(data.drawOffset + 0.75f));
            GenMapUI.DrawPawnLabel(__instance.pawn, pos, 1f, 9999f, null, GameFont.Tiny, true, true);
            return false; 
        }
    }
}
