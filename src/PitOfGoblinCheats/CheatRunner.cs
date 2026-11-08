using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace PitOfGoblinCheats;

public class CheatRunner : MonoBehaviour
{
    public CheatRunner(IntPtr ptr) : base(ptr) { }

    static readonly Color On = new(0.45f, 1f, 0.45f);
    static readonly Color Muted = new(1f, 1f, 1f, 0.6f);
    static readonly Color Accent = new(0.93f, 0.68f, 0.22f, 1f);

    static readonly HashSet<string> LoggedErrors = new();

    float _nextRefresh;

    bool _menuWasOpen;
    CursorLockMode _savedLock;
    bool _savedVisible;
    GameStateService _gameState;

    void Start()
    {
        Cheats.Init();
    }

    void Update()
    {
        if (Time.unscaledTime >= _nextRefresh)
        {
            Safe("Refresh", Cheats.Refresh);
            _nextRefresh = Time.unscaledTime + 0.5f;
        }

        Safe("Hotkeys", HandleKeys);
        Safe("Menu", UpdateMenuState);
        Safe("Enforce", Cheats.Enforce);
        Safe("Night vision", NightVision.Tick);
        Safe("Minimap", Minimap.Tick);
        Safe("Timer", RoundTimer.Tick);
        Safe("Targets", Targets.Tick);
        Safe("Watch", Watch.Tick);
        Safe("ESP", Esp.Tick);
    }

