using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp
{
    public class TextureUtility
    {
        public static string FormatKey(Def def, int age)
        {
            return def.defName +"/"+age.ToString();
        }
        public static float FetchCache(Pawn animal)
        {
            var age = animal.ageTracker?.CurLifeStageIndex ?? 0;
            Settings.offsetCache.TryGetValue(FormatKey(animal.def, age), out float offset);
            return offset;
        }
        public static float SetDrawOffset(PawnKindLifeStage age)
        {
            Texture2D unreadableTexture = age.bodyGraphicData.Graphic.MatEast.mainTexture as Texture2D;
            Texture2D t = TextureUtility.GetReadableTexture(unreadableTexture);
            int backHeight = TextureUtility.GetBackHeight(t);
            float backHeightRelative = (float)backHeight / (float)t.height;

            float textureHeight = age.bodyGraphicData.drawSize.y;
            //If animal texture does not fit in a tile, take this into account
            float extraOffset = textureHeight > 1f ? (textureHeight - 1f) / 2f : 0;
            //Small extra offset, you don't want to draw pawn exactly on back
            extraOffset += (float)textureHeight * backHeightRelative / 20f;
            return (textureHeight * backHeightRelative - extraOffset);
        }
        public static Vector3 ExtractVector3(String extractFrom)
        {
            if (extractFrom.NullOrEmpty())
            {
                return new Vector3();
            }
            Vector3 result = new Vector3();

            List<float> values = extractFrom.Split(',').ToList().Select(x => float.Parse(x)).ToList();
            if (values.Count >= 1)
            {
                result.x = values[0];
            }
            if (values.Count >= 2)
            {
                result.y = values[1];
            }
            if (values.Count >= 3)
            {
                result.z = values[2];
            }
            return result;
        }
        static int GetBackHeight(Texture2D t)
        {

            int middle = t.width / 2;
            int backHeight = 0;
            bool inBody = false;
            float threshold = 0.8f;


            for (int i = 0; i < t.height; i++)
            {
                Color c = t.GetPixel(middle, i);
                if (inBody && c.a < threshold)
                {
                    backHeight = i;
                    break;
                }
                if (c.a >= threshold)
                {
                    inBody = true;
                }
            }
            return backHeight;
        }
        static Texture2D GetReadableTexture(Texture2D texture)
        {
            // Create a temporary RenderTexture of the same size as the texture
            RenderTexture tmp = RenderTexture.GetTemporary(
                                texture.width,
                                texture.height,
                                0,
                                RenderTextureFormat.Default,
                                RenderTextureReadWrite.Linear);

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(texture, tmp);
            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;
            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;
            // Create a new readable Texture2D to copy the pixels to it
            Texture2D myTexture2D = new Texture2D(texture.width, texture.height);
            // Copy the pixels from the RenderTexture to the new Texture
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();
            // Reset the active RenderTexture
            RenderTexture.active = previous;
            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);
            return myTexture2D;
        }
    }
}