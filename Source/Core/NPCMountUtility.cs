using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
//using Multiplayer.API;
using Verse;
using Verse.AI;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp
{
    public class NPCMountUtility
    {
        public static HashSet<PawnKindDef> animalsWithBiome = new HashSet<PawnKindDef>(), animalsWithoutBiome = new HashSet<PawnKindDef>();
        public static bool GenerateMounts(ref List<Pawn> list, IncidentParms parms, int inBiomeWeight, int outBiomeWeight, int nonWildWeight, int mountChance, int mountChanceTribal)
        {
            //if (MP.enabled) return false; // Best we can do for now
            Map map = parms.target as Map;
            if (map == null)
            {
                Caravan caravan = (Caravan)parms.target;
                int tile = caravan.Tile;
                map = Current.Game.FindMap(tile);
                if (map == null)
                {
                    return false;
                }
            }

            Predicate<PawnKindDef> isAnimal = (PawnKindDef d) => d.race != null && d.race.race.Animal;

            mountChance = GetMountChance(parms, mountChance, mountChanceTribal);
            if (mountChance == -1)//wrong faction
            {
                return false;
            }
            List<string> factionFarmAnimalRestrictions = new List<string>();
            List<string> factionWildAnimalRestrictions = new List<string>();
            
            FactionRestrictionsPatch factionRestrictions = parms.faction.def.GetModExtension<FactionRestrictionsPatch>();
            if (factionRestrictions != null)
            {
                factionFarmAnimalRestrictions = factionRestrictions.GetAllowedNonWildAnimalsAsList();
                factionWildAnimalRestrictions = factionRestrictions.GetAllowedWildAnimalsAsList();

                if(factionRestrictions.mountChance > -1)
                {
                    mountChance = factionRestrictions.mountChance;
                }

                if(!factionWildAnimalRestrictions.NullOrEmpty() && factionFarmAnimalRestrictions.NullOrEmpty() && factionRestrictions.wildAnimalWeight >= 0)
                {
                    inBiomeWeight = 0;
                    nonWildWeight = 0;
                    outBiomeWeight = factionRestrictions.wildAnimalWeight;
                }
                if (factionWildAnimalRestrictions.NullOrEmpty() && !factionFarmAnimalRestrictions.NullOrEmpty() && factionRestrictions.nonWildAnimalWeight >= 0)
                {
                    inBiomeWeight = 0;
                    outBiomeWeight = 0;
                    nonWildWeight = factionRestrictions.nonWildAnimalWeight;
                }
                if (!factionWildAnimalRestrictions.NullOrEmpty() && !factionFarmAnimalRestrictions.NullOrEmpty())
                {
                    inBiomeWeight = 0;
                    if(factionRestrictions.wildAnimalWeight >= 0)
                    outBiomeWeight = factionRestrictions.wildAnimalWeight;
                    if (factionRestrictions.nonWildAnimalWeight >= 0)
                    nonWildWeight = factionRestrictions.nonWildAnimalWeight;
                }
            }

            int totalWeight = inBiomeWeight + outBiomeWeight + nonWildWeight;
            float inBiomeWeightNormalized = (float)inBiomeWeight / (float)totalWeight * 100f;
            float outBiomeWeightNormalized = (float)outBiomeWeight / (float)totalWeight * 100f;

            List<Pawn> animals = new List<Pawn>();
            foreach (Pawn pawn in list)
            {
                //TODO add chance
                PawnKindDef pawnKindDef = null;

                if (!pawn.RaceProps.Humanlike || pawn.kindDef == PawnKindDefOf.Slave) continue;

                //changing from System.Random to Verse.Rand for better multiplayer compatibility
                int rndInt = Rand.Range(1, 100);

                CustomMountsPatch modExtension = pawn.kindDef.GetModExtension<CustomMountsPatch>();
                if (modExtension != null)
                {
                    if(modExtension.mountChance <= rndInt) continue;

                    Rand.PushState();
                    bool found = modExtension.possibleMounts.TryRandomElementByWeight((KeyValuePair<String, int> mount) => mount.Value, out KeyValuePair<String, int> selectedMount);
                    Rand.PopState();
                    if (found) pawnKindDef = DefDatabase<PawnKindDef>.GetNamed(selectedMount.Key);
                }
                else
                {
                    if (mountChance <= rndInt || !pawn.RaceProps.Humanlike)
                    {
                        continue;
                    }
                    int pawnHandlingLevel = pawn.skills.GetSkill(SkillDefOf.Animals).Level;

                    pawnKindDef = DeterminePawnKind(map, isAnimal, inBiomeWeightNormalized, outBiomeWeightNormalized, rndInt, pawnHandlingLevel, factionFarmAnimalRestrictions, factionWildAnimalRestrictions, parms);
                }
                if (pawnKindDef == null)
                {
                    return false;
                }
                Pawn animal = PawnGenerator.GeneratePawn(pawnKindDef, parms.faction);
                GenSpawn.Spawn(animal, pawn.Position, map, parms.spawnRotation);
                ConfigureSpawnedAnimal(pawn, ref animal);
                animals.Add(animal);

            }
            list.AddRange(animals);
            return true;
        }
        static void ConfigureSpawnedAnimal(Pawn pawn, ref Pawn animal)
        {
            ExtendedPawnData pawnData = ExtendedDataStorage.GUComp[pawn.thingIDNumber];
            ExtendedPawnData animalData = ExtendedDataStorage.GUComp[animal.thingIDNumber];
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
            animalData.reservedBy = pawn;
            animal.playerSettings = new Pawn_PlayerSettings(animal);
            animal.training.Train(TrainableDefOf.Obedience, pawn);
            pawnData.reservedMount = animal;
        }
        static PawnKindDef DeterminePawnKind(Map map, Predicate<PawnKindDef> isAnimal, float inBiomeWeightNormalized, float outBiomeWeightNormalized, int rndInt, int pawnHandlingLevel, List<string> factionFarmAnimalRestrictions, List<string> factionWildAnimalRestrictions, IncidentParms parms)
        {
            PawnKindDef pawnKindDef = null;
            float averageCommonality = AverageAnimalCommonality(map);
            Predicate<PawnKindDef> canUseAnimal = (PawnKindDef a) => map.mapTemperature.SeasonAcceptableFor(a.race) && Settings.mountableCache.Contains(a.shortHash) && parms.points > a.combatPower * 2f;
            Rand.PushState();
            if (factionWildAnimalRestrictions.NullOrEmpty() && rndInt <= inBiomeWeightNormalized)
            {
                map.Biome.AllWildAnimals.Where(x => canUseAnimal(x)).TryRandomElementByWeight((PawnKindDef def) => CalculateCommonality(def, map, pawnHandlingLevel), out pawnKindDef);
            }
            else if (rndInt <= inBiomeWeightNormalized + outBiomeWeightNormalized)
            {
                animalsWithBiome.Where(x => isAnimal(x) && canUseAnimal(x) && (factionWildAnimalRestrictions.NullOrEmpty() || factionWildAnimalRestrictions.Contains(x.defName))).
                    TryRandomElementByWeight((PawnKindDef def) => CalculateCommonality(def, map, pawnHandlingLevel, averageCommonality), out pawnKindDef);
            }
            else
            {
                animalsWithoutBiome.Where(x => isAnimal(x) && canUseAnimal(x) && (factionFarmAnimalRestrictions.NullOrEmpty() || factionFarmAnimalRestrictions.Contains(x.defName))).
                    TryRandomElementByWeight((PawnKindDef def) => CalculateCommonality(def, map, pawnHandlingLevel, averageCommonality), out pawnKindDef);
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
        static float CalculateCommonality(PawnKindDef def, Map map, int pawnHandlingLevel, float averageCommonality = 0)
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
        static int GetMountChance(IncidentParms parms, int mountChance, int mountChanceTribal)
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

