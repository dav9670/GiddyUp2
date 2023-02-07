using GiddyUp;
using RimWorld;
using System.Collections.Generic;
using Verse;
//using Multiplayer.API;
using Setting = GiddyUp.ModSettings_GiddyUp;

namespace BattleMounts
{
    class EnemyMountUtility
    {
        //[SyncMethod]
        public static void MountAnimals(ref List<Pawn> list, IncidentParms parms)
        {
            //Only allow raids tha are edge walk ins, except sieges
            if (list.Count == 0 || !(parms.raidArrivalMode == null || parms.raidArrivalMode == PawnsArrivalModeDefOf.EdgeWalkIn) ||
                (parms.raidStrategy != null && parms.raidStrategy.workerClass == typeof(RaidStrategyWorker_Siege)))
            {
                return;
            }
            MountUtility.GenerateMounts(ref list, parms, Setting.inBiomeWeight, Setting.outBiomeWeight, Setting.nonWildWeight, Setting.enemyMountChance, Setting.enemyMountChanceTribal);
            
            foreach(Pawn pawn in list)
            {
                if (pawn.equipment == null)
                {
                    pawn.equipment = new Pawn_EquipmentTracker(pawn);
                }
            }
        }
    }
}
