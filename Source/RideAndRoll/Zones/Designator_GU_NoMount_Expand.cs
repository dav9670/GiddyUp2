using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using GiddyUp.Zones;

namespace GiddyUpRideAndRoll.Zones
{
    class Designator_GU_NoMount_Expand : Designator_GU
    {

        public Designator_GU_NoMount_Expand() : base(DesignateMode.Add)
        {
            defaultLabel = "GU_RR_Designator_GU_NoMount_Expand_Label".Translate();
            defaultDesc = "GU_RR_Designator_GU_NoMount_Expand_Description".Translate();
            icon = GiddyUp.ResourceBank.iconNoMountExpand;
            areaLabel = GiddyUp.Setup.NOMOUNT_LABEL;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            selectedArea[c] = true;
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            return c.InBounds(base.Map) && selectedArea != null && !selectedArea[c];
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
