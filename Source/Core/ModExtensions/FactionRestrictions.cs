using System.Collections.Generic;
using Verse;

namespace GiddyUp;

internal class FactionRestrictions : DefModExtension
{
    //Can be used in xml patches to restrict animals per faction. 
    public List<PawnKindDef> allowedNonWildAnimals = new();
    public List<PawnKindDef> allowedWildAnimals = new();
    public int mountChance = -1;
    public int wildAnimalWeight = -1;
    public int nonWildAnimalWeight = -1;
}