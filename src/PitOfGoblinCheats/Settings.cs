using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using UnityEngine;

namespace PitOfGoblinCheats;

internal static class Settings
{
    public static ConfigEntry<KeyCode> MenuKey;
    public static ConfigEntry<bool> HotkeysEnabled;
    public static ConfigEntry<KeyCode> GoldKey, GodKey, StaminaKey, SpeedKey, MagicKey, MapKey, NightVisionKey, CompassKey, OverlayKey,
        TeleportKey, OneHitKey, WatchKey, ItemEspKey, PlayerEspKey;

    public static ConfigEntry<int> GoldAmount;
    public static ConfigEntry<string> SpeedSteps;
    public static ConfigEntry<bool> RefillMagic;

    public static ConfigEntry<float> MinimapSize, MinimapZoom;
    public static ConfigEntry<bool> MinimapEnemies, MinimapLoot;

    public static ConfigEntry<float> NvExposure, NvLightBoost, NvLightRange;
    public static ConfigEntry<bool> NvGreenTint, NvDisableFog;

    public static ConfigEntry<bool> CompassOnStart, OverlayOnStart, TimerOnStart, WatchOnStart;
    public static ConfigEntry<float> ItemEspRange, TimerStep;
    public static ConfigEntry<float> SpawnDistance;
    public static ConfigEntry<float> UiScale;

    public static ConfigEntry<bool> BlockAchievements;

    public static float[] SpeedMultipliers { get; private set; } = { 1f };

    public static void Bind(ConfigFile cfg)
    {
        MenuKey = cfg.Bind("Menu", "Key", KeyCode.Insert, "Opens and closes the mod menu");

        HotkeysEnabled = cfg.Bind("Hotkeys", "Enabled", true, "Extra hotkeys below (everything is also in the menu)");
        GoldKey = cfg.Bind("Hotkeys", "AddGold", KeyCode.F1, "Adds gold");
        GodKey = cfg.Bind("Hotkeys", "GodMode", KeyCode.F2, "Toggles god mode");
        StaminaKey = cfg.Bind("Hotkeys", "InfiniteStamina", KeyCode.F3, "Toggles infinite stamina");
        SpeedKey = cfg.Bind("Hotkeys", "Speed", KeyCode.F4, "Cycles the speed multiplier");
        MagicKey = cfg.Bind("Hotkeys", "InfiniteMagic", KeyCode.F5, "Toggles infinite magic");
        MapKey = cfg.Bind("Hotkeys", "Minimap", KeyCode.F6, "Minimap: off -> small -> large");
        NightVisionKey = cfg.Bind("Hotkeys", "NightVision", KeyCode.F7, "Toggles night vision");
        CompassKey = cfg.Bind("Hotkeys", "Compass", KeyCode.F8, "Toggles the bucket compass");
        OverlayKey = cfg.Bind("Hotkeys", "Overlay", KeyCode.F9, "Shows or hides the active cheat list");
        TeleportKey = cfg.Bind("Hotkeys", "TeleportToBucket", KeyCode.F10, "Teleports you to the bucket");
        OneHitKey = cfg.Bind("Hotkeys", "OneHitKill", KeyCode.None, "Toggles one-hit kill");
        WatchKey = cfg.Bind("Hotkeys", "WatchAlert", KeyCode.None, "Toggles the watch alert");
        ItemEspKey = cfg.Bind("Hotkeys", "ItemEsp", KeyCode.None, "Toggles item ESP");
        PlayerEspKey = cfg.Bind("Hotkeys", "PlayerEsp", KeyCode.None, "Toggles player ESP");

        GoldAmount = cfg.Bind("Gold", "HotkeyAmount", 1000, "How much gold the hotkey adds");

        SpeedSteps = cfg.Bind("Speed", "Multipliers", "1, 1.5, 2, 3",
            "Movement speed steps, separated by commas. The first one should be 1");

        RefillMagic = cfg.Bind("Magic", "RefillOnEnable", true, "Fills your current magic right away when infinite magic is turned on");

        MinimapSize = cfg.Bind("Minimap", "SmallSize", 300f, "Size of the small minimap in pixels (at 1080p)");
        MinimapZoom = cfg.Bind("Minimap", "SmallZoom", 3f, "Zoom of the small minimap (1 = whole map)");
        MinimapEnemies = cfg.Bind("Minimap", "ShowEnemies", true, "Red dots = enemies");
        MinimapLoot = cfg.Bind("Minimap", "ShowLoot", true, "Yellow = treasure, green = food");

        NvExposure = cfg.Bind("NightVision", "Exposure", 2.5f, "Extra exposure in EV (0 = no brightening)");
        NvLightBoost = cfg.Bind("NightVision", "GoblinLightBoost", 4f, "Brightness multiplier for your goblin's own light");
        NvLightRange = cfg.Bind("NightVision", "GoblinLightRange", 30f, "Minimum range of your goblin's light in meters");
        NvGreenTint = cfg.Bind("NightVision", "GreenTint", true, "Green tint like real night vision goggles");
        NvDisableFog = cfg.Bind("NightVision", "DisableFog", true, "Turns fog off while night vision is on");

        CompassOnStart = cfg.Bind("Compass", "EnabledOnStart", false, "Compass is on right after the game starts");

        TimerOnStart = cfg.Bind("Timer", "EnabledOnStart", true, "Countdown until the bucket leaves");
        TimerStep = cfg.Bind("Timer", "StepSeconds", 60f, "How many seconds the timer buttons add or take away (host only)");

        WatchOnStart = cfg.Bind("WatchAlert", "EnabledOnStart", false, "Shows which dead teammates are spectating you, right after the game starts");

        ItemEspRange = cfg.Bind("ESP", "ItemRange", 60f, "Items further away than this are not outlined (m)");

        SpawnDistance = cfg.Bind("Items", "SpawnDistance", 1.5f, "How far in front of you items appear (m)");

        OverlayOnStart = cfg.Bind("Overlay", "VisibleOnStart", true, "Active cheat list is visible after the game starts");
        UiScale = cfg.Bind("Overlay", "Scale", 1f, "Scale of the whole mod UI");

        BlockAchievements = cfg.Bind("Safety", "BlockAchievements", true,
            "After any cheat is used, Xbox achievements are not granted until the game restarts");

        SpeedMultipliers = ParseSteps(SpeedSteps.Value);
    }

    static float[] ParseSteps(string raw)
    {
        var list = new List<float>();
        foreach (var part in raw.Split(','))
        {
            if (float.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && v > 0f)
                list.Add(v);
        }
        if (list.Count == 0 || list[0] != 1f)
            list.Insert(0, 1f);
        return list.ToArray();
    }
}
