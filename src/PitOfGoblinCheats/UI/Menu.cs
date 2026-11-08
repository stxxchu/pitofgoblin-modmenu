using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PitOfGoblinCheats;

internal static class Menu
{
    public static bool Open;

    enum Page { Player, Combat, Visuals, World, Items }

    static readonly Color Bg = new(0.067f, 0.067f, 0.082f, 0.97f);
    static readonly Color SidebarBg = new(0.045f, 0.045f, 0.056f, 0.98f);
    static readonly Color Border = new(1f, 1f, 1f, 0.07f);
    static readonly Color CardBg = new(1f, 1f, 1f, 0.035f);
    static readonly Color CardBgHover = new(1f, 1f, 1f, 0.065f);
    static readonly Color Well = new(0f, 0f, 0f, 0.28f);
    static readonly Color Accent = new(0.95f, 0.69f, 0.23f, 1f);
    static readonly Color Btn = new(1f, 1f, 1f, 0.07f);
    static readonly Color BtnHover = new(1f, 1f, 1f, 0.14f);
    static readonly Color TextMain = new(0.97f, 0.96f, 0.93f, 1f);
    static readonly Color TextSoft = new(1f, 1f, 1f, 0.78f);
    static readonly Color TextDim = new(1f, 1f, 1f, 0.48f);
    static readonly Color TextFaint = new(1f, 1f, 1f, 0.28f);
    static readonly Color TextOnAccent = new(0.12f, 0.09f, 0.04f, 1f);
    static readonly Color Good = new(0.42f, 0.9f, 0.48f);
    static readonly Color Danger = new(1f, 0.4f, 0.34f);
    static readonly Color Warn = new(1f, 0.64f, 0.25f);
    static readonly Color GoldColor = new(1f, 0.82f, 0.32f);
    static readonly Color FoodColor = new(0.52f, 0.95f, 0.52f);

    static readonly (string Icon, string Name, string Subtitle)[] Pages =
    {
        ("♥", "Player", "Gold, health, stamina, magic and speed"),
        ("♠", "Combat", "Kill, revive, teleport players and see who spectates you"),
        ("◊", "Visuals", "ESP, minimap, night vision and compass"),
        ("☼", "World", "Teleport, round timer and on-screen info"),
        ("♦", "Items", "Every item in the game, right in front of you"),
    };

    static readonly int[] GoldPresets = { 100, 1000, 10000, 100000 };
    static readonly int[] Quantities = { 1, 5, 10 };
    static readonly (string Name, ItemType? Type)[] Categories =
    {
        ("All", null), ("Treasure", ItemType.Treasure), ("Food", ItemType.Food),
        ("Items", ItemType.Item), ("Weapons", ItemType.Weapon), ("Relics", ItemType.Relic), ("Keys", ItemType.Key),
    };

    static Page _page;
    static Vector2 _pos = new(-1f, -1f);
    static bool _dragging;
    static Vector2 _dragOffset;
    static float _open;
    static string _focus;

    static string _goldInput = "1000";

    static string _search = "";
    static int _category, _quantity;
    static ItemSort _sort;
    static float _scroll;
    static readonly List<SpawnableItem> Filtered = new();

    static int _combatTab;
    static float _combatScroll;

    static Event _e;
    static bool _repaint, _interactive;
    static float _x, _y, _w;

    public static void Draw()
    {
        _e = Event.current;
        _repaint = _e.type == EventType.Repaint;

        if (_repaint)
            _open = Mathf.Lerp(_open, Open ? 1f : 0f, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));

        if (!Open)
        {
            _focus = null;
            _dragging = false;
        }
        if (_open < 0.01f) return;

        _interactive = Open;
        if (_interactive && _e.type == EventType.MouseDown) _focus = null;

        float W = Gui.Px(780f), H = Gui.Px(600f);
        if (_pos.x < 0f) _pos = new Vector2((Screen.width - W) / 2f, (Screen.height - H) / 2f);
        var win = new Rect(_pos.x, _pos.y + (1f - _open) * Gui.Px(20f), W, H);

        Gui.Alpha = _open;
        try
        {
            DrawWindow(win);
        }
        finally
        {
            Gui.Alpha = 1f;
        }

