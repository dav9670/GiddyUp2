using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GiddyUp;

internal class CompOverlay : ThingComp
{
    private CompProperties_Overlay overlayComp;
    private bool valid;
    private bool incompatible;
    private Pawn pawn;
    private Dictionary<Rot4, (GraphicData, Vector3, Vector3, Vector3)> graphicCache = new();

    private void CacheGraphicData(Rot4 rotation)
    {
        var overlay = overlayComp.GetOverlay(rotation);
        if (overlay == null)
            return;

        var graphicData = (pawn.gender == Gender.Female ? overlay.graphicDataFemale : overlay.graphicDataMale) ??
                          overlay.graphicDataDefault;
        if (graphicData == null)
            return;

        //support multi texture animals
        if (overlay.allVariants != null)
        {
            var graphicPath = pawn.Drawer?.renderer?.SilhouetteGraphic?.path;
            if (graphicPath.NullOrEmpty())
                return;
            var graphicName = graphicPath.Split('/').Last();
            var found = false;
            foreach (var variant in overlay.allVariants)
            {
                var variantName = variant.texPath.Split('/').Last().Split(overlay.stringDelimiter.ToCharArray())[0];

                if (graphicName == variantName)
                {
                    //set required properties
                    var texPath = variant.texPath;
                    variant.CopyFrom(graphicData);
                    variant.texPath = texPath;
                    graphicData = variant;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                incompatible = true;
                return;
            }
        }

        graphicCache.Add(rotation, (graphicData, overlay.offsetFemale, overlay.offsetMale, overlay.offsetDefault));
        valid = true;
    }

    public void TryCache()
    {
        overlayComp = props as CompProperties_Overlay;
        if (parent is Pawn pawn)
        {
            this.pawn = pawn;
            CacheGraphicData(Rot4.South);
            CacheGraphicData(Rot4.North);
            CacheGraphicData(Rot4.West);
        }
    }

    public override void PostDraw()
    {
        if (valid && pawn.IsMountedAnimal() &&
            graphicCache.TryGetValue(pawn.Rotation == Rot4.East ? Rot4.West : pawn.Rotation, out var cache))
        {
            var drawPos = parent.DrawPos;
            var offset = pawn.gender == Gender.Female ? cache.Item2 : cache.Item3;
            if (offset == Vector3.zero)
                offset = cache.Item4;
            if (pawn.Rotation == Rot4.West)
                offset.x = -offset.x;
            offset.y += pawn.Rotation == Rot4.South
                ? 0.08f
                : 0.04375f; //Tries to render above equipment but below the held weapon

            //Somehow the rotation is flipped, hence the use of GetOpposite.
            cache.Item1.Graphic.Draw(drawPos + offset, parent.Rotation, parent, 0f);
        }
        else if (!valid && !incompatible)
        {
            TryCache();
        }
    }
}