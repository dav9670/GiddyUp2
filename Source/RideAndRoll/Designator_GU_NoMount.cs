using RimWorld;
using Verse;
using GiddyUp;

namespace GiddyUpRideAndRoll
{
    class Designator_GU_NoMount_Clear : Designator_GU
    {
        public Designator_GU_NoMount_Clear() : base(DesignateMode.Remove)
        {
            defaultLabel = "GU_RR_Designator_GU_NoMount_Clear_Label".Translate();
            defaultDesc = "GU_RR_Designator_GU_NoMount_Clear_Description".Translate();
            icon = GiddyUp.ResourceBank.iconNoMountClear;
            areaLabel = GiddyUp.ResourceBank.AreaNoMount;
        }
        public override void DesignateSingleCell(IntVec3 c)
        {
            selectedArea[c] = false;
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            return c.InBounds(base.Map) && selectedArea != null && selectedArea[c];
        }
        public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;
        public override bool DragDrawMeasurements
        {
            get
            {
                return true;
            }
        }
    }
    class Designator_GU_NoMount_Expand : Designator_GU
    {
        public Designator_GU_NoMount_Expand() : base(DesignateMode.Add)
        {
            defaultLabel = "GU_RR_Designator_GU_NoMount_Expand_Label".Translate();
            defaultDesc = "GU_RR_Designator_GU_NoMount_Expand_Description".Translate();
            icon = GiddyUp.ResourceBank.iconNoMountExpand;
            areaLabel = GiddyUp.ResourceBank.AreaNoMount;
        }
        public override void DesignateSingleCell(IntVec3 c)
        {
            selectedArea[c] = true;
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            return c.InBounds(base.Map) && selectedArea != null && !selectedArea[c];
        }

        public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;

        public override bool DragDrawMeasurements
        {
            get
            {
                return true;
            }
        }
    }
}
