using Game.Gameplay;
using UnityEngine;

namespace PitOfGoblinCheats;

internal static class RoundTimer
{
    static GameMode_Round _mode;
    static float _nextLookup;

    static float _frozenElapsed;

    public static bool Active { get; private set; }
    public static float Remaining { get; private set; }
    public static bool Warning { get; private set; }
    public static bool Frozen { get; private set; }

    public static bool CanEdit => Active && Cheats.IsHost;

    public static void Tick()
    {
        if (Time.unscaledTime >= _nextLookup)
        {
            _mode = GameMode_Round.Instance;
            _nextLookup = Time.unscaledTime + 1f;
        }

        Active = false;
        if (_mode == null || !_mode.m_gameStarted)
        {
            Frozen = false;
            return;
        }

        if (Frozen && Cheats.IsHost)
        {
            float drift = _mode.ElapsedGameTime - _frozenElapsed;
            if (Mathf.Abs(drift) > 0.001f) _mode.m_startTime += drift;
        }

        float elapsed = _mode.ElapsedGameTime;
        Remaining = _mode.RoundTime - elapsed;
        Warning = elapsed >= _mode.WarningTime;
        Active = true;
    }

    public static void Shift(float seconds)
    {
        if (!CheckEdit()) return;

        float remaining = _mode.RoundTime - _mode.ElapsedGameTime;
        float delta = Mathf.Max(10f, remaining + seconds) - remaining;
        _mode.m_startTime += delta;
        _frozenElapsed -= delta;

        Cheats.UsedThisSession = true;
        Notify.Show(delta >= 0f ? $"+{StepText(delta)} on the clock" : $"−{StepText(-delta)} on the clock");
    }

    public static void SetFrozen(bool on)
    {
        if (!CheckEdit()) return;

        Frozen = on;
        _frozenElapsed = _mode.ElapsedGameTime;
        Cheats.UsedThisSession = true;
        Notify.Switch("Timer freeze", on);
    }

    public static string StepText(float seconds) =>
        seconds >= 60f ? $"{seconds / 60f:0.#} min" : $"{seconds:0} s";

    static bool CheckEdit()
    {
        if (!Active)
        {
            Notify.Show("No round is running");
            return false;
        }
        if (!Cheats.IsHost)
        {
            Notify.Show("Only the host can change the timer");
            return false;
        }
        return true;
    }

    public static string Format(float seconds)
    {
        int s = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return $"{s / 60}:{s % 60:00}";
    }

    public static float Draw(float top)
    {
        if (!Cheats.Timer || !Active) return top;

        var color = Color.white;
        string caption = Frozen ? "timer frozen" : "until the bucket leaves";
        if (Remaining <= 0f)
        {
            color = Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.4f ? new Color(1f, 0.25f, 0.2f) : Color.white;
            caption = "THE BUCKET IS LEAVING!";
        }
        else if (Remaining <= 30f) color = new Color(1f, 0.3f, 0.25f);
        else if (Warning) color = new Color(1f, 0.65f, 0.2f);

        float w = Gui.Px(220f);
        var box = new Rect((Screen.width - w) / 2f, top, w, Gui.Px(56f));
        Gui.Round(box, new Color(0f, 0f, 0f, 0.5f), Gui.Px(10f));
        Gui.Text(new Rect(box.x, box.y + Gui.Px(2f), box.width, Gui.Px(34f)), Format(Remaining), 28, color, TextAnchor.MiddleCenter, bold: true);
        Gui.Text(new Rect(box.x, box.y + Gui.Px(34f), box.width, Gui.Px(18f)), caption, 12, new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleCenter);
        return box.yMax + Gui.Px(8f);
    }
}
