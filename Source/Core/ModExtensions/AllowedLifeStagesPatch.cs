using System.Linq;
using Verse;

namespace GiddyUp
{
    class AllowedLifeStagesPatch : DefModExtension
    {
        //Can be used in xml patches to allow other life stages than the final one. 
        string allowedLifeStagesCSV = "";

        public bool IsAllowedAge(int currentAgeIndex)
        {
            if (!allowedLifeStagesCSV.NullOrEmpty())
            {
                foreach (var number in allowedLifeStagesCSV.Split(','))
                {
                    if (int.Parse(number) == currentAgeIndex) return true;
                }
            }
            return false;
        }
    }
}
