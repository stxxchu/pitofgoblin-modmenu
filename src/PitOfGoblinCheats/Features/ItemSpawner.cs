using System;
using System.Collections.Generic;
using System.Linq;
using Game.Players;
using Unity.Netcode;
using UnityEngine;

namespace PitOfGoblinCheats;

internal class SpawnableItem
{
    public string Name;
    public string RawName;
    public string Term;
    public string SearchKey;
    public NetworkObject Prefab;
    public ItemType Type;
    public InventoryItem.ItemRarity Rarity;
    public int Gold;
    public int Food;
    public Texture2D IconTexture;
    public Rect IconRect;
}

internal enum ItemSort { Name, Value }

internal static class ItemSpawner
{
    static readonly List<SpawnableItem> Catalog = new();
    static readonly System.Random Rng = new();

    public static IReadOnlyList<SpawnableItem> Items
    {
        get
        {
            if (Catalog.Count == 0) Build();
            return Catalog;
        }
    }

    public static int Count(ItemType? category) =>
        category.HasValue ? Catalog.Count(it => (it.Type & category.Value) != 0) : Catalog.Count;

    static void Build()
    {
        var prefabs = NetworkManager.Singleton?.NetworkConfig?.Prefabs?.m_Prefabs;
        if (prefabs == null) return;

        int skipped = 0;
        for (int i = 0; i < prefabs.Count; i++)
        {
            var go = prefabs[i]?.Prefab;
            if (go == null) continue;

            var item = go.GetComponent<InventoryItem>();
            var net = go.GetComponent<NetworkObject>();
            if (item == null || net == null) continue;

            string term = item.GetLocalizedNameTerm();
            var icon = item.ItemIcon;
            if (string.IsNullOrEmpty(term) && icon == null)
            {
                skipped++;
                continue;
            }

            var entry = new SpawnableItem
            {
                RawName = go.name,
                Term = term,
                Prefab = net,
                Type = item.ItemType,
                Rarity = item.Rarity,
                Gold = item.GoldValue,
                Food = item.FoodValue,
            };
            ReadIcon(entry, icon);
            Catalog.Add(entry);
        }

        Localize();
        Plugin.L.LogInfo($"Spawner: {Catalog.Count} items, skipped {skipped} internal ones");
    }

    static void ReadIcon(SpawnableItem entry, Sprite icon)
    {
        if (icon == null) return;
        try
        {
            entry.IconTexture = icon.texture;
            entry.IconRect = icon.textureRect;
        }
        catch
        {
            entry.IconRect = icon.rect;
        }
    }

    static void Localize()
    {
        foreach (var it in Catalog)
            it.Name = Names.Translate(it.Term) ?? Names.Pretty(it.RawName);

        foreach (var group in Catalog.GroupBy(it => it.Name).Where(g => g.Count() > 1))
        {
            int n = 1;
            foreach (var it in group.OrderBy(it => it.Gold))
            {
                if (n > 1) it.Name += $" ({n})";
                n++;
            }
        }

        foreach (var it in Catalog)
            it.SearchKey = $"{it.Name} {it.RawName} {it.Term}".ToLowerInvariant();

        Catalog.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
    }

    public static void Filter(List<SpawnableItem> into, ItemType? category, string search, ItemSort sort)
    {
        into.Clear();
        foreach (var it in Items)
        {
            if (category.HasValue && (it.Type & category.Value) == 0) continue;
            if (!string.IsNullOrEmpty(search) && !it.SearchKey.Contains(search)) continue;
            into.Add(it);
        }

        if (sort == ItemSort.Value)
            into.Sort((a, b) => b.Gold != a.Gold ? b.Gold.CompareTo(a.Gold) : b.Food.CompareTo(a.Food));
    }

    public static void Spawn(SpawnableItem it, int count)
    {
        var nm = NetworkManager.Singleton;
        var cam = Camera.main;
        if (nm == null || cam == null || Cheats.Player == null)
        {
            Notify.Show("Spawning only works during a run");
            return;
        }

        if (!nm.IsServer)
        {
            SpawnAsGuest(it, count);
            return;
        }

        var forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();

        var origin = cam.transform.position + forward * Settings.SpawnDistance.Value;
        for (int i = 0; i < count; i++)
        {
            var spread = count > 1
                ? new Vector3((float)Rng.NextDouble() - 0.5f, (float)Rng.NextDouble() * 0.3f, (float)Rng.NextDouble() - 0.5f) * 0.8f
                : Vector3.zero;
            nm.SpawnManager.InstantiateAndSpawn(it.Prefab, 0, true, false, false, origin + spread, Quaternion.identity);
        }

        Cheats.UsedThisSession = true;
        Notify.Show(count > 1 ? $"Spawned {it.Name} x{count}" : $"Spawned {it.Name}");
    }

    static void SpawnAsGuest(SpawnableItem it, int count)
    {
        var local = PersistentPlayer.GetLocalPlayer();
        if (local == null)
        {
            Notify.Show("Spawning only works during a run");
            return;
        }

        for (int i = 0; i < count; i++)
            local.SpawnObjectRpc(it.RawName);

        Cheats.UsedThisSession = true;
        Notify.Show(count > 1 ? $"Asked the host for {it.Name} x{count}" : $"Asked the host for {it.Name}");
    }
}
