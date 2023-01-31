using System.Linq;
using RimWorld;
using Verse;

namespace GiddyUpRideAndRoll.Alerts
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
                    GiddyUp.Zones.Area_GU.GetGUAreasFast(map, out Area areaNoMount, out Area areaDropAnimal);
                    var unropablePlayerAnimals = map.mapPawns.SpawnedColonyAnimals.Any(animal => animal.factionInt.def.isPlayer && !AnimalPenUtility.NeedsToBeManagedByRope(animal));

                    if (unropablePlayerAnimals && areaNoMount != null && areaDropAnimal != null && areaNoMount.innerGrid.TrueCount != 0 && areaDropAnimal.innerGrid.TrueCount != 0)
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
