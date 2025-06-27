﻿using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GiddyUp;

//Is used in other Giddy-Up mods as a base for simple areas that can be requested in the areamanager using their label
public class Designator_GU : Designator
{
    protected Area selectedArea;
    protected string areaLabel;

    public Designator_GU(DesignateMode mode)
    {
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        useMouseIcon = true;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 loc)
    {
        if (!loc.InBounds(Map))
            return false;
        return true;
    }

    public override void ProcessInput(Event ev)
    {
        if (!CheckCanInteract())
            return;
        SetSelectedArea(areaLabel);
        if (selectedArea != null)
            base.ProcessInput(ev);
    }

    protected void SetSelectedArea(string areaLabel)
    {
        selectedArea = Map.areaManager.GetLabeled(areaLabel);
        if (selectedArea == null)
        {
            //If no area was created yet, create one and add it to areaManager.
            selectedArea = new Area_GU(Map.areaManager, areaLabel);
            List<Area> areaManager_areas = Map.areaManager.areas;
            areaManager_areas.Add(selectedArea);
        }
    }

    public override void SelectedUpdate()
    {
        GenUI.RenderMouseoverBracket();
        if (selectedArea != null)
            selectedArea.MarkForDraw();
    }

    public override void FinalizeDesignationSucceeded()
    {
        base.FinalizeDesignationSucceeded();
    }
}