    void LateUpdate()
    {
        if (!Menu.Open) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnGUI()
    {
        if (Event.current.type == EventType.Repaint)
        {
            Safe("GUI", Gui.Begin);
            Safe("Draw ESP", Esp.Draw);
            Safe("Draw minimap", Minimap.Draw);
            Safe("Draw HUD", DrawHud);
            Safe("Draw overlay", DrawOverlay);
        }

        Safe("Menu GUI", Menu.Draw);
    }

    void OnDestroy()
    {
        if (_menuWasOpen) Safe("Closing menu", () => SetGameInput(true));
    }

    static void DrawHud()
    {
        float top = Compass.Draw(Gui.Px(14f));
        top = RoundTimer.Draw(top);
        Watch.Draw(top);
    }

    void HandleKeys()
    {
        if (Pressed(Settings.MenuKey))
            Menu.Open = !Menu.Open;

        if (!Settings.HotkeysEnabled.Value) return;

        if (Pressed(Settings.GoldKey)) Cheats.ChangeGold(Settings.GoldAmount.Value);
        if (Pressed(Settings.GodKey)) Cheats.SetGod(!Cheats.God);
        if (Pressed(Settings.StaminaKey)) Cheats.SetStamina(!Cheats.InfiniteStamina);
        if (Pressed(Settings.SpeedKey)) Cheats.SetSpeed((Cheats.SpeedIndex + 1) % Settings.SpeedMultipliers.Length);
        if (Pressed(Settings.MagicKey)) Cheats.SetMagic(!Cheats.InfiniteMagic);
        if (Pressed(Settings.MapKey)) Cheats.SetMap((MapMode)(((int)Cheats.Map + 1) % 3));
        if (Pressed(Settings.NightVisionKey)) Cheats.SetNightVision(!Cheats.NightVision);
        if (Pressed(Settings.CompassKey)) Cheats.SetCompass(!Cheats.Compass);
        if (Pressed(Settings.OverlayKey)) Cheats.Overlay = !Cheats.Overlay;
        if (Pressed(Settings.TeleportKey)) Cheats.TeleportToBucket();
        if (Pressed(Settings.OneHitKey)) Cheats.SetOneHitKill(!Cheats.OneHitKill);
        if (Pressed(Settings.WatchKey)) Cheats.SetWatchAlert(!Cheats.WatchAlert);
        if (Pressed(Settings.ItemEspKey)) Cheats.SetItemEsp(!Cheats.ItemEsp);
        if (Pressed(Settings.PlayerEspKey)) Cheats.SetPlayerEsp(!Cheats.PlayerEsp);
    }

    void UpdateMenuState()
    {
        if (Menu.Open == _menuWasOpen) return;
        _menuWasOpen = Menu.Open;

        if (Menu.Open)
        {
            _savedLock = Cursor.lockState;
            _savedVisible = Cursor.visible;
            SetGameInput(false);
        }
        else
        {
            Cursor.lockState = _savedLock;
            Cursor.visible = _savedVisible;
            SetGameInput(true);
        }
    }

    void SetGameInput(bool enabled)
    {
        var gs = FindGameState();
        if (gs == null) return;

        if (enabled) gs.UnregisterDisableInput(this);
        else gs.RegisterDisableInput(this);
    }

    GameStateService FindGameState() => _gameState ??= Services.Find<GameStateService>();

    void DrawOverlay()
    {
        var lines = new List<(string text, Color color)>();

        if (Cheats.Overlay)
        {
            if (Cheats.God) lines.Add(("God mode", On));
            if (Cheats.InfiniteStamina) lines.Add(("Infinite stamina", On));
            if (Cheats.SpeedIndex > 0) lines.Add(($"Speed {Cheats.SpeedText(Cheats.SpeedIndex)}", On));
            if (Cheats.InfiniteMagic) lines.Add(("Infinite magic", On));
            if (Cheats.OneHitKill) lines.Add(("One-hit kill", On));
            if (Cheats.Map != MapMode.Off) lines.Add(("Minimap", On));
            if (Cheats.NightVision) lines.Add(("Night vision", On));
            if (Cheats.Compass) lines.Add(("Compass → bucket", On));
            if (Cheats.WatchAlert) lines.Add(("Watch alert", On));
            if (Cheats.ItemEsp) lines.Add(("Item ESP", On));
            if (Cheats.PlayerEsp) lines.Add(("Player ESP", On));
            if (RoundTimer.Frozen) lines.Add(("Timer frozen", On));

            if (lines.Count == 0 && !Menu.Open)
                lines.Add(($"{Settings.MenuKey.Value} - mod menu", Muted));
        }

        bool toast = Notify.Visible;
        if (lines.Count == 0 && !toast) return;

        float x = Gui.Px(14f), y = Gui.Px(14f);
        float lineH = Gui.Px(22f);
        float width = Gui.Px(300f);
        float height = Gui.Px(14f) + lineH * (lines.Count + (Cheats.Overlay ? 1 : 0) + (toast ? 1 : 0));

        var box = new Rect(x, y, width, height);
        Gui.Round(box, new Color(0.04f, 0.04f, 0.05f, 0.6f), Gui.Px(10f));
        Gui.Round(new Rect(box.x, box.y + Gui.Px(10f), Gui.Px(3f), box.height - Gui.Px(20f)), Accent, Gui.Px(1.5f));

        float tx = x + Gui.Px(14f);
        float ty = y + Gui.Px(7f);
        if (Cheats.Overlay)
        {
            Gui.Text(new Rect(tx, ty, width, lineH), "CHEATS", 12, Accent, TextAnchor.MiddleLeft, bold: true);
            ty += lineH;
        }

        foreach (var (text, color) in lines)
        {
            Gui.Text(new Rect(tx, ty, width, lineH), text, 15, color, TextAnchor.MiddleLeft);
            ty += lineH;
        }

        if (toast)
            Gui.Text(new Rect(tx, ty, width - Gui.Px(20f), lineH), Notify.Text, 14, Color.white, TextAnchor.MiddleLeft, bold: true);
    }

    static bool Pressed(ConfigEntry<KeyCode> key) =>
        key.Value != KeyCode.None && Input.GetKeyDown(key.Value);

    static void Safe(string where, Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            if (LoggedErrors.Add(where + e.GetType().Name + e.Message))
                Plugin.L.LogError($"[{where}] {e}");
        }
    }
}
