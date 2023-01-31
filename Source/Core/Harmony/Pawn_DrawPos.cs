using GiddyUp.Storage;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace GiddyUp.Harmony
{
    [HarmonyPatch(typeof(Pawn_DrawTracker), nameof(Pawn_DrawTracker.DrawPos), MethodType.Getter)]
    static class Pawn_DrawTracker_DrawPos
    {
        static void Postfix(ref Vector3 __result, Pawn ___pawn)
        {
            if (!Setup.isMounted.Contains(___pawn.thingIDNumber)) return;
            ExtendedPawnData pawnData = Setup._extendedDataStorage.GetExtendedDataFor(___pawn.thingIDNumber);
            var offset = pawnData.mount.Drawer.DrawPos;

            if (pawnData.drawOffset != -1)
            {
                offset.z = offset.z + pawnData.drawOffset;
            }
            var modX = pawnData.mount.def.GetModExtension<DrawingOffsetPatch>();
            if (modX != null) offset += AddCustomOffsets(___pawn.rotationInt, modX);
            offset.y += 0.1f;
            
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
