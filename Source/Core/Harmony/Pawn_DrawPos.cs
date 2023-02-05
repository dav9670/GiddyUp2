using HarmonyLib;
using UnityEngine;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.DrawPos), MethodType.Getter)]
    static class Pawn_DrawTracker_DrawPos
    {
        static void Postfix(ref Vector3 __result, Pawn ___pawn)
        {
            if (!ExtendedDataStorage.isMounted.Contains(___pawn.thingIDNumber)) return;
            ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[___pawn.thingIDNumber];

            //Failsafe. Should never happen but too dangerous to chance
            if (pawnData.mount == null)
            {
                pawnData.Reset();
                return;
            }
            var offset = pawnData.mount.Drawer.DrawPos;
            if (pawnData.drawOffset != -1)
            {
                offset.z = offset.z + pawnData.drawOffset;
            }
            //Apply custom offsets
            var rotation = ___pawn.rotationInt;
            var modX = pawnData.mount.def.GetModExtension<DrawingOffsetPatch>();
            if (modX != null) offset += AddCustomOffsets(rotation, modX);
            
            if (rotation == Rot4.South && Settings.drawRulesCache.Contains(pawnData.mount.def.shortHash))
            {
                offset.y -= 0.1f;
            }
            else offset.y += 0.1f;

            __result = offset;
        }

        static Vector3 AddCustomOffsets(Rot4 rotation, DrawingOffsetPatch customOffsets)
        {
            if (rotation == Rot4.North)
            {
                return customOffsets.northOffset;
            }
            if (rotation == Rot4.South)
            {
                return customOffsets.southOffset;
            }
            if (rotation == Rot4.East)
            {
                return customOffsets.eastOffset;
            }
            return customOffsets.westOffset;
        }
    }
}
