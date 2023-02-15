using Verse;

namespace GiddyUp
{
    class FactionRestrictions : DefModExtension
    {
        //Can be used in xml patches to restrict animals per faction. 
        public PawnKindDef[] allowedNonWildAnimals = new PawnKindDef[0];
        public PawnKindDef[] allowedWildAnimals = new PawnKindDef[0];
        public int mountChance = -1;
        public int wildAnimalWeight = -1;
        public int nonWildAnimalWeight = -1;
    }
}