        if (_interactive && win.Contains(_e.mousePosition) &&
            (_e.type == EventType.MouseDown || _e.type == EventType.MouseUp || _e.type == EventType.ScrollWheel))
            _e.Use();
    }

    static void DrawWindow(Rect win)
    {
        float side = Gui.Px(210f), radius = Gui.Px(16f);
        var sidebar = new Rect(win.x, win.y, side, win.height);
        var content = new Rect(win.x + side, win.y, win.width - side, win.height);

        if (_repaint)
        {
            Gui.Shadow(win, radius);
            Gui.Round(win, Bg, radius);
            Gui.Round(sidebar, SidebarBg, radius);
            Gui.Fill(new Rect(sidebar.xMax - radius, sidebar.y, radius, sidebar.height), SidebarBg);
            Gui.Fill(new Rect(sidebar.xMax, sidebar.y + Gui.Px(16f), 1f, sidebar.height - Gui.Px(32f)), Border);
            Gui.Outline(win, Border, 1f, radius);
        }

        DrawSidebar(sidebar);
        DrawContent(content);
    }

    static void DrawSidebar(Rect r)
    {
        float pad = Gui.Px(22f);

        if (_repaint)
        {
            Gui.Text(new Rect(r.x + pad, r.y + Gui.Px(24f), r.width - pad, Gui.Px(30f)), "PIT OF GOBLIN", 21, Accent,
                TextAnchor.MiddleLeft, bold: true);
            Gui.Text(new Rect(r.x + pad + Gui.Px(1f), r.y + Gui.Px(52f), r.width, Gui.Px(16f)), "M O D   M E N U", 10, TextDim,
                TextAnchor.MiddleLeft, shadow: false);
        }

        HandleDrag(new Rect(r.x, r.y, r.width, Gui.Px(80f)));

        float y = r.y + Gui.Px(96f);
        for (int i = 0; i < Pages.Length; i++)
        {
            var item = new Rect(r.x + Gui.Px(12f), y, r.width - Gui.Px(24f), Gui.Px(46f));
            bool selected = (int)_page == i;
            bool hover = _interactive && item.Contains(_e.mousePosition);
            float t = Gui.Animate("nav" + i, selected ? 1f : hover ? 0.45f : 0f);

            if (_repaint)
            {
                if (t > 0.01f) Gui.Round(item, new Color(Accent.r, Accent.g, Accent.b, 0.15f * t), Gui.Px(10f));
                if (selected)
                    Gui.Round(new Rect(item.x, item.y + Gui.Px(12f), Gui.Px(3f), item.height - Gui.Px(24f)), Accent, Gui.Px(1.5f));

                Gui.Text(new Rect(item.x + Gui.Px(12f), item.y, Gui.Px(26f), item.height), Pages[i].Icon, 16,
                    selected ? Accent : TextDim, TextAnchor.MiddleCenter, shadow: false);
                Gui.Text(new Rect(item.x + Gui.Px(48f), item.y, item.width, item.height), Pages[i].Name, 15,
                    selected ? TextMain : TextSoft, TextAnchor.MiddleLeft, bold: selected, shadow: false);
            }

            if (Clicked(item)) _page = (Page)i;
            y += Gui.Px(52f);
        }

        if (!_repaint) return;

        var status = Cheats.IsHost ? Good : Accent;
        string label = Cheats.PlayerCount <= 1 ? "● SOLO"
            : Cheats.IsHost ? $"● HOST · PLAYERS: {Cheats.PlayerCount}"
            : $"● GUEST · PLAYERS: {Cheats.PlayerCount}";
        var pill = new Rect(r.x + pad, r.yMax - Gui.Px(104f), r.width - pad * 2f, Gui.Px(28f));
        Gui.Round(pill, new Color(status.r, status.g, status.b, 0.13f), pill.height / 2f);
        Gui.Text(pill, label, 11, status, TextAnchor.MiddleCenter, bold: true, shadow: false);

        Gui.Text(new Rect(r.x, r.yMax - Gui.Px(48f), r.width, Gui.Px(16f)), $"{Settings.MenuKey.Value} – close", 11, TextDim,
            TextAnchor.MiddleCenter, shadow: false);
        Gui.Text(new Rect(r.x, r.yMax - Gui.Px(30f), r.width, Gui.Px(14f)), "v" + Plugin.Version, 10, TextFaint,
            TextAnchor.MiddleCenter, shadow: false);
    }

    static void DrawContent(Rect r)
    {
        float pad = Gui.Px(28f);
        var page = Pages[(int)_page];

        if (_repaint)
        {
            Gui.Text(new Rect(r.x + pad, r.y + Gui.Px(24f), r.width, Gui.Px(30f)), page.Name, 22, TextMain,
                TextAnchor.MiddleLeft, bold: true);
            Gui.Text(new Rect(r.x + pad, r.y + Gui.Px(52f), r.width, Gui.Px(18f)), page.Subtitle, 12, TextDim,
                TextAnchor.MiddleLeft, shadow: false);
        }

        var close = new Rect(r.xMax - Gui.Px(22f) - Gui.Px(34f), r.y + Gui.Px(22f), Gui.Px(34f), Gui.Px(34f));
        bool hoverClose = _interactive && close.Contains(_e.mousePosition);
        float tc = Gui.Animate("close", hoverClose ? 1f : 0f);
        if (_repaint)
        {
            Gui.Round(close, Color.Lerp(Btn, new Color(0.88f, 0.28f, 0.24f, 0.9f), tc), Gui.Px(10f));
            Gui.Text(close, "×", 20, TextMain, TextAnchor.MiddleCenter, shadow: false);
        }
        if (Clicked(close))
        {
            Open = false;
            return;
        }

        HandleDrag(new Rect(r.x, r.y, r.width - Gui.Px(70f), Gui.Px(80f)));

        _x = r.x + pad;
        _w = r.width - pad * 2f;
        _y = r.y + Gui.Px(90f);

        switch (_page)
        {
            case Page.Player: PlayerPage(); break;
            case Page.Combat: CombatPage(r); break;
            case Page.Visuals: VisualsPage(); break;
            case Page.World: WorldPage(); break;
            case Page.Items: ItemsPage(r); break;
        }
    }

    static void PlayerPage()
    {
        GoldCard(NextRow(Gui.Px(150f)));

        Grid(out var a, out var b, out var c, out var d);

        if (SwitchCard(a, "God mode", "enemies and traps can't hurt you", Cheats.God))
            Cheats.SetGod(!Cheats.God);
        if (SwitchCard(b, "Infinite stamina", "run and jump without getting tired", Cheats.InfiniteStamina))
            Cheats.SetStamina(!Cheats.InfiniteStamina);

        var refill = new Rect(c.xMax - Gui.Px(16f) - Gui.Px(76f), c.yMax - Gui.Px(14f) - Gui.Px(28f), Gui.Px(76f), Gui.Px(28f));
        if (Button(refill, "Refill", "refill", 12)) Cheats.RefillMagic();
        if (SwitchCard(c, "Infinite magic", "spells never use up charges", Cheats.InfiniteMagic))
            Cheats.SetMagic(!Cheats.InfiniteMagic);

        Card(d);
        if (_repaint)
            Gui.Text(new Rect(d.x + Gui.Px(18f), d.y + Gui.Px(14f), d.width, Gui.Px(22f)), "Movement speed", 15, TextSoft,
                TextAnchor.MiddleLeft, shadow: false);

        var labels = new string[Settings.SpeedMultipliers.Length];
        for (int i = 0; i < labels.Length; i++) labels[i] = Cheats.SpeedText(i);
        var seg = new Rect(d.x + Gui.Px(14f), d.yMax - Gui.Px(14f) - Gui.Px(34f), d.width - Gui.Px(28f), Gui.Px(34f));
        int speed = Segmented(seg, labels, Cheats.SpeedIndex, "speed");
        if (speed >= 0) Cheats.SetSpeed(speed);
    }

    static void GoldCard(Rect card)
    {
        Card(card);
        float inner = Gui.Px(20f);

        if (_repaint)
        {
            Gui.Text(new Rect(card.x + inner, card.y + Gui.Px(16f), card.width, Gui.Px(16f)), "GOLD", 11, Accent,
                TextAnchor.MiddleLeft, bold: true, shadow: false);

            bool hasGold = Cheats.Gold >= 0;
            Gui.Text(new Rect(card.x + inner, card.y + Gui.Px(34f), Gui.Px(200f), Gui.Px(40f)),
                hasGold ? Thousands(Cheats.Gold) : "—", 30, GoldColor, TextAnchor.MiddleLeft, bold: true);
            if (!hasGold)
                Gui.Text(new Rect(card.x + Gui.Px(56f), card.y + Gui.Px(34f), Gui.Px(200f), Gui.Px(40f)),
                    "available during a run", 11, TextDim, TextAnchor.MiddleLeft, shadow: false);
        }

        float chipW = Gui.Px(66f), chipH = Gui.Px(30f), gap = Gui.Px(6f);
        float cx = card.xMax - inner - (chipW * GoldPresets.Length + gap * (GoldPresets.Length - 1));
        int current = ParseAmount(_goldInput);
        for (int i = 0; i < GoldPresets.Length; i++)
        {
            var chip = new Rect(cx + i * (chipW + gap), card.y + Gui.Px(26f), chipW, chipH);
            if (Chip(chip, Thousands(GoldPresets[i]), current == GoldPresets[i], "goldpreset" + i))
                _goldInput = GoldPresets[i].ToString(CultureInfo.InvariantCulture);
        }

        float rowY = card.yMax - inner - Gui.Px(40f);
        if (_repaint)
            Gui.Text(new Rect(card.x + inner, rowY, Gui.Px(60f), Gui.Px(40f)), "Amount", 13, TextDim, TextAnchor.MiddleLeft, shadow: false);

        var field = new Rect(card.x + inner + Gui.Px(50f), rowY, Gui.Px(150f), Gui.Px(40f));
        _goldInput = TextField("gold", field, _goldInput, "0", digitsOnly: true, maxLength: 9);

        float bw = Gui.Px(122f);
        var add = new Rect(card.xMax - inner - bw, rowY, bw, Gui.Px(40f));
        var remove = new Rect(add.x - Gui.Px(8f) - bw, rowY, bw, Gui.Px(40f));
        int amount = ParseAmount(_goldInput);
        if (Button(remove, "− Take", "goldremove")) Cheats.ChangeGold(-amount);
        if (AccentButton(add, "+ Add", "goldadd")) Cheats.ChangeGold(amount);
    }

    static void VisualsPage()
    {
        var map = NextRow(Gui.Px(84f));
        Card(map);
        if (_repaint)
        {
            Gui.Text(new Rect(map.x + Gui.Px(18f), map.y + Gui.Px(18f), map.width, Gui.Px(22f)), "Minimap", 15, TextMain,
                TextAnchor.MiddleLeft, bold: Cheats.Map != MapMode.Off, shadow: false);
            Gui.Text(new Rect(map.x + Gui.Px(18f), map.y + Gui.Px(42f), map.width, Gui.Px(18f)), "the dungeon with enemies, treasure and the bucket", 11,
                TextDim, TextAnchor.MiddleLeft, shadow: false);
        }
        var seg = new Rect(map.xMax - Gui.Px(16f) - Gui.Px(220f), map.y + (map.height - Gui.Px(36f)) / 2f, Gui.Px(220f), Gui.Px(36f));
        int mode = Segmented(seg, new[] { "Off", "Small", "Large" }, (int)Cheats.Map, "map");
        if (mode >= 0) Cheats.SetMap((MapMode)mode);

        Grid(out var a, out var b, out var c, out var d);

        if (SwitchCard(a, "Item ESP", "outlines items in their rarity color", Cheats.ItemEsp))
            Cheats.SetItemEsp(!Cheats.ItemEsp);
        if (SwitchCard(b, "Player ESP", "teammates through walls, with names", Cheats.PlayerEsp))
            Cheats.SetPlayerEsp(!Cheats.PlayerEsp);
        if (SwitchCard(c, "Night vision", "brighter, green tint, no fog", Cheats.NightVision))
            Cheats.SetNightVision(!Cheats.NightVision);
        if (SwitchCard(d, "Compass → bucket", "arrow at the top of the screen", Cheats.Compass))
            Cheats.SetCompass(!Cheats.Compass);

        if (!_repaint) return;
        var legend = NextRow(Gui.Px(28f));
        float x = legend.x + Gui.Px(4f);
        foreach (var (name, rarity) in Rarities)
        {
            Gui.Dot(new Vector2(x + Gui.Px(5f), legend.center.y), Gui.Px(9f), Esp.RarityColor(rarity));
            Gui.Text(new Rect(x + Gui.Px(14f), legend.y, Gui.Px(90f), legend.height), name, 11, TextDim, TextAnchor.MiddleLeft, shadow: false);
            x += Gui.Px(24f) + name.Length * Gui.Px(6.5f);
        }
    }

    static readonly (string Name, InventoryItem.ItemRarity Rarity)[] Rarities =
    {
        ("Common", InventoryItem.ItemRarity.Common), ("Uncommon", InventoryItem.ItemRarity.Uncommon),
        ("Rare", InventoryItem.ItemRarity.Rare), ("Mythical", InventoryItem.ItemRarity.Mythical),
        ("Legendary", InventoryItem.ItemRarity.Legendary), ("Relic", InventoryItem.ItemRarity.Relic),
    };

    static void WorldPage()
    {
        var tp = NextRow(Gui.Px(70f));
        Card(tp);
        if (_repaint)
        {
            Gui.Text(new Rect(tp.x + Gui.Px(18f), tp.y + Gui.Px(13f), tp.width, Gui.Px(22f)), "Teleport to the bucket", 15, TextMain,
                TextAnchor.MiddleLeft, shadow: false);
            Gui.Text(new Rect(tp.x + Gui.Px(18f), tp.y + Gui.Px(36f), tp.width, Gui.Px(18f)), "puts you right inside the bucket", 11,
                TextDim, TextAnchor.MiddleLeft, shadow: false);
        }
        var tpButton = new Rect(tp.xMax - Gui.Px(16f) - Gui.Px(140f), tp.y + (tp.height - Gui.Px(38f)) / 2f, Gui.Px(140f), Gui.Px(38f));
        if (AccentButton(tpButton, "Teleport", "teleport")) Cheats.TeleportToBucket();

        TimerCard(NextRow(Gui.Px(116f)));

        Pair(out var a, out var b);
        if (SwitchCard(a, "Return timer", "time until the bucket leaves", Cheats.Timer))
            Cheats.SetTimer(!Cheats.Timer);
        if (SwitchCard(b, "Cheat list", "active cheats in the corner", Cheats.Overlay))
            Cheats.Overlay = !Cheats.Overlay;
    }

    static void TimerCard(Rect card)
    {
        Card(card);
        float inner = Gui.Px(20f);
        float step = Mathf.Max(1f, Settings.TimerStep.Value);

        if (_repaint)
        {
            Gui.Text(new Rect(card.x + inner, card.y + Gui.Px(16f), card.width, Gui.Px(16f)), "ROUND TIMER", 11, Accent,
                TextAnchor.MiddleLeft, bold: true, shadow: false);

            string time = RoundTimer.Active ? RoundTimer.Format(RoundTimer.Remaining) : "—";
            Gui.Text(new Rect(card.x + inner, card.y + Gui.Px(34f), Gui.Px(160f), Gui.Px(40f)), time, 30,
                RoundTimer.Frozen ? new Color(0.55f, 0.85f, 1f) : TextMain, TextAnchor.MiddleLeft, bold: true);

            string note = !RoundTimer.Active ? "no round is running"
                : !Cheats.IsHost ? "only the host can change it"
                : RoundTimer.Frozen ? "frozen, the bucket waits for you"
                : "until the bucket leaves";
            Gui.Text(new Rect(card.x + inner, card.y + Gui.Px(76f), card.width, Gui.Px(18f)), note, 11, TextDim,
                TextAnchor.MiddleLeft, shadow: false);
        }

        float bw = Gui.Px(92f), bh = Gui.Px(38f), gap = Gui.Px(8f);
        var freeze = new Rect(card.xMax - inner - bw, card.y + (card.height - bh) / 2f, bw, bh);
        var plus = new Rect(freeze.x - gap - bw, freeze.y, bw, bh);
        var minus = new Rect(plus.x - gap - bw, freeze.y, bw, bh);

        string stepText = RoundTimer.StepText(step);
        if (Button(minus, "− " + stepText, "timerminus", 13)) RoundTimer.Shift(-step);
        if (Button(plus, "+ " + stepText, "timerplus", 13)) RoundTimer.Shift(step);
        if (TintButton(freeze, RoundTimer.Frozen ? "Unfreeze" : "Freeze", "timerfreeze", new Color(0.55f, 0.85f, 1f), _interactive))
            RoundTimer.SetFrozen(!RoundTimer.Frozen);
    }

    static void ItemsPage(Rect content)
    {
        var items = ItemSpawner.Items;

        var top = NextRow(Gui.Px(40f));
        var qty = new Rect(top.xMax - Gui.Px(170f), top.y, Gui.Px(170f), top.height);
        string search = TextField("search", new Rect(top.x, top.y, top.width - qty.width - Gui.Px(10f), top.height),
            _search, "Search items…", digitsOnly: false, maxLength: 30);
        if (search != _search)
        {
            _search = search;
            _scroll = 0f;
        }
        int q = Segmented(qty, new[] { "x1", "x5", "x10" }, _quantity, "qty");
        if (q >= 0) _quantity = q;

        float cx = _x, cy = _y, chipH = Gui.Px(28f), gap = Gui.Px(6f);
        for (int i = 0; i < Categories.Length; i++)
        {
            string text = items.Count > 0 ? $"{Categories[i].Name}  {ItemSpawner.Count(Categories[i].Type)}" : Categories[i].Name;
            float cw = Gui.Px(24f) + text.Length * Gui.Px(7f);
            if (cx + cw > _x + _w)
            {
                cx = _x;
                cy += chipH + gap;
            }
            if (Chip(new Rect(cx, cy, cw, chipH), text, _category == i, "cat" + i))
            {
                _category = i;
                _scroll = 0f;
            }
            cx += cw + gap;
        }
        _y = cy + chipH + Gui.Px(12f);

        ItemSpawner.Filter(Filtered, Categories[_category].Type, _search.Trim().ToLowerInvariant(), _sort);

        var bar = NextRow(Gui.Px(30f));
        var sort = new Rect(bar.xMax - Gui.Px(210f), bar.y, Gui.Px(210f), bar.height);
        int s = Segmented(sort, new[] { "A–Z", "Value" }, (int)_sort, "sort");
        if (s >= 0)
        {
            _sort = (ItemSort)s;
            _scroll = 0f;
        }
        if (_repaint)
        {
            Gui.Text(new Rect(bar.x + Gui.Px(4f), bar.y, bar.width - sort.width, bar.height),
                $"{Filtered.Count} items · click one to spawn it", 12, TextDim, TextAnchor.MiddleLeft, shadow: false);
        }

        DrawItemList(new Rect(_x, _y, _w, content.yMax - Gui.Px(24f) - _y), items.Count);
    }

    static void DrawItemList(Rect list, int total)
    {
        float rowH = Gui.Px(44f);
        if (!BeginList(list, Filtered.Count, rowH, ref _scroll, out int first, out int last, out bool inList))
        {
            if (_repaint)
                Gui.Text(list, total == 0 ? "The list shows up during a run" : "Nothing matches", 13, TextDim,
                    TextAnchor.MiddleCenter, shadow: false);
            return;
        }

        float rowW = list.width - Gui.Px(18f);
        for (int i = first; i < last; i++)
        {
            var it = Filtered[i];
            var r = ListRow(i, rowH, rowW, _scroll);
            bool hover = inList && r.Contains(_e.mousePosition);

            if (_repaint)
            {
                if (hover) Gui.Round(r, new Color(Accent.r, Accent.g, Accent.b, 0.16f), Gui.Px(8f));

                var iconBox = new Rect(r.x + Gui.Px(6f), r.y + (r.height - Gui.Px(32f)) / 2f, Gui.Px(32f), Gui.Px(32f));
                Gui.Round(iconBox, new Color(1f, 1f, 1f, 0.05f), Gui.Px(7f));
                Gui.Icon(new Rect(iconBox.x + Gui.Px(3f), iconBox.y + Gui.Px(3f), iconBox.width - Gui.Px(6f), iconBox.height - Gui.Px(6f)),
                    it.IconTexture, it.IconRect);

                Gui.Text(new Rect(iconBox.xMax + Gui.Px(12f), r.y, r.width - Gui.Px(180f), r.height), it.Name, 14,
                    hover ? TextMain : Esp.RarityColor(it.Rarity), TextAnchor.MiddleLeft, shadow: false);

                if (hover)
                {
                    Gui.Text(new Rect(r.x, r.y, r.width - Gui.Px(14f), r.height), "+ spawn", 12, Accent,
                        TextAnchor.MiddleRight, bold: true, shadow: false);
                }
                else
                {
                    string value = it.Gold > 0 ? $"{Thousands(it.Gold)} gold" : it.Food > 0 ? $"food {it.Food}" : "";
                    if (value.Length > 0)
                        Gui.Text(new Rect(r.x, r.y, r.width - Gui.Px(14f), r.height), value, 12,
                            it.Gold > 0 ? GoldColor : FoodColor, TextAnchor.MiddleRight, shadow: false);
                }
            }

            if (hover && _e.type == EventType.MouseDown && _e.button == 0)
            {
                _e.Use();
                ItemSpawner.Spawn(it, Quantities[_quantity]);
            }
        }

        EndList(list, Filtered.Count, rowH, _scroll);
    }

    static void CombatPage(Rect content)
    {
        Pair(out var a, out var b);
        if (SwitchCard(a, "One-hit kill", "every hit you land on an enemy kills it", Cheats.OneHitKill))
            Cheats.SetOneHitKill(!Cheats.OneHitKill);
        if (SwitchCard(b, "Watch alert", "shows which dead teammate spectates you", Cheats.WatchAlert))
            Cheats.SetWatchAlert(!Cheats.WatchAlert);

        var bar = NextRow(Gui.Px(36f));
        var tabs = new Rect(bar.x, bar.y, Gui.Px(250f), bar.height);
        var action = new Rect(bar.xMax - Gui.Px(110f), bar.y, Gui.Px(110f), bar.height);

        int tab = Segmented(tabs, new[] { $"Players  {Targets.Players.Count}", $"Enemies  {Targets.Enemies.Count}" }, _combatTab, "combat");
        if (tab >= 0)
        {
            _combatTab = tab;
            _combatScroll = 0f;
        }

        if (_combatTab == 0)
        {
            var bring = new Rect(action.x - Gui.Px(8f) - action.width, action.y, action.width, action.height);
            if (TintButton(bring, "Bring all", "bringall", TextSoft, _interactive)) Combat.BringAll();
            if (TintButton(action, "Revive all", "reviveall", Good, _interactive)) Combat.ReviveAll();
        }
        else if (TintButton(action, "Kill all", "killall", Danger, _interactive))
        {
            Combat.KillAllEnemies();
        }

        var list = new Rect(_x, _y, _w, content.yMax - Gui.Px(24f) - _y);
        if (_combatTab == 0) PlayerList(list);
        else EnemyList(list);
    }

    static void PlayerList(Rect list)
    {
        var players = Targets.Players;
        float rowH = Gui.Px(56f);
        if (!BeginList(list, players.Count, rowH, ref _combatScroll, out int first, out int last, out bool inList))
        {
            if (_repaint)
                Gui.Text(list, Cheats.InSession ? "No players found" : "Join a game to see the players", 13, TextDim,
                    TextAnchor.MiddleCenter, shadow: false);
            return;
        }

        float rowW = list.width - Gui.Px(18f);
        for (int i = first; i < last; i++)
        {
            var p = players[i];
            var r = ListRow(i, rowH, rowW, _combatScroll);
            bool dead = p.IsDead;
            bool other = !p.IsLocal && !dead;

            var buttons = ActionButtons(r, other ? 3 : 1);

            if (_repaint)
            {
                string title = p.IsLocal ? $"{p.Name}  (you)" : p.Name;
                var details = new List<string>();
                if (dead)
                {
                    details.Add("dead");
                }
                else
                {
                    string hp = Targets.Health(p.Character);
                    if (hp != null) details.Add(hp);
                    if (!p.IsLocal && p.Distance >= 0f) details.Add($"{p.Distance:0} m");
                }
                DrawTargetRow(r, buttons[buttons.Length - 1].x, title, string.Join(" · ", details), dead ? Danger : Good, p.IsLocal,
                    dead ? Watch.Describe(p.Persistent.Pointer) : null);
            }

            if (dead)
            {
                if (TintButton(buttons[0], "Revive", "revive" + i, Good, inList)) Combat.Revive(p);
            }
            else if (other)
            {
                if (TintButton(buttons[0], "Kill", "killp" + i, Danger, inList)) Combat.Kill(p.Character, p.Name);
                if (TintButton(buttons[1], "Bring", "bring" + i, TextSoft, inList)) Combat.Bring(p);
                if (TintButton(buttons[2], "Go to", "goto" + i, TextSoft, inList)) Combat.TeleportTo(p);
            }
        }

        EndList(list, players.Count, rowH, _combatScroll);
    }

    static void EnemyList(Rect list)
    {
        var enemies = Targets.Enemies;
        float rowH = Gui.Px(56f);
        if (!BeginList(list, enemies.Count, rowH, ref _combatScroll, out int first, out int last, out bool inList))
        {
            if (_repaint)
                Gui.Text(list, Cheats.InSession ? "No enemies around" : "Join a game to see enemies", 13, TextDim,
                    TextAnchor.MiddleCenter, shadow: false);
            return;
        }

        float rowW = list.width - Gui.Px(18f);
        for (int i = first; i < last; i++)
        {
            var en = enemies[i];
            var r = ListRow(i, rowH, rowW, _combatScroll);

            var kill = ActionButtons(r, 1)[0];

            if (_repaint)
            {
                var details = new List<string>();
                string hp = Targets.Health(en.Entity);
                if (hp != null) details.Add(hp);
                details.Add($"{en.Distance:0} m");
                DrawTargetRow(r, kill.x, en.Name, string.Join(" · ", details), Warn, false, null);
            }

            if (TintButton(kill, "Kill", "kille" + i, Danger, inList)) Combat.Kill(en.Entity, en.Name);
        }

        EndList(list, enemies.Count, rowH, _combatScroll);
    }

    static Rect[] ActionButtons(Rect row, int count)
    {
        float w = Gui.Px(70f), h = Gui.Px(30f), gap = Gui.Px(6f);
        var rects = new Rect[count];
        float x = row.xMax - Gui.Px(10f) - w;
        for (int i = 0; i < count; i++)
        {
            rects[i] = new Rect(x, row.y + (row.height - h) / 2f, w, h);
            x -= w + gap;
        }
        return rects;
    }

    static void DrawTargetRow(Rect r, float textRight, string title, string details, Color dot, bool bold, (string Text, Color Color)? tag)
    {
        Gui.Round(r, new Color(1f, 1f, 1f, 0.03f), Gui.Px(8f));
        Gui.Dot(new Vector2(r.x + Gui.Px(16f), r.y + Gui.Px(18f)), Gui.Px(9f), dot);

        float x = r.x + Gui.Px(30f), width = textRight - x - Gui.Px(10f);
        Gui.Text(new Rect(x, r.y + Gui.Px(5f), width, Gui.Px(24f)), title, 14, TextMain, TextAnchor.MiddleLeft, bold: bold, shadow: false);
        Gui.Text(new Rect(x, r.y + Gui.Px(28f), width, Gui.Px(18f)), details, 11, TextDim, TextAnchor.MiddleLeft, shadow: false);

        if (tag == null) return;
        Gui.Text(new Rect(x, r.y + Gui.Px(28f), width, Gui.Px(18f)), tag.Value.Text, 11, tag.Value.Color, TextAnchor.MiddleRight,
            bold: true, shadow: false);
    }

    static bool BeginList(Rect list, int count, float rowH, ref float scroll, out int first, out int last, out bool inList)
    {
        if (_repaint) Gui.Round(list, Well, Gui.Px(12f));

        inList = _interactive && list.Contains(_e.mousePosition);
        first = last = 0;
        if (count == 0) return false;

        float maxScroll = Mathf.Max(0f, count * rowH + Gui.Px(8f) - list.height);
        if (inList && _e.type == EventType.ScrollWheel)
        {
            scroll += _e.delta.y * Gui.Px(26f);
            _e.Use();
        }
        scroll = Mathf.Clamp(scroll, 0f, maxScroll);

        GUI.BeginGroup(list, GUIContent.none, GUIStyle.none);
        first = Mathf.Max(0, (int)(scroll / rowH));
        last = Mathf.Min(count, first + (int)(list.height / rowH) + 2);
        return true;
    }

    static void EndList(Rect list, int count, float rowH, float scroll)
    {
        float content = count * rowH + Gui.Px(8f);
        float maxScroll = Mathf.Max(0f, content - list.height);
        if (maxScroll > 0f && _repaint)
        {
            float track = list.height - Gui.Px(12f);
            float thumb = Mathf.Max(Gui.Px(28f), track * list.height / content);
            float pos = Gui.Px(6f) + scroll / maxScroll * (track - thumb);
            Gui.Round(new Rect(list.width - Gui.Px(9f), pos, Gui.Px(4f), thumb), new Color(1f, 1f, 1f, 0.22f), Gui.Px(2f));
        }
        GUI.EndGroup();
    }

    static Rect ListRow(int index, float rowH, float width, float scroll) =>
        new(Gui.Px(5f), Gui.Px(4f) + index * rowH - scroll, width, rowH - Gui.Px(4f));

    static Rect NextRow(float height)
    {
        var r = new Rect(_x, _y, _w, height);
        _y += height + Gui.Px(12f);
        return r;
    }

    static void Pair(out Rect a, out Rect b)
    {
        float gap = Gui.Px(12f);
        float w = (_w - gap) / 2f;
        float h = Gui.Px(88f);
        a = new Rect(_x, _y, w, h);
        b = new Rect(_x + w + gap, _y, w, h);
        _y += h + gap;
    }

    static void Grid(out Rect a, out Rect b, out Rect c, out Rect d)
    {
        float gap = Gui.Px(12f);
        float w = (_w - gap) / 2f;
        float h = Gui.Px(88f);
        a = new Rect(_x, _y, w, h);
        b = new Rect(_x + w + gap, _y, w, h);
        c = new Rect(_x, _y + h + gap, w, h);
        d = new Rect(_x + w + gap, _y + h + gap, w, h);
        _y += (h + gap) * 2f;
    }

    static void Card(Rect r)
    {
        if (!_repaint) return;
        Gui.Round(r, CardBg, Gui.Px(12f));
        Gui.Outline(r, Border, 1f, Gui.Px(12f));
    }

    static bool Clicked(Rect r)
    {
        if (!_interactive || _e.type != EventType.MouseDown || _e.button != 0 || !r.Contains(_e.mousePosition))
            return false;
        _e.Use();
        return true;
    }

    static bool Button(Rect r, string text, string id, int size = 14)
    {
        bool hover = _interactive && r.Contains(_e.mousePosition);
        float t = Gui.Animate("btn" + id, hover ? 1f : 0f);
        if (_repaint)
        {
            Gui.Round(r, Color.Lerp(Btn, BtnHover, t), Gui.Px(9f));
            Gui.Text(r, text, size, TextMain, TextAnchor.MiddleCenter, bold: true, shadow: false);
        }
        return Clicked(r);
    }

    static bool TintButton(Rect r, string text, string id, Color tint, bool active)
    {
        bool hover = active && r.Contains(_e.mousePosition);
        float t = Gui.Animate("tint" + id, hover ? 1f : 0f);
        if (_repaint)
        {
            Gui.Round(r, new Color(tint.r, tint.g, tint.b, 0.14f + 0.2f * t), Gui.Px(8f));
            Gui.Text(r, text, 12, Color.Lerp(tint, Color.white, 0.3f + 0.5f * t), TextAnchor.MiddleCenter, bold: true, shadow: false);
        }
        return active && Clicked(r);
    }

    static bool AccentButton(Rect r, string text, string id)
    {
        bool hover = _interactive && r.Contains(_e.mousePosition);
        float t = Gui.Animate("accent" + id, hover ? 1f : 0f);
        if (_repaint)
        {
            Gui.Round(r, Color.Lerp(Accent, new Color(1f, 0.8f, 0.4f), t * 0.6f), Gui.Px(10f));
            Gui.Text(r, text, 14, TextOnAccent, TextAnchor.MiddleCenter, bold: true, shadow: false);
        }
        return Clicked(r);
    }

    static bool Chip(Rect r, string text, bool selected, string id)
    {
        bool hover = _interactive && r.Contains(_e.mousePosition);
        float t = Gui.Animate("chip" + id, selected ? 1f : 0f);
        if (_repaint)
        {
            var idle = hover ? BtnHover : Btn;
            Gui.Round(r, Color.Lerp(idle, Accent, t), r.height / 2f);
            Gui.Text(r, text, 12, Color.Lerp(TextSoft, TextOnAccent, t), TextAnchor.MiddleCenter, bold: selected, shadow: false);
        }
        return Clicked(r);
    }

    static int Segmented(Rect r, string[] labels, int selected, string id)
    {
        float gap = Gui.Px(4f);
        float w = (r.width - gap * (labels.Length + 1)) / labels.Length;
        float pos = Gui.Animate("seg" + id, selected, 18f);

        if (_repaint)
        {
            Gui.Round(r, Well, Gui.Px(10f));
            var pill = new Rect(r.x + gap + pos * (w + gap), r.y + gap, w, r.height - gap * 2f);
            Gui.Round(pill, Accent, Gui.Px(7f));
        }

        int clicked = -1;
        for (int i = 0; i < labels.Length; i++)
        {
            var b = new Rect(r.x + gap + i * (w + gap), r.y + gap, w, r.height - gap * 2f);
            if (_repaint)
            {
                float near = Mathf.Clamp01(1f - Mathf.Abs(pos - i));
                bool hover = _interactive && b.Contains(_e.mousePosition) && i != selected;
                if (hover) Gui.Round(b, new Color(1f, 1f, 1f, 0.06f), Gui.Px(7f));
                Gui.Text(b, labels[i], 13, Color.Lerp(TextSoft, TextOnAccent, near), TextAnchor.MiddleCenter, bold: i == selected, shadow: false);
            }
            if (Clicked(b)) clicked = i;
        }
        return clicked;
    }

    static bool SwitchCard(Rect r, string title, string description, bool on)
    {
        bool hover = _interactive && r.Contains(_e.mousePosition);
        float th = Gui.Animate("hover" + title, hover ? 1f : 0f);
        float ts = Gui.Animate("switch" + title, on ? 1f : 0f, 18f);

        if (_repaint)
        {
            float radius = Gui.Px(12f);
            Gui.Round(r, Color.Lerp(CardBg, CardBgHover, th), radius);
            Gui.Outline(r, Color.Lerp(Border, new Color(Accent.r, Accent.g, Accent.b, 0.45f), ts), 1f, radius);

            Gui.Text(new Rect(r.x + Gui.Px(18f), r.y + Gui.Px(16f), r.width - Gui.Px(90f), Gui.Px(22f)), title, 15,
                Color.Lerp(TextSoft, TextMain, ts), TextAnchor.MiddleLeft, bold: on, shadow: false);
            Gui.Text(new Rect(r.x + Gui.Px(18f), r.y + Gui.Px(42f), r.width - Gui.Px(36f), Gui.Px(18f)), description, 11,
                TextDim, TextAnchor.MiddleLeft, shadow: false);

            var sw = new Rect(r.xMax - Gui.Px(16f) - Gui.Px(44f), r.y + Gui.Px(16f), Gui.Px(44f), Gui.Px(24f));
            Gui.Switch(sw, ts, Accent, hover);
        }
        return Clicked(r);
    }

    static string TextField(string id, Rect field, string value, string placeholder, bool digitsOnly, int maxLength)
    {
        if (_interactive && _e.type == EventType.MouseDown && _e.button == 0 && field.Contains(_e.mousePosition))
        {
            _focus = id;
            _e.Use();
        }

        bool focused = _focus == id;
        if (focused && _interactive && _e.type == EventType.KeyDown)
            value = Type(value, digitsOnly, maxLength);

        if (!_repaint) return value;

        float t = Gui.Animate("field" + id, focused ? 1f : 0f);
        Gui.Round(field, Color.Lerp(Well, new Color(0f, 0f, 0f, 0.4f), t), Gui.Px(10f));
        Gui.Outline(field, Color.Lerp(Border, Accent, t), Gui.Px(1.5f), Gui.Px(10f));

        bool empty = value.Length == 0;
        string shown = empty && !focused ? placeholder
            : digitsOnly && !focused ? Thousands(ParseAmount(value))
            : value;
        if (focused && Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f) shown += "|";
        Gui.Text(new Rect(field.x + Gui.Px(14f), field.y, field.width - Gui.Px(28f), field.height), shown, 14,
            empty && !focused ? TextDim : TextMain, TextAnchor.MiddleLeft, shadow: false);
        return value;
    }

    static string Type(string value, bool digitsOnly, int maxLength)
    {
        var key = _e.keyCode;
        if (key == KeyCode.Backspace)
        {
            if (value.Length > 0) value = value.Substring(0, value.Length - 1);
        }
        else if (key == KeyCode.Escape || key == KeyCode.Return || key == KeyCode.KeypadEnter)
        {
            _focus = null;
        }
        else
        {
            char c = _e.character;
            if (c == '\0' || char.IsControl(c) || value.Length >= maxLength) return value;
            if (digitsOnly && !char.IsDigit(c)) return value;
            value += c;
        }
        _e.Use();
        return value;
    }

    static void HandleDrag(Rect area)
    {
        if (!_interactive) return;

        var mouse = _e.mousePosition;
        switch (_e.type)
        {
            case EventType.MouseDown when _e.button == 0 && area.Contains(mouse):
                _dragging = true;
                _dragOffset = mouse - _pos;
                _e.Use();
                break;
            case EventType.MouseDrag when _dragging:
                _pos = mouse - _dragOffset;
                _pos.x = Mathf.Clamp(_pos.x, -Gui.Px(600f), Screen.width - Gui.Px(160f));
                _pos.y = Mathf.Clamp(_pos.y, 0f, Screen.height - Gui.Px(60f));
                _e.Use();
                break;
            case EventType.MouseUp when _dragging:
                _dragging = false;
                _e.Use();
                break;
        }
    }

    static int ParseAmount(string text) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int v) ? v : 0;

    static string Thousands(int value) => value.ToString("#,0", CultureInfo.InvariantCulture);
}
