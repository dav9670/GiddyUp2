using RimWorld;
using Verse;

namespace GiddyUpRideAndRoll
{
    public static class Utitlities
    {
        public static bool HungryOrTired(this Pawn animal)
        {
            bool value = false;
            if (animal.needs != null)
            {
                //animal needs break?
                value = (animal.needs.food != null && animal.needs.food.CurCategory >= HungerCategory.UrgentlyHungry) || 
                    (animal.needs.rest != null && animal.needs.rest.CurCategory >= RestCategory.VeryTired);
            }

            return value;
        }
    }
}
