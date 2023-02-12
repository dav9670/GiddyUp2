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
        int ticker = 1;
        bool ShouldAlert()
        {
            if (--ticker == 0)
            {
                ticker = 20;
                foreach (Map map in Find.Maps)
                {
                    map.GetGUAreas(out Area areaNoMount, out Area areaDropAnimal);
                    var unropablePlayerAnimals = map.mapPawns.SpawnedColonyAnimals.Any(animal => !AnimalPenUtility.NeedsToBeManagedByRope(animal));

                    if (unropablePlayerAnimals && areaNoMount != null && areaNoMount.innerGrid.TrueCount != 0 && (areaDropAnimal == null || areaDropAnimal.innerGrid.TrueCount == 0))
                    {
                        cacheResult = true;
                        return true;
                    }
                }
                cacheResult = false;
                return false;
            }
            return cacheResult;
        }
    }
}