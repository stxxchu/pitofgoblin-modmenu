using System;
using System.Collections.Generic;
using Entities;
using Game.Players;
using Il2CppInterop.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PitOfGoblinCheats;

internal sealed class PlayerTarget
{
    public PersistentPlayer Persistent;
    public EntityPlayer Character;
    public string Name;
    public bool IsLocal;
    public float Distance;

    public bool IsDead => Character == null || Character.IsDead;
}

internal sealed class EnemyTarget
{
    public AIEntity Entity;
    public string Name;
    public float Distance;
}

internal static class Targets
{
    public static readonly List<PlayerTarget> Players = new();
    public static readonly List<EnemyTarget> Enemies = new();

    static readonly Dictionary<IntPtr, string> EnemyNames = new();
    static float _next;

    public static void Tick()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + 0.5f;

        var origin = Origin();
        ScanPlayers(origin);
        ScanEnemies(origin);
    }

    static Vector3 Origin()
    {
        if (Cheats.Player != null) return Cheats.Player.transform.position;
        var cam = Camera.main;
        return cam != null ? cam.transform.position : Vector3.zero;
    }

    static void ScanPlayers(Vector3 origin)
    {
        Players.Clear();
        if (!Cheats.InSession) return;

        IntPtr local = IntPtr.Zero;
        try
        {
            var lp = PersistentPlayer.GetLocalPlayer();
            if (lp != null) local = lp.Pointer;
        }
        catch
        {
        }

        foreach (var obj in Object.FindObjectsByType(Il2CppType.Of<PersistentPlayer>(), FindObjectsSortMode.None))
        {
            var p = obj.TryCast<PersistentPlayer>();
            if (p == null) continue;

            var ch = p.m_character;
            var target = new PlayerTarget
            {
                Persistent = p,
                Character = ch,
                IsLocal = p.Pointer == local,
                Name = PlayerName(p),
                Distance = ch != null ? Vector3.Distance(origin, ch.transform.position) : -1f,
            };
            Players.Add(target);
        }

        Players.Sort((a, b) =>
        {
            if (a.IsLocal != b.IsLocal) return a.IsLocal ? -1 : 1;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });
    }

    static string PlayerName(PersistentPlayer p)
    {
        string name = null;
        try
        {
            name = p.FilteredName;
        }
        catch
        {
        }
        return string.IsNullOrWhiteSpace(name) ? $"Player {p.OwnerClientId}" : name.Trim();
    }

    static void ScanEnemies(Vector3 origin)
    {
        Enemies.Clear();
        if (!Cheats.InSession) return;

        foreach (var obj in Object.FindObjectsByType(Il2CppType.Of<AIEntity>(), FindObjectsSortMode.None))
        {
            var e = obj.TryCast<AIEntity>();
            if (e == null || e.IsDead) continue;

            Enemies.Add(new EnemyTarget
            {
                Entity = e,
                Name = EnemyName(e),
                Distance = Vector3.Distance(origin, e.transform.position),
            });
        }

        Enemies.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        if (EnemyNames.Count > 1024) EnemyNames.Clear();
    }

    static string EnemyName(AIEntity e)
    {
        if (EnemyNames.TryGetValue(e.Pointer, out var cached)) return cached;

        string name = null;
        try
        {
            var journal = e.JournalEntryData;
            if (journal != null) name = Names.Label(journal.Name);
        }
        catch
        {
        }

        if (string.IsNullOrEmpty(name))
        {
            try
            {
                name = Names.Pretty(e.GetIl2CppType().Name);
            }
            catch
            {
                name = Names.Pretty(e.gameObject.name);
            }
        }

        EnemyNames[e.Pointer] = name;
        return name;
    }

    public static string Health(Entity e)
    {
        try
        {
            var h = e != null ? e.Health : null;
            if (h == null) return null;
            return $"{Mathf.CeilToInt(Mathf.Max(0f, h.Health))}/{Mathf.CeilToInt(h.MaxHealth)} HP";
        }
        catch
        {
            return null;
        }
    }
}
