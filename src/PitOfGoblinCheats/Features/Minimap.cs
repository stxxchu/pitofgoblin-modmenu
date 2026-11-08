using System;
using System.Collections.Generic;
using Game.Gameplay;
using Il2CppInterop.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PitOfGoblinCheats;

internal static class Minimap
{
    static readonly Color EnemyColor = new(1f, 0.25f, 0.2f);
    static readonly Color TreasureColor = new(1f, 0.85f, 0.2f);
    static readonly Color FoodColor = new(0.4f, 1f, 0.4f);
    static readonly Color BucketColor = new(0.3f, 0.75f, 1f);
    static readonly Color PlayerColor = Color.white;

    static readonly List<Transform> Enemies = new();
    static readonly List<Transform> Treasure = new();
    static readonly List<Transform> Food = new();

    static Texture2D _tex, _converted;
    static IntPtr _sourcePtr, _requestedFor;
    static int _requests;
    static float _nextRequest, _retryAt;
    static string _status;
    static Camera _mapCam;
    static float _nextScan;

    public static Transform Bucket { get; private set; }

    public static void Tick()
    {
        if (Time.unscaledTime < _nextScan) return;
        _nextScan = Time.unscaledTime + 0.5f;

        var bucket = BucketSpawner.Bucket;
        Bucket = bucket != null ? bucket.transform : null;

        if (Cheats.Map == MapMode.Off) return;

        var gm = GenerateMap.Instance;
        _mapCam = gm != null ? gm.ScreenshotCamera : null;

        Texture2D source = null;
        if (gm == null)
        {
            Status("no GenerateMap in this scene");
        }
        else
        {
            source = gm.GetMapTexture;
            if (source == null && gm.GetMap != null) source = gm.GetMap.texture;
            if (source == null) RequestScreenshot(gm);
            else Status($"dungeon image {source.width}x{source.height}");
        }
        UpdateTexture(source, gm);

        ScanMarkers();
    }

    static void RequestScreenshot(GenerateMap gm)
    {
        if (gm.Pointer != _requestedFor)
        {
            _requestedFor = gm.Pointer;
            _requests = 0;
        }

        var round = GameMode_Round.Instance;
        if (round == null || !round.m_gameStarted)
        {
            Status("waiting for the round to start");
            return;
        }
        if (_requests >= 3 || Time.unscaledTime < _nextRequest || gm.m_takingScreenshot) return;

        _requests++;
        _nextRequest = Time.unscaledTime + 8f;
        Status($"the game has no dungeon image, taking one (try {_requests})");
        gm.TakeMapScreenshot();
    }

    static void Status(string text)
    {
        if (text == _status) return;
        _status = text;
        Plugin.L.LogInfo("Map: " + text);
    }

    static void UpdateTexture(Texture2D source, GenerateMap gm)
    {
        if (source == null)
        {
            _tex = null;
            return;
        }
        if (source.Pointer == _sourcePtr && (_tex != null || Time.unscaledTime < _retryAt)) return;

        _sourcePtr = source.Pointer;
        if (_converted != null) Object.Destroy(_converted);
        _converted = null;

        bool blank = false;
        try
        {
            _converted = MapTexture.Build(source, out blank);
        }
        catch (Exception e)
        {
            Plugin.L.LogWarning($"Could not convert the map: {e.Message}");
        }

        if (blank)
        {
            _tex = null;
            _retryAt = Time.unscaledTime + (_requests >= 3 ? 15f : 3f);
            Status("the dungeon image came out empty, retaking it");
            RequestScreenshot(gm);
            return;
        }
        _tex = _converted != null ? _converted : source;
    }

    static void ScanMarkers()
    {
        Enemies.Clear();
        Treasure.Clear();
        Food.Clear();

        if (Settings.MinimapEnemies.Value)
        {
            foreach (var obj in Object.FindObjectsByType(Il2CppType.Of<EnemyHealth>(), FindObjectsSortMode.None))
            {
                var enemy = obj.TryCast<EnemyHealth>();
                if (enemy == null || enemy.Health <= 0f) continue;
                var ent = enemy.m_entity;
                if (ent != null && ent.IsDead) continue;
                Enemies.Add(enemy.transform);
            }
        }

        if (Settings.MinimapLoot.Value)
        {
            foreach (var obj in Object.FindObjectsByType(Il2CppType.Of<InventoryItem>(), FindObjectsSortMode.None))
            {
                var item = obj.TryCast<InventoryItem>();
                if (item == null || item.IsInInventory) continue;

                var type = item.MapIconType;
                if (type == MapIconType.Treasure) Treasure.Add(item.transform);
                else if (type == MapIconType.Food) Food.Add(item.transform);
            }
        }
    }

