using RimWorld;
using Verse;

namespace GiddyUpRideAndRoll
{
    public static class Utitlities
    {
        public static bool HungryOrTired(Pawn_NeedsTracker needs)
        {
            if (needs != null)
            {
                //animal needs break?
                return (needs.food != null && needs.food.CurCategory >= HungerCategory.UrgentlyHungry) || 
                    (needs.rest != null && needs.rest.CurCategory >= RestCategory.VeryTired);
            }

            return false;
        }
    }
}
