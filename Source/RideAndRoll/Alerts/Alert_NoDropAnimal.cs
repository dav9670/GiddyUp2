using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using GiddyUp.Zones;

namespace GiddyUpRideAndRoll.Alerts
{
    class Alert_NoDropAnimal : RimWorld.Alert
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
        private bool ShouldAlert()
        {
            foreach (Map map in Find.Maps)
            {
                Area_GU areaNoMount = (Area_GU)map.areaManager.GetLabeled(GiddyUp.Setup.NOMOUNT_LABEL);
                Area_GU areaDropAnimal = (Area_GU)map.areaManager.GetLabeled(GiddyUp.Setup.DROPANIMAL_LABEL);
                var unropablePlayerAnimals = map.mapPawns.SpawnedColonyAnimals.Any(animal => animal.Faction == Faction.OfPlayer && !AnimalPenUtility.NeedsToBeManagedByRope(animal));

                if (unropablePlayerAnimals && areaNoMount != null && areaDropAnimal != null && areaNoMount.ActiveCells.Count() > 0 && areaDropAnimal.ActiveCells.Count() == 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