    public static void Draw()
    {
        if (Cheats.Map == MapMode.Off) return;

        bool large = Cheats.Map == MapMode.Large;
        float margin = Gui.Px(16f);
        Rect box;
        if (large)
        {
            float size = Mathf.Min(Screen.width, Screen.height) * 0.8f;
            box = new Rect((Screen.width - size) / 2f, (Screen.height - size) / 2f, size, size);
        }
        else
        {
            float size = Gui.Px(Settings.MinimapSize.Value);
            box = new Rect(Screen.width - size - margin, margin, size, size);
        }

        float radius = Gui.Px(12f);
        Gui.Round(new Rect(box.x - 3f, box.y - 3f, box.width + 6f, box.height + 6f), new Color(0f, 0f, 0f, 0.75f), radius + 3f);
        Gui.Round(box, new Color(0.05f, 0.05f, 0.06f, 0.9f), radius);

        if (_tex == null || _mapCam == null)
        {
            Gui.Text(box, "The map shows up once you are in the dungeon", 14, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter);
            return;
        }

        Vector3 playerPos = PlayerPosition();
        Vector2 playerUv = WorldToUv(playerPos);

        float aspect = (float)_tex.width / _tex.height;
        float zoom = large ? 1f : Mathf.Max(1f, Settings.MinimapZoom.Value);
        float w = (aspect >= 1f ? box.width : box.height * aspect) * zoom;
        float h = (aspect >= 1f ? box.width / aspect : box.height) * zoom;

        float ox = large ? (box.width - w) / 2f : box.width / 2f - playerUv.x * w;
        float oy = large ? (box.height - h) / 2f : box.height / 2f - (1f - playerUv.y) * h;
        var img = new Rect(ox, oy, w, h);

        GUI.BeginGroup(box, GUIContent.none, GUIStyle.none);

        GUI.DrawTexture(img, _tex);

        float dot = Gui.Px(large ? 9f : 7f);
        foreach (var t in Food) Marker(t, img, dot, FoodColor);
        foreach (var t in Treasure) Marker(t, img, dot, TreasureColor);
        foreach (var t in Enemies) Marker(t, img, dot * 1.2f, EnemyColor);

        if (Bucket != null)
            Gui.Square(UvToPoint(WorldToUv(Bucket.position), img), Gui.Px(large ? 12f : 10f), BucketColor);

        GUI.EndGroup();

        var p = UvToPoint(playerUv, img) + box.position;
        if (box.Contains(p))
            Gui.Arrow(p, Gui.Px(large ? 22f : 18f), PlayerHeading(), PlayerColor);

        var legend = new Rect(box.x, box.yMax + Gui.Px(4f), box.width, Gui.Px(20f));
        Gui.Text(legend, "■ bucket   ● enemy   ● treasure   ● food", 12, new Color(1f, 1f, 1f, 0.75f), TextAnchor.UpperCenter);
    }

    static void Marker(Transform t, Rect img, float size, Color c)
    {
        if (t == null) return;
        Gui.Dot(UvToPoint(WorldToUv(t.position), img), size, c);
    }

    static Vector2 WorldToUv(Vector3 world)
    {
        var local = _mapCam.transform.InverseTransformPoint(world);
        float halfH = _mapCam.orthographicSize;
        float halfW = halfH * _tex.width / _tex.height;
        return new Vector2(0.5f + local.x / (2f * halfW), 0.5f + local.y / (2f * halfH));
    }

    static Vector2 UvToPoint(Vector2 uv, Rect img) =>
        new(img.x + uv.x * img.width, img.y + (1f - uv.y) * img.height);

    static Vector3 PlayerPosition()
    {
        if (Cheats.Player != null) return Cheats.Player.transform.position;
        var cam = Camera.main;
        return cam != null ? cam.transform.position : Vector3.zero;
    }

    static float PlayerHeading()
    {
        var cam = Camera.main;
        if (cam == null) return 0f;
        var d = _mapCam.transform.InverseTransformDirection(cam.transform.forward);
        return Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
    }
}
