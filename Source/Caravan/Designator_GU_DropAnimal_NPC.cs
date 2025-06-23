using RimWorld;
using UnityEngine;
using Verse;
using GiddyUp;

namespace GiddyUpCaravan;

internal class Designator_GU_DropAnimal_NPC_Clear : Designator_GU
{
    public Designator_GU_DropAnimal_NPC_Clear() : base(DesignateMode.Remove)
    {
        defaultLabel = "GU_Car_Designator_GU_DropAnimal_NPC_Clear_Label".Translate();
        defaultDesc = "GU_Car_Designator_GU_DropAnimal_NPC_Clear_Description".Translate();

        icon = ContentFinder<Texture2D>.Get("UI/GU_Car_Designator_GU_DropAnimal_NPC_Clear", true);
        areaLabel = ResourceBank.VisitorAreaDropMount;
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        selectedArea[c] = false;
    }

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;
    public override bool DragDrawMeasurements => true;

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        return c.InBounds(Map) && selectedArea != null && selectedArea[c];
    }
}

internal class Designator_GU_DropAnimal_NPC_Expand : Designator_GU
{
    public Designator_GU_DropAnimal_NPC_Expand() : base(DesignateMode.Add)
    {
        defaultLabel = "GU_Car_Designator_GU_DropAnimal_NPC_Expand_Label".Translate();
        defaultDesc = "GU_Car_Designator_GU_DropAnimal_NPC_Expand_Description".Translate();
        icon = ContentFinder<Texture2D>.Get("UI/GU_Car_Designator_GU_DropAnimal_NPC_Expand", true);
        areaLabel = ResourceBank.VisitorAreaDropMount;
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        selectedArea[c] = true;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        return c.InBounds(Map) && selectedArea != null && !selectedArea[c];
    }
}