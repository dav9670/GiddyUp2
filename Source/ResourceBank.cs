using RimWorld;
using Verse;
using UnityEngine;

namespace GiddyUp
{
    [StaticConstructorOnStartup]
    public static class ResourceBank
    {
        public static Texture2D iconDropAnimalClear = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_DropAnimal_Clear", true);
        public static Texture2D iconDropAnimalExpand = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_DropAnimal_Expand", true);
        public static Texture2D iconNoMountClear = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_NoMount_Clear", true);
        public static Texture2D iconNoMountExpand = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_NoMount_Expand", true);

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
            public static JobDef WaitForRider;
        }
        [DefOf]
        public static class RoomRoleDefOf
        {
            public static RoomRoleDef Barn;
        }
    }
}
