using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace PitOfGoblinCheats;

internal static class MapTexture
{
    const int MaxSize = 1024;

    static readonly Color32 Background = new(16, 16, 21, 255);
    static readonly Color32 Floor = new(226, 208, 170, 255);

    public static Texture2D Build(Texture2D source, out bool blank)
    {
        blank = false;
        int sw = source.width, sh = source.height;
        if (!source.isReadable)
        {
            Plugin.L.LogInfo($"Map: {sw}x{sh} {source.format}, texture is not readable, drawing the original");
            return null;
        }

        var pixels = source.GetPixels32();
        int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(sw, sh) / (float)MaxSize));
        int w = sw / step, h = sh / step;
        int count = w * h;

        var luminance = new byte[count];
        var alpha = new byte[count];
        var lumHistogram = new int[256];
        var alphaHistogram = new int[256];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var c = pixels[y * step * sw + x * step];
            int i = y * w + x;
            byte l = (byte)((c.r * 77 + c.g * 150 + c.b * 29) >> 8);
            luminance[i] = l;
            alpha[i] = c.a;
            lumHistogram[l]++;
            alphaHistogram[c.a]++;
        }

        int lumLow = Percentile(lumHistogram, count, 0.02f), lumHigh = Percentile(lumHistogram, count, 0.98f);
        int alphaLow = Percentile(alphaHistogram, count, 0.02f), alphaHigh = Percentile(alphaHistogram, count, 0.98f);

        if (lumHigh - lumLow < 4 && alphaHigh - alphaLow < 4)
        {
            Plugin.L.LogInfo($"Map: {sw}x{sh} image is empty (luminance {lumLow}, alpha {alphaLow})");
            blank = true;
            return null;
        }

        bool useAlpha = alphaHigh - alphaLow > 50 && lumHigh - lumLow < 25;
        var values = useAlpha ? alpha : luminance;
        float low = useAlpha ? alphaLow : lumLow;
        float high = useAlpha ? alphaHigh : lumHigh;
        if (high - low < 1f) high = low + 1f;

        var output = new Il2CppStructArray<Color32>(count);
        for (int i = 0; i < count; i++)
        {
            float v = Mathf.Clamp01((values[i] - low) / (high - low));
            output[i] = new Color32(
                (byte)(Background.r + (Floor.r - Background.r) * v),
                (byte)(Background.g + (Floor.g - Background.g) * v),
                (byte)(Background.b + (Floor.b - Background.b) * v),
                255);
        }

        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.SetPixels32(output);
        texture.Apply();

        Plugin.L.LogInfo($"Map: {sw}x{sh} {source.format}, luminance {lumLow}-{lumHigh}, alpha {alphaLow}-{alphaHigh}, " +
                         $"channel: {(useAlpha ? "alpha" : "luminance")}");
        return texture;
    }

    static int Percentile(int[] histogram, int total, float fraction)
    {
        int target = (int)(total * fraction);
        int sum = 0;
        for (int i = 0; i < histogram.Length; i++)
        {
            sum += histogram[i];
            if (sum > target) return i;
        }
        return histogram.Length - 1;
    }
}
