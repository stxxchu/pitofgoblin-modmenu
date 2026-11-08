using System;
using System.Globalization;
using Entities;
using Game.Players;
using Unity.Netcode;
using UnityEngine;

namespace PitOfGoblinCheats;

internal enum MapMode { Off, Small, Large }

internal static class Cheats
{
    public static bool God, InfiniteStamina, InfiniteMagic, OneHitKill, NightVision, Compass, Overlay, Timer, WatchAlert, ItemEsp, PlayerEsp;
    public static int SpeedIndex;
    public static MapMode Map;

    public static bool UsedThisSession;

    public static bool IsHost = true;
    public static bool InSession;
    public static int PlayerCount = 1;

    public static EntityPlayer Player;
    public static EntityHealth Health;
    public static StaminaSystem Stamina;
    public static PlayerController Controller;
    public static MagicSystem Magic;

    public static IntPtr PlayerPtr, StaminaPtr, ControllerPtr;

    public static int Gold = -1;

    public static float SpeedMultiplier => Settings.SpeedMultipliers[SpeedIndex];

    public static string SpeedText(int index) =>
        "x" + Settings.SpeedMultipliers[index].ToString("0.##", CultureInfo.InvariantCulture);

    public static bool AnyGameplayCheat => God || InfiniteStamina || InfiniteMagic || OneHitKill || SpeedIndex > 0;

    public static bool AnyCheat => AnyGameplayCheat || NightVision || Compass || WatchAlert || ItemEsp || PlayerEsp || Map != MapMode.Off;

    public static void Init()
    {
        Compass = Settings.CompassOnStart.Value;
        Overlay = Settings.OverlayOnStart.Value;
        Timer = Settings.TimerOnStart.Value;
        WatchAlert = Settings.WatchOnStart.Value;
    }

    public static void Refresh()
    {
        RefreshSession();
        RefreshPlayer();

        var rm = ResourceManager.Instance;
        Gold = rm != null ? rm.PlayerGold : -1;
    }

    static void RefreshSession()
    {
        var nm = NetworkManager.Singleton;
        IsHost = nm == null || nm.IsServer || !nm.IsClient;
        InSession = nm != null && (nm.IsClient || nm.IsServer);

        var all = PersistentPlayer.Players;
        PlayerCount = Math.Max(1, all != null ? all.Count : 1);
    }

    static void RefreshPlayer()
    {
        EntityPlayer ent = null;

        if (InSession)
        {
            try
            {
                var local = PersistentPlayer.GetLocalPlayer();
                if (local != null) ent = local.m_character;
            }
            catch
            {
            }
        }

        if (ent == null)
        {
            Player = null;
            Health = null;
            Stamina = null;
            Controller = null;
            Magic = null;
            PlayerPtr = StaminaPtr = ControllerPtr = IntPtr.Zero;
            return;
        }

        if (Player != null && ent.Pointer == PlayerPtr) return;

        Player = ent;
        Health = ent.Health;
        Stamina = ent.StaminaSystem;
        Controller = ent.PlayerController;
        Magic = ent.MagicSystem;

        PlayerPtr = ent.Pointer;
        StaminaPtr = Stamina != null ? Stamina.Pointer : IntPtr.Zero;
        ControllerPtr = Controller != null ? Controller.Pointer : IntPtr.Zero;
    }

    public static void Enforce()
    {
        if (AnyCheat) UsedThisSession = true;
        if (Player == null) return;

        if (God && Health != null)
        {
            float missing = Health.MaxHealth - Health.Health;
            if (missing > 0.01f) Health.GainHealth(missing, false);
        }

        if (InfiniteStamina && Stamina != null)
            Stamina.m_staminaCurrent = Stamina.MaxStamina;
    }

    public static void SetGod(bool on)
    {
        God = on;
        Notify.Switch("God mode", on);
    }

    public static void SetStamina(bool on)
    {
        InfiniteStamina = on;
        Notify.Switch("Infinite stamina", on);
    }

    public static void SetMagic(bool on)
    {
        InfiniteMagic = on;
        Notify.Switch("Infinite magic", on);
        if (on && Settings.RefillMagic.Value) RefillMagic(quiet: true);
    }

    public static void SetSpeed(int index)
    {
        SpeedIndex = Mathf.Clamp(index, 0, Settings.SpeedMultipliers.Length - 1);
        Notify.Show($"Speed {SpeedText(SpeedIndex)}");
    }

    public static void SetMap(MapMode mode)
    {
        Map = mode;
        Notify.Show(mode switch
        {
            MapMode.Small => "Minimap: small",
            MapMode.Large => "Minimap: large",
            _ => "Minimap: OFF",
        });
    }

    public static void SetNightVision(bool on)
    {
        NightVision = on;
        Notify.Switch("Night vision", on);
    }

    public static void SetCompass(bool on)
    {
        Compass = on;
        Notify.Switch("Compass", on);
    }

    public static void SetTimer(bool on)
    {
        Timer = on;
        Notify.Switch("Return timer", on);
    }

    public static void SetOneHitKill(bool on)
    {
        OneHitKill = on;
        Notify.Switch("One-hit kill", on);
    }

    public static void SetWatchAlert(bool on)
    {
        WatchAlert = on;
        Notify.Switch("Watch alert", on);
    }

    public static void SetItemEsp(bool on)
    {
        ItemEsp = on;
        Notify.Switch("Item ESP", on);
    }

    public static void SetPlayerEsp(bool on)
    {
        PlayerEsp = on;
        Notify.Switch("Player ESP", on);
    }

    public static void TeleportToBucket()
    {
        if (Player == null || Player.IsDead)
        {
            Notify.Show("You need to be alive in a run to teleport");
            return;
        }
        if (Game.Gameplay.BucketSpawner.Bucket == null)
        {
            Notify.Show("There is no bucket here");
            return;
        }

        Player.TeleportToBucket();
        UsedThisSession = true;
        Notify.Show("Teleported to the bucket");
    }

    public static void ChangeGold(int delta)
    {
        if (delta == 0) return;

        var rm = ResourceManager.Instance;
        if (rm == null)
        {
            Notify.Show("No gold here, join a game first");
            return;
        }

        int current = rm.PlayerGold;
        int target = (int)Math.Clamp((long)current + delta, 0L, int.MaxValue);
        if (target == current)
        {
            Notify.Show("There is no gold left to take");
            return;
        }

        if (IsHost) rm.SetGold(target);
        else if (target > current) rm.AddGold(target - current);
        else rm.ReduceGoldRpc(current - target);

        UsedThisSession = true;
        Gold = target;

        int change = target - current;
        string text = change > 0 ? $"+{change} gold" : $"−{-change} gold";
        Notify.Show(IsHost ? $"{text} (now {Gold})" : text);
    }

    public static void RefillMagic(bool quiet = false)
    {
        var magic = Magic != null ? Magic.CurrentMagicClass : null;
        if (magic == null)
        {
            if (!quiet) Notify.Show("You have no magic right now");
            return;
        }

        int missing = magic.MaxCharges - magic.Charges;
        if (missing > 0) magic.TryGainCharge(missing);
        UsedThisSession = true;
        Notify.Show("Magic refilled");
    }
}
