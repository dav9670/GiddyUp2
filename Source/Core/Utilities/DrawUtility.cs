using System;
using UnityEngine;
using Verse;

namespace GiddyUp.Utilities
{
    public class DrawUtility
    {
        const float ContentPadding = 5f, TextMargin = 20f, BottomMargin = 2f, rowHeight = 20f;

        static readonly Color iconBaseColor = new Color(0.5f, 0.5f, 0.5f, 1f),
            iconMouseOverColor = new Color(0.6f, 0.6f, 0.4f, 1f),
            SelectedOptionColor = new Color(0.5f, 1f, 0.5f, 1f),
            constGrey = new Color(0.8f, 0.8f, 0.8f, 1f),
            background = new Color(0.5f, 0, 0, 0.1f),
            exceptionBackground = new Color(0f, 0.5f, 0, 0.1f);

        static void drawBackground(Rect rect, Color background)
        {
            Color save = GUI.color;
            GUI.color = background;
            GUI.DrawTexture(rect, TexUI.FastFillTex);
            GUI.color = save;
        }
        static void DrawLabel(string labelText, Rect textRect, float offset)
        {
            var labelHeight = Text.CalcHeight(labelText, textRect.width);
            labelHeight -= 2f;
            var labelRect = new Rect(textRect.x, textRect.yMin - labelHeight + offset, textRect.width, labelHeight);
            GUI.DrawTexture(labelRect, TexUI.GrayTextBG);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(labelRect, labelText);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }
        static Color getColor(ThingDef Animal)
        {
            if (Animal.graphicData != null)
            {
                return Animal.graphicData.color;
            }
            return Color.white;
        }
        public static bool CustomDrawer_Tabs(Rect rect, string selected, String[] defaultValues)
        {
            int labelWidth = 140;
            int offset = 0;
            bool change = false;

            foreach (String tab in defaultValues)
            {
                Rect buttonRect = new Rect(rect);
                buttonRect.width = labelWidth;
                buttonRect.position = new Vector2(buttonRect.position.x + offset, buttonRect.position.y);
                Color activeColor = GUI.color;
                bool isSelected = tab == selected;
                if (isSelected)
                    GUI.color = SelectedOptionColor;
                bool clicked = Widgets.ButtonText(buttonRect, tab);
                if (isSelected)
                    GUI.color = activeColor;

                if (clicked)
                {
                    if(selected != tab)
                    {
                        selected = tab;
                    }
                    else
                    {
                        selected = "none";
                    }
                    change = true;
                }

                offset += labelWidth;

            }
            return change;
        }
        public static bool CustomDrawer_Filter(Rect rect, float slider, bool def_isPercentage, float def_min, float def_max, Color background)
        {
            drawBackground(rect, background);
            int labelWidth = 50;

            Rect sliderPortion = new Rect(rect);
            sliderPortion.width = sliderPortion.width - labelWidth;

            Rect labelPortion = new Rect(rect);
            labelPortion.width = labelWidth;
            labelPortion.position = new Vector2(sliderPortion.position.x + sliderPortion.width + 5f, sliderPortion.position.y + 4f);

            sliderPortion = sliderPortion.ContractedBy(2f);

            if (def_isPercentage)
                Widgets.Label(labelPortion, (Mathf.Round(slider * 100f)).ToString("F0") + "%");
            else
                Widgets.Label(labelPortion, slider.ToString("F2"));

            float val = Widgets.HorizontalSlider_NewTemp(sliderPortion, slider, def_min, def_max, true);
            bool change = false;

            if (slider != val)
                change = true;

            slider = val;
            return change;
        }
    }
}