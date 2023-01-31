using GiddyUp.Jobs;
using GiddyUp.Storage;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Multiplayer.API;
using Verse;
using Verse.AI;

namespace GiddyUp.Utilities
{
    public class NPCMountUtility
    {
        public static void ConfigureSpawnedAnimal(Pawn pawn, ref Pawn animal)
        {
            ExtendedPawnData pawnData = Setup._extendedDataStorage.GetExtendedDataFor(pawn.thingIDNumber);
            ExtendedPawnData animalData = Setup._extendedDataStorage.GetExtendedDataFor(animal.thingIDNumber);
            pawnData.Mount = animal;
            TextureUtility.SetDrawOffset(pawnData);
            animal.mindState.duty = new PawnDuty(DutyDefOf.Defend);
            if (animal.jobs == null)
            {
                animal.jobs = new Pawn_JobTracker(animal);
            }
            Job jobAnimal = new Job(ResourceBank.JobDefOf.Mounted, pawn);
            jobAnimal.count = 1;
            animal.jobs.TryTakeOrderedJob(jobAnimal);
            animalData.ownedBy = pawn;
            animal.playerSettings = new Pawn_PlayerSettings(animal);
            animal.training.Train(TrainableDefOf.Obedience, pawn);
            pawnData.owning = animal;

        }
        static PawnKindDef determinePawnKind(Map map, Predicate<PawnKindDef> isAnimal, float inBiomeWeightNormalized, float outBiomeWeightNormalized, int rndInt, int pawnHandlingLevel, List<string> factionFarmAnimalRestrictions, List<string> factionWildAnimalRestrictions, IncidentParms parms)
        {
            PawnKindDef pawnKindDef = null;
            float averageCommonality = AverageAnimalCommonality(map);
            Predicate<PawnKindDef> canUseAnimal = (PawnKindDef a) => map.mapTemperature.SeasonAcceptableFor(a.race) && IsMountableUtility.isAllowedInModOptions(a.shortHash) && parms.points > a.combatPower * 2f;
            Rand.PushState();
            if (factionWildAnimalRestrictions.NullOrEmpty() && rndInt <= inBiomeWeightNormalized)
            {
                (from a in map.Biome.AllWildAnimals
                 where canUseAnimal(a)
                 select a).TryRandomElementByWeight((PawnKindDef def) => calculateCommonality(def, map, pawnHandlingLevel), out pawnKindDef);
            }
            else if (rndInt <= inBiomeWeightNormalized + outBiomeWeightNormalized)
            {
                (from a in Setup.animalsWithBiome
                 where isAnimal(a)
                 && canUseAnimal(a)
                 && (factionWildAnimalRestrictions.NullOrEmpty() || factionWildAnimalRestrictions.Contains(a.defName))
                 select a).TryRandomElementByWeight((PawnKindDef def) => calculateCommonality(def, map, pawnHandlingLevel, averageCommonality), out pawnKindDef);
            }
            else
            {
                (from a in Setup.animalsWithoutBiome
                 where isAnimal(a)
                 && canUseAnimal(a)
                 && (factionFarmAnimalRestrictions.NullOrEmpty() || factionFarmAnimalRestrictions.Contains(a.defName))
                 select a).TryRandomElementByWeight((PawnKindDef def) => calculateCommonality(def, map, pawnHandlingLevel, averageCommonality), out pawnKindDef);
            }
            Rand.PopState();
            return pawnKindDef;
        }
        static float AverageAnimalCommonality(Map map)
        {
            float sum = 0;
            foreach (PawnKindDef animalKind in map.Biome.AllWildAnimals)
            {
                sum += map.Biome.CommonalityOfAnimal(animalKind);
            }
            return sum / map.Biome.AllWildAnimals.Count();
        }
        static float calculateCommonality(PawnKindDef def, Map map, int pawnHandlingLevel, float averageCommonality = 0)
        {
            float commonality;
            if (averageCommonality == 0)
            {
                commonality = map.Biome.CommonalityOfAnimal(def);
            }
            else
            {
                commonality = averageCommonality;
            }
                
            //minimal level to get bonus. 
            pawnHandlingLevel = pawnHandlingLevel > 5 ? pawnHandlingLevel - 5 : 0;

            //Common animals more likely when pawns have low handling, and rare animals more likely when pawns have high handling.  
            float commonalityAdjusted = commonality * ((15f - (float)commonality)) / 15f + (1 - commonality) * ((float)pawnHandlingLevel) / 15f;
            //Wildness decreases the likelyhood of the mount being picked. Handling level mitigates this. 
            float wildnessPenalty = 1 - (def.RaceProps.wildness * ((15f - (float)pawnHandlingLevel) / 15f));

            //Log.Message("name: " + def.defName + ", commonality: " + commonality + ", pawnHandlingLevel: " + pawnHandlingLevel + ", wildness: " + def.RaceProps.wildness + ", commonalityBonus: " + commonalityAdjusted + ", wildnessPenalty: " + wildnessPenalty + ", result: " + commonalityAdjusted * wildnessPenalty);
            return commonalityAdjusted * wildnessPenalty;
        }
        static int getMountChance(IncidentParms parms, int mountChance, int mountChanceTribal)
        {
            if(parms.faction == null)
            {
                return -1;
            }
            if (parms.faction.def == FactionDefOf.Ancients || parms.faction.def == FactionDefOf.AncientsHostile)
            {
                return mountChanceTribal;
            }
            else if (parms.faction.def != FactionDefOf.Mechanoid)
            {
                return mountChance;
            }
            else
            {
                return -1;
            }
        }
    }
}

