using System.Linq;
using GiddyUp;
using HarmonyLib;
using Verse;

namespace GiddyUpCore.Core.Harmony;

[HarmonyPatch(typeof(PathUtility), nameof(PathUtility.GetAllowedArea))]
public class Patch_PathUtility_GetAllowedArea
{
     /// <summary>
     /// Used to make riders avoid going into no mount zones
     /// </summary>
     static void Postfix(ref Area? __result, Pawn? pawn)
     {
         //Prepare variables and check if an area merge for riders should be done
         if (pawn == null || !pawn.IsMounted() || pawn.Faction == null || !pawn.Faction.def.isPlayer) return;
         var map = pawn.Map;
         ExtendedDataStorage.GUComp.areaNoMount.TryGetValue(map.uniqueID, out var areaNoMount);
         if (areaNoMount == null) return;
         var length = areaNoMount.innerGrid.arr.Length;

         var invertedArea = new Area_GU(map.areaManager, ResourceBank.AreaNoMount)
         {
             innerGrid = new BoolGrid() { trueCountInt = areaNoMount.innerGrid.trueCountInt, arr = areaNoMount.innerGrid.arr }
         };
         invertedArea.Invert();
			
         //Merge with original passed area
         if (__result != null)
         {
             var baseArr = __result.innerGrid.arr;
             for (int i = 0; i < length; i++)
             {
                 if (!baseArr[i]) invertedArea.innerGrid.arr[i] = false;
             }
         }

         __result = invertedArea;
     }
}
