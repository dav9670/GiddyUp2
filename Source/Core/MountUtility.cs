using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
//using Multiplayer.API;
using Verse;
using Verse.AI;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp
{
	static class MountUtility
	{
		public static HashSet<PawnKindDef> allWildAnimals = new HashSet<PawnKindDef>(), allDomesticAnimals = new HashSet<PawnKindDef>();
		public enum GiveJobMethod { Inject, Try, Instant, Think };
		enum ListToUse { Local, Foreign, Domestic };
		
		public static ThinkResult? GoMount(this Pawn rider, Pawn animal, GiveJobMethod giveJobMethod = GiveJobMethod.Inject, ThinkResult? thinkResult = null, Job currentJob = null)
		{
			//Immediately mount the pawn on the animal. This is done when the mount job finishes, or emulating that it has already finished such as when pawns come in pre-mounted
			if (giveJobMethod == GiveJobMethod.Instant) rider.Mount(animal);
			
			//This prompts them to mount up before they carry out another job they were planning to do
			else if (giveJobMethod == GiveJobMethod.Inject)
			{
				if (currentJob != null) rider.jobs?.jobQueue?.EnqueueFirst(currentJob);
				if (thinkResult != null) return new ThinkResult(new Job(ResourceBank.JobDefOf.Mount, animal) {count = 1}, thinkResult.Value.SourceNode, thinkResult.Value.Tag, false);
			}
			
			//This has them mount after they're done doing their current task
			else if (giveJobMethod == GiveJobMethod.Try)
			{
				animal.jobs.StopAll();
				animal.jobs.EndCurrentJob(JobCondition.InterruptForced, false, false); //The StopAll above will trigger the WaitForRider job. This will stop it.
				animal.pather.StopDead();
				rider.jobs.TryTakeOrderedJob(new Job(ResourceBank.JobDefOf.Mount, animal) {count = 1});
			}
			
			//TODO: May be possible to merge this and Inject together. Also not quite working right. Only used by colonist caravan leaving
			else if (giveJobMethod == GiveJobMethod.Think)
			{
				rider.jobs?.StartJob(new Job(ResourceBank.JobDefOf.Mount, animal) {count = 1}, JobCondition.InterruptOptional, null, true, false);
			}
			
			return null;
		}
		static void Mount(this Pawn rider, Pawn animal)
		{
			//First check if the pawn had a mount to begin with...
			ExtendedPawnData pawnData = rider.GetGUData();
			if (animal == null) animal = pawnData.reservedMount;
			
			//If they did...
			if (animal != null)
			{
				//Instantly mount, as if the mount jobdriver had just finished
				pawnData.mount = animal;
				ExtendedDataStorage.isMounted.Add(rider.thingIDNumber);
				pawnData.ReservedMount = animal;
				animal.GetGUData().ReservedBy = rider;
				
				//Break ropes if there are any
				if (animal.roping?.IsRoped ?? false) animal.roping.BreakAllRopes();

				//Set the offset
				pawnData.drawOffset = TextureUtility.FetchCache(animal);
				
				//Set the animal job and state
				if (animal.CurJobDef != ResourceBank.JobDefOf.Mounted)
				{
					if (animal.HostileTo(Current.gameInt.worldInt.factionManager.ofPlayer)) animal.mindState.duty = new PawnDuty(DutyDefOf.Defend);
					animal.jobs.TryTakeOrderedJob(new Job(ResourceBank.JobDefOf.Mounted, rider) { count = 1});
				}
			}
		}
		public static bool GoDismount(this Pawn rider, Pawn animal, GiveJobMethod giveJobMethod, IntVec3 target = default(IntVec3))
		{
			if (animal == null) return false;
			bool isGuest = animal.Faction == null || !animal.Faction.def.isPlayer;
			Area areaFound = rider.Map.areaManager.GetLabeled(isGuest ? ResourceBank.VisitorAreaDropMount : ResourceBank.AreaDropMount);

			IntVec3 targetLoc;
			//Any dismount spots available?
			if (areaFound != null && areaFound.innerGrid.TrueCount > 0) targetLoc = areaFound.GetClosestAreaLoc(target == default(IntVec3) ? rider.Position : target);
			
			//If not, use a pen?
			else 
			{
				var pen = AnimalPenUtility.GetPenAnimalShouldBeTakenTo(rider, animal, out string failReason, true, true, false, true);
				if (pen != null) targetLoc = AnimalPenUtility.FindPlaceInPenToStand(pen, rider);
				else return false; //Neither a pen or spot available
			}
			//Can reach
			if (rider.Map.reachability.CanReach(rider.Position, targetLoc, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
			{
				rider.jobs.jobQueue.EnqueueFirst(new Job(ResourceBank.JobDefOf.Dismount, targetLoc) { count = 1});
				return true;
			}
			else Messages.Message("GU_Car_NotReachable_DropAnimal_NPC_Message".Translate(), new RimWorld.Planet.GlobalTargetInfo(targetLoc, rider.Map), MessageTypeDefOf.NegativeEvent);

			return false;
		}
		public static void Dismount(this Pawn rider, Pawn animal, ExtendedPawnData pawnData, bool clearReservation = false, IntVec3 parkLoc = default(IntVec3))
		{
			ExtendedDataStorage.isMounted.Remove(rider.thingIDNumber);
			if (pawnData == null) pawnData = rider.GetGUData();
			pawnData.mount = null;
			if (Settings.logging) Log.Message("[Giddy-Up] pawn " + rider.thingIDNumber.ToString() + " no longer riding  " + (animal?.thingIDNumber.ToString() ?? "NULL"));

			//Normally should not happen, may come in null from sanity checks. Odd bugs or save/reload conflicts between version changes
			ExtendedPawnData animalData;
			if (animal == null) ExtendedDataStorage.GUComp.ReverseLookup(rider.thingIDNumber, out animalData);
			else animalData = animal.GetGUData();

			//Reservation handling
			if (clearReservation) 
			{
				pawnData.ReservedMount = null;
				animalData.ReservedBy = null;
			}
			
			//Reset free locomotion
			if (animal == null) return; //We're done here
			animal.Drawer.tweener = new PawnTweener(animal);
			animal.pather.ResetToCurrentPosition();
			
			//========Post-dismount behavior======
			//If this is a visitor's animal, keep it from wandering off
			if (!rider.Faction.def.isPlayer && animal.mindState.duty != null) animal.mindState.duty.focus = new LocalTargetInfo(animal.Position);
			
			//If the animal is being dismounted outside of a pen and it's a roamer, hitch it
			if (AnimalPenUtility.NeedsToBeManagedByRope(animal) && AnimalPenUtility.GetCurrentPenOf(animal, true) == null && //Needs to be roped and not already penned?
				 (animal.roping == null || !animal.roping.IsRoped) && //Skip already roped
				 !animal.Position.CloseToEdge(animal.Map, 8) && //Skip pawns near the map edge. They may be entering/exiting the map which triggers dismount calls
				 (animal.Faction.def.isPlayer && animal.inventory != null && animal.inventory.innerContainer.Count == 0)) //Skip guest caravan pack animals
			{
				if (animal.roping == null) animal.roping = new Pawn_RopeTracker(animal); //Not needed, but changes to modded animals could maybe cause issues
				if (Settings.logging) Log.Message("[Giddy-Up] pawn " + rider.thingIDNumber.ToString() + " just roped " + animal.thingIDNumber);
				animal.roping.RopeToSpot(parkLoc == default(IntVec3) ? animal.Position : parkLoc);
			}
			//Follow the rider for a while to give it an opportunity to take a ride back
			if (Settings.rideAndRollEnabled)
			{
				if ((animal.roping == null || !animal.roping.IsRoped) && !rider.Drafted && animal.Faction.def.isPlayer && animalData.reservedBy != null)
				{
					if (animal.CurJobDef != ResourceBank.JobDefOf.Mounted || animal.jobs.curDriver is not Jobs.JobDriver_Mounted mountJob || !mountJob.interrupted)
					{
						animal.jobs.jobQueue.EnqueueFirst(new Job(ResourceBank.JobDefOf.WaitForRider, animalData.reservedBy)
						{
							expiryInterval = 10000,
							checkOverrideOnExpire = true,
							followRadius = 8,
							locomotionUrgency = LocomotionUrgency.Walk
						});
					}
				}
			}
		}
		public static bool GenerateMounts(ref List<Pawn> list, IncidentParms parms)
		{
			//if (MP.enabled) return false; // Best we can do for now
			Map map = parms.target as Map;
			if (map == null)
			{
				Caravan caravan = (Caravan)parms.target;
				int tile = caravan.Tile;
				map = Current.Game.FindMap(tile);
				if (map == null) return false;
			}

			int mountChance = Settings.enemyMountChance;
			float domesticWeight = Settings.nonWildWeight;
			float localWeight = Settings.inBiomeWeight;
			float foreignWeight = Settings.outBiomeWeight;
			
			mountChance = GetMountChance(parms, mountChance);
			if (mountChance == -1) return false; //wrong faction

			//Setup working lists
			GetAnimalArrays(out PawnKindDef[] wildAnimals, out PawnKindDef[] domesticAnimals, out PawnKindDef[] localAnimals);
			
			//Setup weight ranges
			float totalWeight = localWeight + foreignWeight + domesticWeight; //EG 100
			localWeight /= totalWeight * 100f; //EG 20
			foreignWeight /= totalWeight * 100f; //EG 20+10 = 30
			foreignWeight += localWeight;
			float averageCommonality = AverageAnimalCommonality(map.Biome);
			
			bool hasPackAnimals = GetPackAnimals(list, out List<Pawn> packAnimals);
			hasPackAnimals = false;
			var length = list.Count;
			for (int i = 0; i < length; i++)
			{
				Pawn pawn = list[i];
				if (!pawn.RaceProps.Humanlike || pawn.kindDef == PawnKindDefOf.Slave) continue;

				int random = Rand.Range(1, 100);

				PawnKindDef pawnKindDef;
				Pawn animal;
				CustomMounts modExtension = pawn.kindDef.GetModExtension<CustomMounts>();
				if (hasPackAnimals)
				{
					animal = packAnimals.Pop();
					animal.Position = pawn.Position; //Avoids the pop-in glitch
					hasPackAnimals = packAnimals.Count != 0;
					goto Spawned;
				}
				else if (modExtension != null)
				{
					if (modExtension.mountChance <= random) continue;

					Rand.PushState();
					bool found = modExtension.possibleMounts.TryRandomElementByWeight((KeyValuePair<PawnKindDef, int> mount) => mount.Value, out KeyValuePair<PawnKindDef, int> selectedMount);
					Rand.PopState();
					if (found) pawnKindDef = selectedMount.Key;
					else pawnKindDef = null;
				}
				else
				{
					if (mountChance <= random) continue;
					int pawnHandlingLevel = pawn.skills.GetSkill(SkillDefOf.Animals).Level;
					if (pawnHandlingLevel >= Settings.minHandlingLevel) continue;

					PawnKindDef[] workingList;
					bool domestic = false;
					switch (DetermineList(localWeight, foreignWeight, random))
					{
						case ListToUse.Local: workingList = localAnimals; break;
						case ListToUse.Foreign: workingList = wildAnimals; break;
						default: workingList = domesticAnimals; domestic = true; break;
					}

					if (domestic) workingList.Where(x => Settings.mountableCache.Contains(x.shortHash)).
						TryRandomElementByWeight(def => def.race.BaseMarketValue / def.race.GetStatValueAbstract(StatDefOf.CaravanRidingSpeedFactor), out pawnKindDef);
					else workingList.Where(x => map.mapTemperature.SeasonAcceptableFor(x.race) && Settings.mountableCache.Contains(x.shortHash) && parms.points > x.combatPower * 2f).
						TryRandomElementByWeight(def => CalculateCommonality(def, map.Biome, pawnHandlingLevel, averageCommonality), out pawnKindDef);
				}

				//Validate and spawn
				if (pawnKindDef == null) 
				{
					if (Settings.logging) Log.Warning("[Giddy-Up] Could not find any suitable animal for " + pawn.thingIDNumber);
					return false;
				}
				animal = PawnGenerator.GeneratePawn(pawnKindDef, parms.faction);
				GenSpawn.Spawn(animal, pawn.Position, map, parms.spawnRotation);
				list.Add(animal);

				//Set their training
				Spawned:
				if (animal.playerSettings == null) animal.playerSettings = new Pawn_PlayerSettings(animal);
				animal.training.Train(TrainableDefOf.Obedience, pawn);

				//Mount up
				pawn.GoMount(animal, GiveJobMethod.Instant);
			}
			return true;

			#region embedded methods
			int GetMountChance(IncidentParms parms, int mountChance)
			{
				if (parms.faction == null) return -1;
				if (parms.faction.def.techLevel < TechLevel.Industrial) return Settings.enemyMountChancePreInd;
				else if (parms.faction.def != FactionDefOf.Mechanoid) return mountChance;
				return -1;
			}
			ListToUse DetermineList(float localWeight, float foreignWeight, int random)
			{
				if (random < foreignWeight) return ListToUse.Local;
				else if (random >= localWeight && random < foreignWeight) return ListToUse.Foreign;
				return ListToUse.Domestic;
			}
			void GetAnimalArrays(out PawnKindDef[] wildAnimals, out PawnKindDef[] domesticAnimals, out PawnKindDef[] localAnimals)
			{
				FactionRestrictions factionRules = parms.faction?.def?.GetModExtension<FactionRestrictions>();
				if (factionRules != null)
				{
					//Override working list
					wildAnimals = factionRules.allowedWildAnimals;
					var wildAnimalsReadonly = wildAnimals;
					domesticAnimals = factionRules.allowedNonWildAnimals;
					localAnimals = map.Biome.AllWildAnimals.
						Where(x => wildAnimalsReadonly.Contains(x) && map.mapTemperature.SeasonAcceptableFor(x.race) && 
						Settings.mountableCache.Contains(x.shortHash) && parms.points > x.combatPower * 2f).ToArray();

					//Override mount chance
					if (factionRules.mountChance > -1) mountChance = factionRules.mountChance;

					//Apply weights if needed
					if (wildAnimals.Length == 0) localWeight = foreignWeight = 0;
					else if (factionRules.wildAnimalWeight >= 0) foreignWeight = factionRules.wildAnimalWeight;

					if (domesticAnimals.Length == 0) domesticWeight = 0;
					else if (factionRules.nonWildAnimalWeight >= 0) foreignWeight = factionRules.nonWildAnimalWeight;
				}
				else
				{
					wildAnimals = allWildAnimals.ToArray();
					domesticAnimals = allDomesticAnimals.ToArray();
					localAnimals = map.Biome.AllWildAnimals.
						Where(x => map.mapTemperature.SeasonAcceptableFor(x.race) && Settings.mountableCache.Contains(x.shortHash) && parms.points > x.combatPower * 2f).ToArray();
				}

			}
			float AverageAnimalCommonality(BiomeDef biome)
			{
				float sum = 0;
				float count = 0f;
				foreach (PawnKindDef animalKind in biome.AllWildAnimals)
				{
					sum += biome.CommonalityOfAnimal(animalKind);
					++count;
				}
				return sum / count;
			}
			float CalculateCommonality(PawnKindDef def, BiomeDef biome, int pawnHandlingLevel, float averageCommonality = 0)
			{
				float commonality;
				if (averageCommonality == 0) commonality = biome.CommonalityOfAnimal(def);
				else commonality = averageCommonality;

				//minimal level to get bonus. 
				pawnHandlingLevel = pawnHandlingLevel > 5 ? pawnHandlingLevel - 5 : 0;

				//Common animals more likely when pawns have low handling, and rare animals more likely when pawns have high handling.  
				float commonalityAdjusted = commonality * ((15f - (float)commonality)) / 15f + (1 - commonality) * ((float)pawnHandlingLevel) / 15f;
				//Wildness decreases the likelyhood of the mount being picked. Handling level mitigates this. 
				float wildnessPenalty = 1 - (def.RaceProps.wildness * ((15f - (float)pawnHandlingLevel) / 15f));

				//Log.Message("name: " + def.defName + ", commonality: " + commonality + ", pawnHandlingLevel: " + pawnHandlingLevel + ", wildness: " + def.RaceProps.wildness + ", commonalityBonus: " + commonalityAdjusted + ", wildnessPenalty: " + wildnessPenalty + ", result: " + commonalityAdjusted * wildnessPenalty);
				return commonalityAdjusted * wildnessPenalty;
			}
			bool GetPackAnimals(List<Pawn> list, out List<Pawn> packAnimals)
			{
				packAnimals = new List<Pawn>();
				if (list.NullOrEmpty()) return false;

				var length = list.Count;
				for (int i = 0; i < length; i++)
				{
					Pawn pawn = list[i];
					if (pawn.IsEverMountable(out IsMountableUtility.Reason reason) && pawn.RaceProps.packAnimal && pawn.inventory != null && pawn.inventory.innerContainer.Count > 0)
					{
						packAnimals.Add(pawn);
					}
					else if (Settings.logging) Log.Message("[Giddy-Up] Skipping " + pawn.def.defName +"-"+ pawn.thingIDNumber.ToString() + ". Reason: " + reason.ToString());
				}
				return packAnimals.Count == 0 ? false : true;
			}
			#endregion
		}
		//Gets animal that'll get the pawn to the target the quickest. Returns null if no animal is found or if walking is faster. 
		public static bool GetBestAnimal(Pawn pawn, out Pawn bestanimal, IntVec3 firstTarget, IntVec3 secondTarget, float pawnTargetDistance, float firstToSecondTargetDistance)
		{
			//Prepare locals
			float pawnWalkSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed);
			float timeNormalWalking = (pawnTargetDistance + firstToSecondTargetDistance) / pawnWalkSpeed;
			ExtendedPawnData pawnData = pawn.GetGUData();
			bool firstTargetInForbiddenArea = false;
			bool secondTargetInForbiddenArea = false;
			Map map = pawn.Map;
			map.GetGUAreas(out Area areaNoMount, out Area areaDropAnimal);

			//This notes that the first destination is in a no-ride zone
			IntVec3[] areaDropCache = new IntVec3[0];
			if (areaNoMount != null && areaDropAnimal != null)
			{
				firstTargetInForbiddenArea = areaNoMount.innerGrid[map.cellIndices.CellToIndex(firstTarget)];
				secondTargetInForbiddenArea = secondTarget.y >= 0 && areaNoMount.innerGrid[map.cellIndices.CellToIndex(secondTarget)];
				areaDropCache = areaDropAnimal.ActiveCells.ToArray();
			}

			//Start looking for an animal
			Pawn closestAnimal = null;
			float timeBestRiding = float.MaxValue;
			float distanceBestRiding = float.MaxValue;
			var list = map.mapPawns.pawnsSpawned;
			var length = list.Count;
			for (int i = 0; i < length; i++)
			{
				Pawn animal = list[i];

				if (!animal.IsMountable(out IsMountableUtility.Reason reason, pawn, true, true)) 
				{
					if (Settings.logging) Log.Message("[Giddy-Up] Pawn " + pawn.Name.ToString() + " will not ride " + animal.thingIDNumber + " because: " + reason.ToString());
					continue;
				}
			
				if (!pawn.IsAllowed(animal)) continue; //Disallowed

				float distancePawnToAnimal = pawn.Position.DistanceTo(animal.Position);
				float distanceRiding = (animal.Position.DistanceTo(firstTarget) + firstToSecondTargetDistance) * 1.05f;
				float timeNeededForThisMount = (distancePawnToAnimal / pawnWalkSpeed) + (distanceRiding / StatPart_Riding.GetRidingSpeed(animal.GetStatValue(StatDefOf.MoveSpeed), animal, pawn.skills));
				
				if (timeNeededForThisMount < timeBestRiding)
				{
					closestAnimal = animal;
					timeBestRiding = timeNeededForThisMount;
					distanceBestRiding = distanceRiding; //Only used for logging
				}
			}
			
			if (Settings.logging)
			{
				if (closestAnimal == null) Log.Message("[Giddy-Up] " + (pawn.Name?.ToString() ?? "NULL") + " tried to find an animal but couldn't fnid any.");
				else Log.Message("[Giddy-Up] report for " + (pawn.Name?.ToString() ?? "NULL") + ":\n" +
				"Animal: " + (closestAnimal.Name?.ToString() ?? closestAnimal.thingIDNumber.ToString()) + "\n" +
				"First target: " + (firstTarget.ToString()) + "\n" + 
				"Second target: " + (secondTarget.ToString()) + "\n" + 
				"Normal walking speed: " + ((int)(pawnWalkSpeed)).ToString() + "\n" + 
				"Normal walking distance: " + ((int)(pawnTargetDistance + firstToSecondTargetDistance)).ToString() + "\n" + 
				"Normal walking time: " + ((int)timeNormalWalking).ToString() + "\n" + 
				"Distance to animal: " + ((int)pawn.Position.DistanceTo(closestAnimal.Position)).ToString() + "\n" + 
				"Ride distance: " + ((int)distanceBestRiding).ToString()  + "\n" + 
				"Ride speed: " + ((int)StatPart_Riding.GetRidingSpeed(closestAnimal.GetStatValue(StatDefOf.MoveSpeed), closestAnimal, pawn.skills)).ToString() + "\n" +
				"Ride time: " + ((int)((pawn.Position.DistanceTo(closestAnimal.Position) / pawnWalkSpeed) + 
					(distanceBestRiding / StatPart_Riding.GetRidingSpeed(closestAnimal.GetStatValue(StatDefOf.MoveSpeed), closestAnimal, pawn.skills)))).ToString()
				);
			}

			if (timeBestRiding < timeNormalWalking)
			{
				bestanimal = closestAnimal;
				return true;
			}
			bestanimal = null;
			return false;
		}
	}
}