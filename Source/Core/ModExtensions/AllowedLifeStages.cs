using Verse;

namespace GiddyUp;

//For ThingDef (races)
internal class AllowedLifeStages : DefModExtension
{
    private string allowedLifeStagesCSV = "";

    public bool IsAllowedAge(int currentAgeIndex)
    {
        if (!allowedLifeStagesCSV.NullOrEmpty())
            foreach (var number in allowedLifeStagesCSV.Split(','))
                if (int.Parse(number) == currentAgeIndex)
                    return true;

        return false;
    }
}