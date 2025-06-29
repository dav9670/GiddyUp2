﻿using GiddyUp;
using RimWorld;
using Verse;

namespace GiddyUpRideAndRoll;

internal class Alert_NoDropAnimal : Alert
{
    public Alert_NoDropAnimal()
    {
        defaultLabel = "GU_RR_NoDropAnimal_Label".Translate();
        defaultExplanation = "GU_RR_NoDropAnimal_Description".Translate();
    }

    public override AlertReport GetReport()
    {
        return ShouldAlert();
    }

    private bool _shouldAlertCached;

    private bool ShouldAlert()
    {
        if (Current.gameInt.tickManager.ticksGameInt % 20 != 0)
            return _shouldAlertCached;
        
        foreach (var map in Find.Maps)
        {
            map.GetGUAreas(out var areaNoMount, out var areaDropAnimal);
            var unropablePlayerAnimals =
                map.mapPawns.SpawnedColonyAnimals.Any(animal => !AnimalPenUtility.NeedsToBeManagedByRope(animal));

            if (unropablePlayerAnimals && areaNoMount != null && areaNoMount.innerGrid.TrueCount != 0 &&
                (areaDropAnimal == null || areaDropAnimal.innerGrid.TrueCount == 0))
                return _shouldAlertCached = true;
        }

        return _shouldAlertCached = false;
    }
}