using HarmonyLib;
using System.Linq;
using Verse;
using Verse.AI;

namespace GiddyUp.Harmony;

// [HarmonyPatch(typeof(PathFinder), nameof(PathFinder.GetAllowedArea))]
// internal class Patch_GetAllowedArea
// {
//     private static void Postfix(ref Area __result, Pawn pawn)
//     {
//         //Prepare variables and check if an area merge for riders should be done
//         if (pawn == null || !pawn.IsMounted() || pawn.Faction == null || !pawn.Faction.def.isPlayer) return;
//         var map = pawn.Map;
//         ExtendedDataStorage.GUComp.areaNoMount.TryGetValue(map.uniqueID, out var areaNoMount);
//         if (areaNoMount == null) return;
//         var length = areaNoMount.innerGrid.arr.Length;
//
//         //Make a working, temp area based on an inverted no-ride zone
//         Area mergedArea = new Area_GU
//         {
//             areaManager = map.areaManager,
//             innerGrid = new BoolGrid
//                 { trueCountInt = areaNoMount.innerGrid.trueCountInt, arr = areaNoMount.innerGrid.arr.ToArray() }
//         };
//         mergedArea.innerGrid.Invert();
//
//         //Merge with original passed area
//         if (__result != null)
//         {
//             var baseArr = __result.innerGrid.arr;
//             for (var i = 0; i < length; i++)
//                 if (!baseArr[i])
//                     mergedArea.innerGrid.arr[i] = false;
//         }
//
//         //Switch reference pointer to our temp area
//         __result = mergedArea;
//     }
// }