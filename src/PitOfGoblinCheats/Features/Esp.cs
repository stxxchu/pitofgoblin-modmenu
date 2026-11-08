using System;
using System.Collections.Generic;
using EPOOutline;
using Il2CppInterop.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PitOfGoblinCheats;

internal static class Esp
{
    static readonly Color TeammateColor = new(0.35f, 0.85f, 1f);
    static readonly Color DeadColor = new(1f, 0.35f, 0.3f);

    static readonly Dictionary<IntPtr, Outlinable> ItemOutlines = new();
    static readonly Dictionary<IntPtr, Outlinable> PlayerOutlines = new();
    static readonly HashSet<IntPtr> Seen = new();
    static readonly List<IntPtr> Gone = new();

    static Outlinable _template;
    static float _next;

    public static Color RarityColor(InventoryItem.ItemRarity rarity) => rarity switch
    {
        InventoryItem.ItemRarity.Uncommon => new Color(0.5f, 0.92f, 0.5f),
        InventoryItem.ItemRarity.Rare => new Color(0.45f, 0.72f, 1f),
        InventoryItem.ItemRarity.Mythical => new Color(0.78f, 0.55f, 1f),
        InventoryItem.ItemRarity.Legendary => new Color(1f, 0.66f, 0.22f),
        InventoryItem.ItemRarity.Relic => new Color(1f, 0.42f, 0.36f),
        _ => new Color(0.9f, 0.9f, 0.9f),
    };

    public static void Tick()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + 0.5f;

        UpdateItems();
        UpdatePlayers();
    }

    static void UpdateItems()
    {
        Seen.Clear();

        if (Cheats.ItemEsp && Cheats.InSession)
        {
            var origin = Origin();
            float range = Settings.ItemEspRange.Value;

            foreach (var obj in Object.FindObjectsByType(Il2CppType.Of<InventoryItem>(), FindObjectsSortMode.None))
            {
                var item = obj.TryCast<InventoryItem>();
                if (item == null || item.IsInInventory) continue;
                if ((item.transform.position - origin).sqrMagnitude > range * range) continue;

                if (_template == null && item.Outline != null) _template = item.Outline;

                if (!ItemOutlines.TryGetValue(item.Pointer, out var outline) || outline == null)
                {
                    outline = Create(item.gameObject, item.Outline);
                    item.AddToOutline(outline);
                    ItemOutlines[item.Pointer] = outline;
                }

                Show(outline, RarityColor(item.Rarity));
                Seen.Add(item.Pointer);
            }
        }

        HideUnseen(ItemOutlines);
    }

    static void UpdatePlayers()
    {
        Seen.Clear();

        if (Cheats.PlayerEsp)
        {
            foreach (var p in Targets.Players)
            {
                var ch = p.Character;
                if (p.IsLocal || ch == null) continue;

                if (!PlayerOutlines.TryGetValue(ch.Pointer, out var outline) || outline == null)
                {
                    outline = Create(ch.gameObject, ch.Outline);
                    PlayerOutlines[ch.Pointer] = outline;
                }

                Refill(outline, ch);
                Show(outline, p.IsDead ? DeadColor : TeammateColor);
                Seen.Add(ch.Pointer);
            }
        }

        HideUnseen(PlayerOutlines);
    }

    static Outlinable Create(GameObject owner, Outlinable template)
    {
        var source = template != null ? template : _template;
        var outline = owner.AddComponent<Outlinable>();

        if (source != null)
        {
            outline.OutlineLayer = source.OutlineLayer;
            outline.DrawingMode = source.DrawingMode;
            CopyShape(source.OutlineParameters, outline.FrontParameters);
            CopyShape(source.OutlineParameters, outline.BackParameters);
        }

        outline.RenderStyle = RenderStyle.FrontBack;
        outline.enabled = false;
        return outline;
    }

    static void CopyShape(Outlinable.OutlineProperties from, Outlinable.OutlineProperties to)
    {
        if (from == null || to == null) return;
        to.DilateShift = from.DilateShift;
        to.BlurShift = from.BlurShift;
    }

    static void Refill(Outlinable outline, Entities.EntityPlayer ch)
    {
        for (int i = outline.OutlineTargetsCount - 1; i >= 0; i--)
            outline.RemoveTarget(outline[i]);
        ch.AddToOutline(outline);
    }

    static void Show(Outlinable outline, Color color)
    {
        var front = outline.FrontParameters;
        front.Enabled = true;
        front.Color = color;

        var back = outline.BackParameters;
        back.Enabled = true;
        back.Color = new Color(color.r, color.g, color.b, 0.85f);

        if (!outline.enabled) outline.enabled = true;
    }

    static void HideUnseen(Dictionary<IntPtr, Outlinable> map)
    {
        Gone.Clear();
        foreach (var pair in map)
        {
            var outline = pair.Value;
            if (outline == null)
            {
                Gone.Add(pair.Key);
                continue;
            }
            if (!Seen.Contains(pair.Key) && outline.enabled) outline.enabled = false;
        }
        foreach (var key in Gone) map.Remove(key);
    }

    static Vector3 Origin()
    {
        if (Cheats.Player != null) return Cheats.Player.transform.position;
        var cam = Camera.main;
        return cam != null ? cam.transform.position : Vector3.zero;
    }

    public static void Draw()
    {
        if (!Cheats.PlayerEsp) return;

        var cam = Camera.main;
        if (cam == null) return;

        foreach (var p in Targets.Players)
        {
            var ch = p.Character;
            if (p.IsLocal || ch == null) continue;

            Vector3 head = ch.transform.position + Vector3.up * 1.5f;
            Vector3 screen = cam.WorldToScreenPoint(head);
            if (screen.z <= 0f) continue;

            float x = screen.x, y = Screen.height - screen.y;
            var color = p.IsDead ? DeadColor : TeammateColor;
            string name = p.IsDead ? $"{p.Name} (dead)" : p.Name;

            Gui.Text(new Rect(x - Gui.Px(120f), y - Gui.Px(36f), Gui.Px(240f), Gui.Px(20f)), name, 14, color, TextAnchor.MiddleCenter, bold: true);
            float distance = Vector3.Distance(cam.transform.position, head);
            Gui.Text(new Rect(x - Gui.Px(120f), y - Gui.Px(18f), Gui.Px(240f), Gui.Px(16f)), $"{distance:0} m", 11,
                new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter);
        }
    }
}
