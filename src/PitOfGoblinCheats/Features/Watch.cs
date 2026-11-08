using System;
using System.Collections.Generic;
using Entities;
using UnityEngine;

namespace PitOfGoblinCheats;

internal struct Spectator
{
    public IntPtr Persistent;
    public string Name;
    public string TargetName;
    public bool OnMe;
    public bool Certain;
}

internal static class Watch
{
    static readonly Color Red = new(1f, 0.32f, 0.28f);
    static readonly Color Orange = new(1f, 0.62f, 0.25f);
    static readonly Color Dim = new(1f, 1f, 1f, 0.55f);

    public static readonly List<Spectator> Current = new();

    static readonly Dictionary<IntPtr, IntPtr> Following = new();
    static readonly List<EntityPlayer> Order = new();
    static readonly HashSet<IntPtr> StillDead = new();
    static readonly List<IntPtr> Stale = new();

    static float _next;

    public static bool OnMe
    {
        get
        {
            foreach (var s in Current)
                if (s.OnMe) return true;
            return false;
        }
    }

    public static (string Text, Color Color)? Describe(IntPtr persistent)
    {
        foreach (var s in Current)
        {
            if (s.Persistent != persistent) continue;
            if (s.OnMe) return s.Certain ? ("spectating you", Red) : ("probably spectating you", Orange);
            return ($"probably watching {s.TargetName}", Dim);
        }
        return null;
    }

    public static void Tick()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + 0.25f;

        Current.Clear();
        StillDead.Clear();
        if (!Cheats.InSession) return;

        BuildOrder();
        int alive = 0;
        foreach (var p in Order)
            if (!p.IsDead) alive++;

        foreach (var p in Targets.Players)
        {
            if (p.IsLocal || !p.IsDead) continue;

            var key = p.Persistent.Pointer;
            StillDead.Add(key);

            Following.TryGetValue(key, out var previous);
            var target = FindAlive(previous) ?? NextAlive(previous);
            Following[key] = target != null ? target.Pointer : IntPtr.Zero;
            if (target == null) continue;

            Current.Add(new Spectator
            {
                Persistent = key,
                Name = p.Name,
                TargetName = NameOf(target),
                OnMe = target.Pointer == Cheats.PlayerPtr,
                Certain = alive == 1,
            });
        }

        Stale.Clear();
        foreach (var key in Following.Keys)
            if (!StillDead.Contains(key)) Stale.Add(key);
        foreach (var key in Stale) Following.Remove(key);

        Current.Sort((a, b) => b.OnMe.CompareTo(a.OnMe));
    }

    static void BuildOrder()
    {
        Order.Clear();

        SpawnedObjectsService spawned = null;
        try
        {
            spawned = Services.Find<SpawnedObjectsService>();
        }
        catch
        {
        }

        var players = spawned?.m_players;
        if (players != null)
        {
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p != null) Order.Add(p);
            }
        }

        if (Order.Count > 0) return;
        foreach (var p in Targets.Players)
            if (p.Character != null) Order.Add(p.Character);
    }

    static EntityPlayer FindAlive(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return null;
        foreach (var p in Order)
            if (p.Pointer == ptr && !p.IsDead) return p;
        return null;
    }

    static EntityPlayer NextAlive(IntPtr after)
    {
        int start = 0;
        for (int i = 0; i < Order.Count; i++)
        {
            if (Order[i].Pointer != after) continue;
            start = i + 1;
            break;
        }

        for (int n = 0; n < Order.Count; n++)
        {
            var p = Order[(start + n) % Order.Count];
            if (!p.IsDead) return p;
        }
        return null;
    }

    static string NameOf(EntityPlayer ch)
    {
        if (ch.Pointer == Cheats.PlayerPtr) return "you";
        foreach (var p in Targets.Players)
            if (p.Character != null && p.Character.Pointer == ch.Pointer) return p.Name;
        return "someone";
    }

    public static float Draw(float top)
    {
        if (!Cheats.WatchAlert || Current.Count == 0 || Cheats.Player == null || Cheats.Player.IsDead) return top;

        bool onMe = OnMe;
        int lines = Mathf.Min(Current.Count, 5);
        float w = Gui.Px(330f), header = Gui.Px(30f), lineH = Gui.Px(21f);
        var box = new Rect((Screen.width - w) / 2f, top, w, header + lines * lineH + Gui.Px(8f));

        Gui.Round(box, new Color(0f, 0f, 0f, 0.55f), Gui.Px(10f));
        if (onMe)
        {
            float pulse = 0.55f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
            Gui.Outline(box, new Color(Red.r, Red.g, Red.b, pulse), Gui.Px(1.5f), Gui.Px(10f));
            if (Mathf.Repeat(Time.unscaledTime, 0.8f) < 0.5f)
                Gui.Dot(new Vector2(box.x + Gui.Px(18f), box.y + header / 2f), Gui.Px(9f), Red);
        }
        else
        {
            Gui.Dot(new Vector2(box.x + Gui.Px(18f), box.y + header / 2f), Gui.Px(9f), Dim);
        }

        Gui.Text(new Rect(box.x + Gui.Px(32f), box.y, box.width, header), onMe ? "A DEAD PLAYER IS SPECTATING YOU" : "Dead teammates", 13,
            onMe ? Red : new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleLeft, bold: onMe);

        float y = box.y + header;
        for (int i = 0; i < lines; i++)
        {
            var s = Current[i];
            var (text, color) = Describe(s.Persistent) ?? ("", Dim);

            var row = new Rect(box.x + Gui.Px(14f), y, box.width - Gui.Px(28f), lineH);
            Gui.Text(row, s.Name, 13, Color.white, TextAnchor.MiddleLeft, bold: true);
            Gui.Text(row, text, 12, color, TextAnchor.MiddleRight);
            y += lineH;
        }

        return box.yMax + Gui.Px(8f);
    }
}
