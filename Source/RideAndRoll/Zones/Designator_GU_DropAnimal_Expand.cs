using GiddyUp.Zones;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace GiddyUpRideAndRoll.Zones
{
    class Designator_GU_DropAnimal_Expand : Designator_GU
    {
        public Designator_GU_DropAnimal_Expand() : base(DesignateMode.Add)
        {
            defaultLabel = "GU_RR_Designator_GU_DropAnimal_Expand_Label".Translate();
            defaultDesc = "GU_RR_Designator_GU_DropAnimal_Expand_Description".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/GU_RR_Designator_GU_DropAnimal_Expand", true);
            areaLabel = GiddyUp.Setup.DROPANIMAL_LABEL;
        }
        public override void DesignateSingleCell(IntVec3 c)
        {
            selectedArea[c] = true;
        }
        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            return c.InBounds(base.Map) && selectedArea != null && !selectedArea[c] && !c.Impassable(base.Map);
        }
    }
}
