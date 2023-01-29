using GiddyUp.Storage;
using GiddyUp.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace GiddyUp.HarmonyPlaceholder
{
    
    [HarmonyPatch(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.DrawPos), MethodType.Getter)]
    static class Pawn_DrawTracker_DrawPos
    {
        static Vector3 Postfix(Vector3 __result, Pawn_DrawTracker __instance, Pawn ___pawn)
        {
            try
            {
                if (!Setup.isMounted.Contains(___pawn.thingIDNumber)) return __result;
                ExtendedPawnData pawnData = Setup._extendedDataStorage.GetExtendedDataFor(___pawn.thingIDNumber);
                if (pawnData == null) Log.Warning("1");
                if (pawnData.mount == null) Log.Warning("2");
                if (pawnData.mount.Drawer == null) Log.Warning("3");
                __result = pawnData.mount.Drawer.DrawPos;

                if (pawnData.drawOffset != -1)
                {
                    __result.z = __result.z + pawnData.drawOffset;
                }
                var modX = pawnData.mount.def.GetModExtension<DrawingOffsetPatch>();
                if (modX != null) __result += AddCustomOffsets(___pawn, pawnData, modX);
                __result.y += 0.1f;
            }
            catch (System.Exception ex)
            {
                Log.Error("[Giddy-Up] Draw error:\n" + ex);
            }
            
            
            return __result;
        }

        private static Vector3 AddCustomOffsets(Pawn __instance, ExtendedPawnData pawnData, DrawingOffsetPatch customOffsets)
        {
            if (__instance.rotationInt == Rot4.North)
            {
                return customOffsets.northOffset;
            }
            if (__instance.rotationInt == Rot4.South)
            {
                return customOffsets.southOffset;
            }
            if (__instance.rotationInt == Rot4.East)
            {
                return customOffsets.eastOffset;
            }
            return customOffsets.westOffset;
        }
    }
    
}
