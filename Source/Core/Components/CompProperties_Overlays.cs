using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GiddyUp;

public class CompProperties_Overlay : CompProperties
{
    public GraphicOverlay overlayFront,
        overlaySide,
        overlayBack;

    public class GraphicOverlay
    {
        public GraphicData graphicDataDefault,
            graphicDataFemale,
            graphicDataMale;

        public Vector3 offsetDefault = Vector3.zero,
            offsetFemale = Vector3.zero,
            offsetMale = Vector3.zero;

        public List<GraphicData> allVariants;
        public string stringDelimiter = "_";
    }

    public GraphicOverlay GetOverlay(Rot4 dir)
    {
        if (dir == Rot4.South) return overlayFront;
        if (dir == Rot4.North) return overlayBack;
        return overlaySide;
    }

    public CompProperties_Overlay()
    {
        compClass = typeof(CompOverlay);
    }
}