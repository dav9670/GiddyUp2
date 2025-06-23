using RimWorld;
using Verse;
using UnityEngine;
using System;

namespace GiddyUp;

[StaticConstructorOnStartup]
public static class ResourceBank
{
    public static Type RaidStrategyWorker_Siege = typeof(RaidStrategyWorker_Siege);

    public static Texture2D iconDropAnimalClear =
            ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_DropAnimal_Clear", true),
        iconDropAnimalExpand = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_DropAnimal_Expand", true),
        iconNoMountClear = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_NoMount_Clear", true),
        iconNoMountExpand = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_NoMount_Expand", true);

    public const string VisitorAreaDropMount = "GU_Car_Area_GU_DropAnimal_NPC",
        AreaDropMount = "Gu_Area_DropMount",
        AreaNoMount = "Gu_Area_NoMount";

    public const float defaultSizeThreshold = 1.2f,
        combatPowerFactor = 2.0f,
        autoHitchDistance = 8.0f,
        guestSpotCheckRange = 120f;

    public const int mapEdgeIgnore = 8;

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