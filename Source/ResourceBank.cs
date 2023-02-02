using RimWorld;
using Verse;
using UnityEngine;

namespace GiddyUp
{
    [StaticConstructorOnStartup]
    public static class ResourceBank
    {
        public static Texture2D iconDropAnimalClear = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_DropAnimal_Clear", true),
            iconDropAnimalExpand = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_DropAnimal_Expand", true),
            iconNoMountClear = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_NoMount_Clear", true),
            iconNoMountExpand = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_NoMount_Expand", true);

        public const string DROPANIMAL_LABEL = "Gu_Area_DropMount", 
            NOMOUNT_LABEL = "Gu_Area_NoMount",
            DropAnimal_NPC_LABEL = "GU_Car_Area_GU_DropAnimal_NPC";

        [DefOf]
        public class ConceptDefOf
        {
            public static ConceptDef GUC_Animal_Handling;
            public static ConceptDef BM_Mounting;
            public static ConceptDef BM_Enemy_Mounting;
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
