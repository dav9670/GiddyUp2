using Verse;
using UnityEngine;
using Verse.Sound;
using RimWorld;
using static GiddyUp.ModSettings_GiddyUp;
using static GiddyUp.Setup;
 
namespace GiddyUp
{
    public static class OptionsDrawUtility
	{
		public static int lineNumber, cellPosition;
		public const int lineHeight = 22; //Text.LineHeight + options.verticalSpacing;
		public static void DrawList(this Listing_Standard options)
		{
			lineNumber = cellPosition = 0; //Reset
			//List out all the unremoved defs from the compiled database
			for (int i = 0; i < allAnimals.Length; i++)
			{
				var def = allAnimals[i];
				if (def == null) continue;
				if (selectedTab == SelectedTab.bodySize && def.race.baseBodySize < bodySizeFilter) continue;
				
				DrawListItem(options, def);
				cellPosition += lineHeight;
				++lineNumber;
			}
		}

		public static void DrawListItem(Listing_Standard options, ThingDef def)
		{
			//Determine checkbox status...
			bool checkOn;
			ushort hash = def.shortHash;
			if (selectedTab == SelectedTab.bodySize) checkOn = mountableCache.Contains(hash);
			else checkOn = drawRulesCache.Contains(hash);
			
			//Fetch bounding rect
			Rect rect = options.GetRect(lineHeight);
			rect.y = cellPosition;

			//Label
			string dataString = def.label + " :: " + def.modContentPack?.Name + " :: " + def.defName;

			//Actually draw the line item
			if (options.BoundingRectCached == null || rect.Overlaps(options.BoundingRectCached.Value))
			{
				CheckboxLabeled(rect, dataString, def.label, ref checkOn, def);
			}

			//Handle row coloring and spacing
			if (lineNumber % 2 != 0) Widgets.DrawLightHighlight(rect);
			Widgets.DrawHighlightIfMouseover(rect);
			
			if (selectedTab == SelectedTab.bodySize)
			{
				if (checkOn && !mountableCache.Contains(hash)) mountableCache.Add(hash);
				else if (!checkOn && mountableCache.Contains(hash)) mountableCache.Remove(hash);
			}
			else
			{
				if (checkOn && !drawRulesCache.Contains(hash)) drawRulesCache.Add(hash);
				else if (!checkOn && drawRulesCache.Contains(hash)) drawRulesCache.Remove(hash);
			}
		}

		static void CheckboxLabeled(Rect rect, string data, string label, ref bool checkOn, ThingDef def)
		{
			Rect leftHalf = rect.LeftHalf();
			
			//Is there an icon?
			Rect iconRect = new Rect(leftHalf.x, leftHalf.y, 32f, leftHalf.height);
			Texture2D icon = null;
			if (def is BuildableDef) icon = ((BuildableDef)def).uiIcon;
			if (icon != null) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true, 1f, Color.white, 0f, 0f);

			//If there is a label, split the cell in half, otherwise use the full cell for data
			if (!label.NullOrEmpty())
			{
				Rect dataRect = new Rect(iconRect.xMax, iconRect.y, leftHalf.width - 32f, leftHalf.height);

				Widgets.Label(dataRect, data?.Truncate(dataRect.width - 12f, InspectPaneUtility.truncatedLabelsCached));
				Rect rightHalf = rect.RightHalf();
				Widgets.Label(rightHalf, label.Truncate(rightHalf.width - 12f, InspectPaneUtility.truncatedLabelsCached));
			}
			else
			{
				Rect dataRect = new Rect(iconRect.xMax, iconRect.y, rect.width - 32f, leftHalf.height);
				Widgets.Label(dataRect, data?.Truncate(dataRect.width - 12f, InspectPaneUtility.truncatedLabelsCached));
			}

			Widgets.Checkbox(new Vector2(rect.xMax - 24f, rect.y), ref checkOn, 24f, paintable: true);
		}
	}
}
