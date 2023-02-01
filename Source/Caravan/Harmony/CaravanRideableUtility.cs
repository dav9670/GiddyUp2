using GiddyUp.Utilities;
using HarmonyLib;
using RimWorld.Planet;
using System;
using Verse;
using System.Collections.Generic;
using System.Reflection;

namespace GiddyUpCaravan.Harmony
{
    class CaravanRideableUtility_IsCaravanRideable
    {
        static bool Prepare()
        {
            return GiddyUp.ModSettings_GiddyUp.caravansEnabled;
        }
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(CaravanRideableUtility), nameof(CaravanRideableUtility.IsCaravanRideable), new Type[] {typeof(Pawn)});
            yield return AccessTools.Method(typeof(CaravanRideableUtility), nameof(CaravanRideableUtility.IsCaravanRideable), new Type[] {typeof(ThingDef)});
        }
        static bool Prefix(Pawn pawn, ref bool __result)
        {
            if(IsMountableUtility.isMountable(pawn))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
