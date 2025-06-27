using RimWorld;
using Verse;
using GiddyUp;

namespace GiddyUpRideAndRoll;

internal class Designator_GU_NoMount_Clear : Designator_GU
{
    public Designator_GU_NoMount_Clear() : base(ResourceBank.AreaNoMount)
    {
        defaultLabel = "GU_RR_Designator_GU_NoMount_Clear_Label".Translate();
        defaultDesc = "GU_RR_Designator_GU_NoMount_Clear_Description".Translate();
        icon = ResourceBank.iconNoMountClear;
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        SelectedArea[c] = false;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        return c.InBounds(Map) && SelectedArea != null && SelectedArea[c];
    }

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;
    public override bool DragDrawMeasurements => true;
}

internal class Designator_GU_NoMount_Expand : Designator_GU
{
    public Designator_GU_NoMount_Expand() : base(ResourceBank.AreaNoMount)
    {
        defaultLabel = "GU_RR_Designator_GU_NoMount_Expand_Label".Translate();
        defaultDesc = "GU_RR_Designator_GU_NoMount_Expand_Description".Translate();
        icon = ResourceBank.iconNoMountExpand;
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        SelectedArea[c] = true;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        return c.InBounds(Map) && SelectedArea != null && !SelectedArea[c];
    }

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

    public override bool DragDrawMeasurements => true;
}