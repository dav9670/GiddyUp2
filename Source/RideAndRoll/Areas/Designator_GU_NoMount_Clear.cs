using RimWorld;
using Verse;
using GiddyUp.Zones;
namespace GiddyUpRideAndRoll.Zones
{
    class Designator_GU_NoMount_Clear : Designator_GU
    {
        public Designator_GU_NoMount_Clear() : base(DesignateMode.Remove)
        {
            defaultLabel = "GU_RR_Designator_GU_NoMount_Clear_Label".Translate();
            defaultDesc = "GU_RR_Designator_GU_NoMount_Clear_Description".Translate();
            icon = GiddyUp.ResourceBank.iconNoMountClear;
            areaLabel = GiddyUp.ResourceBank.NOMOUNT_LABEL;
        }
        public override void DesignateSingleCell(IntVec3 c)
        {
            selectedArea[c] = false;
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            return c.InBounds(base.Map) && selectedArea != null && selectedArea[c];
        }
        public override int DraggableDimensions
        {
            get
            {
                return 2;
            }
        }
        public override bool DragDrawMeasurements
        {
            get
            {
                return true;
            }
        }
    }
}
