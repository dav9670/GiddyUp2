using System.Linq;
using RimWorld;
using Verse;
using GiddyUp;

namespace GiddyUpRideAndRoll
{
    class Alert_NoDropAnimal : Alert
    {
        public Alert_NoDropAnimal()
        {
            this.defaultLabel = "GU_RR_NoDropAnimal_Label".Translate();
            this.defaultExplanation = "GU_RR_NoDropAnimal_Description".Translate();
        }
        public override AlertReport GetReport()
        {
            return this.ShouldAlert();
        }
        bool cacheResult;
        bool ShouldAlert()
        {
            if (Current.gameInt.tickManager.ticksGameInt % 20 == 0)
            {
                foreach (Map map in Find.Maps)
                {
                    map.GetGUAreas(out Area areaNoMount, out Area areaDropAnimal);
                    var unropablePlayerAnimals = map.mapPawns.SpawnedColonyAnimals.Any(animal => !AnimalPenUtility.NeedsToBeManagedByRope(animal));

                    if (unropablePlayerAnimals && areaNoMount != null && areaNoMount.innerGrid.TrueCount != 0 && (areaDropAnimal == null || areaDropAnimal.innerGrid.TrueCount == 0))
                    {
                        return cacheResult = true;
                    }
                }
                return cacheResult = false;
            }
            return cacheResult;
        }
    }
}