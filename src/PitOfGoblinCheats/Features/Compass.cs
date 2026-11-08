using UnityEngine;

namespace PitOfGoblinCheats;

internal static class Compass
{
    static readonly Color ArrowColor = new(1f, 0.6f, 0.15f);

    public static float Draw(float top)
    {
        if (!Cheats.Compass) return top;

        float size = Gui.Px(84f);
        var circle = new Rect((Screen.width - size) / 2f, top, size, size);
        var label = new Rect(circle.x - Gui.Px(80f), circle.yMax + Gui.Px(2f), size + Gui.Px(160f), Gui.Px(24f));

        Gui.Circle(circle, new Color(0f, 0f, 0f, 0.55f));

        var bucket = Minimap.Bucket;
        var cam = Camera.main;
        if (bucket == null || cam == null)
        {
            Gui.Text(label, "No bucket", 15, new Color(1f, 1f, 1f, 0.6f), TextAnchor.UpperCenter);
            return label.yMax + Gui.Px(6f);
        }

        Vector3 from = Cheats.Player != null ? Cheats.Player.transform.position : cam.transform.position;
        Vector3 to = bucket.position - from;
        Vector3 fwd = cam.transform.forward;

        float angle = Mathf.Atan2(to.x, to.z) - Mathf.Atan2(fwd.x, fwd.z);
        angle = Mathf.DeltaAngle(0f, angle * Mathf.Rad2Deg);

        Gui.Arrow(circle.center, size * 0.7f, angle, ArrowColor);

        float flat = new Vector2(to.x, to.z).magnitude;
        string text = $"Bucket {flat:0} m";
        if (Mathf.Abs(to.y) > 3f)
            text += to.y > 0f ? $"  ({to.y:0} m above)" : $"  ({-to.y:0} m below)";

        Gui.Text(label, text, 16, Color.white, TextAnchor.UpperCenter);
        return label.yMax + Gui.Px(6f);
    }
}
