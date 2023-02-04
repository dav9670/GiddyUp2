using HarmonyLib;
using UnityEngine;
using Verse;

namespace GiddyUp.Harmony
{
    //This just moves their label down so it's below the mounted animal
    [HarmonyPatch(typeof(PawnUIOverlay), nameof(PawnUIOverlay.DrawPawnGUIOverlay))]
    
    class PawnUIOverlay_DrawPawnGUIOverlay
    {
        static bool Prefix(PawnUIOverlay __instance)
        {
            if (!ExtendedDataStorage.isMounted.Contains(__instance.pawn.thingIDNumber)) return true;
            var data =  ExtendedDataStorage.GUComp[__instance.pawn.thingIDNumber];
           
            Vector2 pos = GenMapUI.LabelDrawPosFor(__instance.pawn, -(data.drawOffset + 0.6f));
            GenMapUI.DrawPawnLabel(__instance.pawn, pos, 1f, 9999f, null, GameFont.Tiny, true, true);
            return false; 
        }
    }
}
