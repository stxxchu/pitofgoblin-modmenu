using System.Collections.Generic;
using UnityEngine;

namespace PitOfGoblinCheats;

internal static class Gui
{
    public static float S = 1f;
    public static float Alpha = 1f;

    static GUIStyle _style;
    static Texture2D _dot, _arrow;

    static readonly Dictionary<int, float> Anim = new();

    public static void Begin()
    {
        S = Screen.height / 1080f * Settings.UiScale.Value;
        Alpha = 1f;
        if (_style == null)
        {
            _style = new GUIStyle();
            _dot = MakeCircle(64);
            _arrow = MakeArrow(64);
        }
    }

    public static float Px(float v) => v * S;

    static Color Fade(Color c) => new(c.r, c.g, c.b, c.a * Alpha);

    public static float Animate(string id, float target, float speed = 14f)
    {
        int key = id.GetHashCode();
        if (!Anim.TryGetValue(key, out float v)) v = target;
        if (Event.current.type == EventType.Repaint)
            v = Mathf.Lerp(v, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
        Anim[key] = v;
        return v;
    }

    public static void Fill(Rect r, Color c)
    {
        GUI.color = Fade(c);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    public static void Round(Rect r, Color c, float radius)
    {
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, Fade(c), 0f, radius);
    }

    public static void Outline(Rect r, Color c, float width, float radius)
    {
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, Fade(c), width, radius);
    }

    public static void Shadow(Rect r, float radius)
    {
        for (int i = 3; i >= 1; i--)
        {
            float grow = Px(i * 4f);
            var s = new Rect(r.x - grow, r.y - grow + Px(i * 3f), r.width + grow * 2f, r.height + grow * 2f);
            Round(s, new Color(0f, 0f, 0f, 0.07f * (4 - i)), radius + grow);
        }
    }

    public static void Text(Rect r, string text, int size, Color color,
        TextAnchor anchor = TextAnchor.UpperLeft, bool bold = false, bool shadow = true)
    {
        _style.fontSize = Mathf.RoundToInt(size * S);
        _style.alignment = anchor;
        _style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;

        if (shadow)
        {
            _style.normal.textColor = new Color(0f, 0f, 0f, 0.8f * color.a * Alpha);
            float o = Mathf.Max(1f, S * 1.2f);
            GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), text, _style);
        }

        _style.normal.textColor = Fade(color);
        GUI.Label(r, text, _style);
    }

    public static void Dot(Vector2 p, float size, Color c)
    {
        GUI.color = Fade(c);
        GUI.DrawTexture(new Rect(p.x - size / 2f, p.y - size / 2f, size, size), _dot);
        GUI.color = Color.white;
    }

    public static void Square(Vector2 p, float size, Color c)
    {
        Fill(new Rect(p.x - size / 2f - 1f, p.y - size / 2f - 1f, size + 2f, size + 2f), Color.black);
        Fill(new Rect(p.x - size / 2f, p.y - size / 2f, size, size), c);
    }

    public static void Circle(Rect r, Color c)
    {
        GUI.color = Fade(c);
        GUI.DrawTexture(r, _dot);
        GUI.color = Color.white;
    }

    public static void Switch(Rect r, float t, Color accent, bool hover)
    {
        var off = new Color(1f, 1f, 1f, hover ? 0.2f : 0.13f);
        Round(r, Color.Lerp(off, accent, t), r.height / 2f);

        float knob = r.height - Px(6f);
        float x = Mathf.Lerp(r.x + Px(3f), r.xMax - knob - Px(3f), t);
        Circle(new Rect(x, r.y + Px(3f), knob, knob), Color.Lerp(Color.white, new Color(0.12f, 0.09f, 0.04f), t));
    }

    public static void Arrow(Vector2 center, float size, float angle, Color c)
    {
        var old = GUI.matrix;

        float rad = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
        var shift = new Vector3(
            center.x - (cos * center.x - sin * center.y),
            center.y - (sin * center.x + cos * center.y),
            0f);
        GUI.matrix = Matrix4x4.TRS(shift, Quaternion.Euler(0f, 0f, angle), Vector3.one);

        GUI.color = Fade(c);
        GUI.DrawTexture(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), _arrow);
        GUI.color = Color.white;

        GUI.matrix = old;
    }

    public static void Icon(Rect r, Texture2D texture, Rect source)
    {
        if (texture == null || source.width <= 0f || source.height <= 0f) return;

        float scale = Mathf.Min(r.width / source.width, r.height / source.height);
        float w = source.width * scale, h = source.height * scale;
        var box = new Rect(r.x + (r.width - w) / 2f, r.y + (r.height - h) / 2f, w, h);

        GUI.BeginGroup(box, GUIContent.none, GUIStyle.none);
        GUI.color = Fade(Color.white);
        GUI.DrawTexture(new Rect(-source.x * scale, -(texture.height - source.yMax) * scale,
            texture.width * scale, texture.height * scale), texture);
        GUI.color = Color.white;
        GUI.EndGroup();
    }

    static Texture2D NewTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    static Texture2D MakeCircle(int size)
    {
        var tex = NewTexture(size);
        float r = size / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
        }
        tex.Apply();
        return tex;
    }

    static Texture2D MakeArrow(int size)
    {
        var tex = NewTexture(size);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = (x + 0.5f) / size - 0.5f;
            float v = (y + 0.5f) / size;
            float halfWidth = (1f - v) * 0.45f;
            float notch = 0.25f - Mathf.Abs(u) * 0.55f;
            bool inside = v > 0.05f && v < 0.95f && Mathf.Abs(u) < halfWidth && v > notch;
            tex.SetPixel(x, y, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
        }
        tex.Apply();
        return tex;
    }
}
