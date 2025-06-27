using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GiddyUp;

//Is used in other Giddy-Up mods as a base for simple areas that can be requested in the areaManager using their label
public class Designator_GU : Designator
{
    private readonly string _areaLabel;
    protected Area? SelectedArea { get; set; }

    public Designator_GU(string areaLabel)
    {
        _areaLabel = areaLabel;
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        useMouseIcon = true;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 loc) => loc.InBounds(Map);

    public override void ProcessInput(Event ev)
    {
        if (!CheckCanInteract()) return;
        SetSelectedArea();
        if (SelectedArea != null) base.ProcessInput(ev);
    }
    
    public override void SelectedUpdate()
    {
        GenUI.RenderMouseoverBracket();
        SelectedArea?.MarkForDraw();
    }
    
    protected void SetSelectedArea()
    {
        SelectedArea = Map.areaManager.GetLabeled(_areaLabel);
        if (SelectedArea != null) 
            return;
        
        //If no area was created yet, create one and add it to areaManager.
        SelectedArea = new Area_GU(Map.areaManager, _areaLabel);
        Map.areaManager.areas.Add(SelectedArea);
    }
}