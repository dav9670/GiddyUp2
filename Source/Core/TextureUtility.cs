using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Settings = GiddyUp.ModSettings_GiddyUp;

namespace GiddyUp;

public static class TextureUtility
{
    public static string FormatKey(Def def, int age)
    {
        return def.defName + "/" + age.ToString();
    }

    public static float FetchCache(Pawn animal)
    {
        var age = animal.ageTracker?.CurLifeStageIndex ?? 0;
        Settings.offsetCache.TryGetValue(FormatKey(animal.def, age), out var offset);
        return offset;
    }

    public static float SetDrawOffset(PawnKindLifeStage age)
    {
        var unreadableTexture = age.bodyGraphicData.Graphic.MatEast.mainTexture as Texture2D;
        var t = GetReadableTexture(unreadableTexture);
        var backHeight = GetBackHeight(t);
        var backHeightRelative = (float)backHeight / (float)t.height;

        var textureHeight = age.bodyGraphicData.drawSize.y;
        //If animal texture does not fit in a tile, take this into account
        var extraOffset = textureHeight > 1f ? (textureHeight - 1f) / 2f : 0;
        //Small extra offset, you don't want to draw pawn exactly on back
        extraOffset += (float)textureHeight * backHeightRelative / 20f;
        return textureHeight * backHeightRelative - extraOffset;
    }

    private static int GetBackHeight(Texture2D t)
    {
        var middle = t.width / 2;
        var backHeight = 0;
        var inBody = false;
        var threshold = 0.8f;


        for (var i = 0; i < t.height; i++)
        {
            var c = t.GetPixel(middle, i);
            if (inBody && c.a < threshold)
            {
                backHeight = i;
                break;
            }

            if (c.a >= threshold)
                inBody = true;
        }

        return backHeight;
    }

    private static Texture2D GetReadableTexture(Texture2D texture)
    {
        // Create a temporary RenderTexture of the same size as the texture
        var tmp = RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        // Blit the pixels on texture to the RenderTexture
        Graphics.Blit(texture, tmp);
        // Backup the currently set RenderTexture
        var previous = RenderTexture.active;
        // Set the current RenderTexture to the temporary one we created
        RenderTexture.active = tmp;
        // Create a new readable Texture2D to copy the pixels to it
        var myTexture2D = new Texture2D(texture.width, texture.height);
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