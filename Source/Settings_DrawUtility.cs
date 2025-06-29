using Verse;
using UnityEngine;
using RimWorld;
using static GiddyUp.ModSettings_GiddyUp;
using static GiddyUp.Setup;

namespace GiddyUp;

public static class OptionsDrawUtility
{
    public static int lineNumber, cellPosition;
    private const int LineHeight = 22; //Text.LineHeight + options.verticalSpacing;

    public static void DrawList(this Listing_Standard options)
    {
        lineNumber = cellPosition = 0; //Reset
        //List out all the unremoved defs from the compiled database
        for (var i = 0; i < AllAnimals.Count; i++)
        {
            var def = AllAnimals[i];
            if (def == null)
                continue;
            if (selectedTab == SelectedTab.BodySize && def.race.baseBodySize < bodySizeFilter)
                continue;

            DrawListItem(options, def);
            cellPosition += LineHeight;
            ++lineNumber;
        }
    }

    public static void DrawListItem(Listing_Standard options, ThingDef def)
    {
        //Determine checkbox status...
        bool checkOn;
        var hash = def.shortHash;
        if (selectedTab == SelectedTab.BodySize)
            checkOn = MountableCache.Contains(hash);
        else
            checkOn = DrawRulesCache.Contains(hash);

        //Fetch bounding rect
        var rect = options.GetRect(LineHeight);
        rect.y = cellPosition;

        //Label
        var dataString = def.label + " :: " + def.modContentPack?.Name + " :: " + def.defName;

        //Actually draw the line item
        if (options.BoundingRectCached == null || rect.Overlaps(options.BoundingRectCached.Value))
            CheckboxLabeled(rect, dataString, def.label, ref checkOn, def);

        //Handle row coloring and spacing
        if (lineNumber % 2 != 0)
            Widgets.DrawLightHighlight(rect);
        Widgets.DrawHighlightIfMouseover(rect);

        if (selectedTab == SelectedTab.BodySize)
        {
            if (checkOn && !MountableCache.Contains(hash))
                MountableCache.Add(hash);
            else if (!checkOn && MountableCache.Contains(hash))
                MountableCache.Remove(hash);
        }
        else
        {
            if (checkOn && !DrawRulesCache.Contains(hash))
                DrawRulesCache.Add(hash);
            else if (!checkOn && DrawRulesCache.Contains(hash))
                DrawRulesCache.Remove(hash);
        }
    }

    private static void CheckboxLabeled(Rect rect, string data, string label, ref bool checkOn, ThingDef def)
    {
        var leftHalf = rect.LeftHalf();

        //Is there an icon?
        var iconRect = new Rect(leftHalf.x, leftHalf.y, 32f, leftHalf.height);
        Texture2D? icon = null;
        if (def is BuildableDef)
            icon = def.uiIcon;
        if (icon != null)
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true, 1f, Color.white, 0f, 0f);

        //If there is a label, split the cell in half, otherwise use the full cell for data
        if (!label.NullOrEmpty())
        {
            var dataRect = new Rect(iconRect.xMax, iconRect.y, leftHalf.width - 32f, leftHalf.height);

            Widgets.Label(dataRect, data?.Truncate(dataRect.width - 12f, InspectPaneUtility.truncatedLabelsCached));
            var rightHalf = rect.RightHalf();
            Widgets.Label(rightHalf, label.Truncate(rightHalf.width - 12f, InspectPaneUtility.truncatedLabelsCached));
        }
        else
        {
            var dataRect = new Rect(iconRect.xMax, iconRect.y, rect.width - 32f, leftHalf.height);
            Widgets.Label(dataRect, data?.Truncate(dataRect.width - 12f, InspectPaneUtility.truncatedLabelsCached));
        }

        Widgets.Checkbox(new Vector2(rect.xMax - 24f, rect.y), ref checkOn, 24f, paintable: true);
    }
}