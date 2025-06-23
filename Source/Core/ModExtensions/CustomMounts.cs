using System.Collections.Generic;
using Verse;

namespace GiddyUp;

//For PawnKindDef
internal class CustomMounts : DefModExtension
{
    public int mountChance = 0;
    public Dictionary<PawnKindDef, int> possibleMounts = new();
}