using RimWorld;
using Verse;

namespace GiddyUp
{
    public static class ResourceBank
    {
        [DefOf]
        public class ConceptDefOf
        {
            public static ConceptDef GUC_Animal_Handling;
        }
        [DefOf]
        public static class JobDefOf
        {
            public static JobDef Mount;
            public static JobDef Mounted;
            public static JobDef Dismount;
            public static JobDef RideToJob;
            public static JobDef WaitForRider;
        }
        [DefOf]
        public static class RoomRoleDefOf
        {
            public static RoomRoleDef Barn;
        }
    }
}
