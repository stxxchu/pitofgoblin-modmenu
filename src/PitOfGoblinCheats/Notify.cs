using UnityEngine;

namespace PitOfGoblinCheats;

internal static class Notify
{
    public static string Text;
    public static float Until;

    public static bool Visible => Text != null && Time.unscaledTime < Until;

    public static void Show(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        Text = text;
        Until = Time.unscaledTime + 2.5f;
        Plugin.L.LogInfo(text);
    }

    public static void Switch(string name, bool on) =>
        Show($"{name}: {(on ? "ON" : "OFF")}");
}
