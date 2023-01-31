using System.Linq;
using Verse;

namespace GiddyUp
{
    class AllowedLifeStagesPatch : DefModExtension
    {
        //Can be used in xml patches to allow other life stages than the final one. 
        string allowedLifeStagesCSV = "";

        public int[] GetAllowedLifeStagesAsList()
        {
            if (!allowedLifeStagesCSV.NullOrEmpty())
            {
                return allowedLifeStagesCSV.Split(',').Select(int.Parse).ToArray();
            }
            return new int[0];
        }
    }
}
