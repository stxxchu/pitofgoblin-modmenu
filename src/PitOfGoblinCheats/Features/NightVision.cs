using System;
using System.Collections.Generic;
using Entities;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace PitOfGoblinCheats;

internal static class NightVision
{
    class LightState
    {
        public float Intensity, Range, Peak;
        public bool Enabled, Boosted;
    }

    static readonly Dictionary<IntPtr, LightState> Lights = new();
    static readonly Dictionary<IntPtr, bool> LocalEyes = new();
    static IntPtr _eyesCachedFor;

    static GameObject _root;
    static Volume _volume;

    static bool Active => Cheats.NightVision;

    public static void Tick()
    {
        if (Active && _volume == null)
            CreateVolume();

        if (_volume != null)
        {
            _volume.enabled = Active;
            _volume.weight = Active ? 1f : 0f;
        }
    }

    static void CreateVolume()
    {
        _root = new GameObject("PoG_NightVision");
        Object.DontDestroyOnLoad(_root);
        _root.hideFlags = HideFlags.HideAndDontSave;
        _root.layer = FindVolumeLayer();

        _volume = _root.AddComponent<Volume>();
        _volume.isGlobal = true;
        _volume.priority = 1000f;

        var profile = ScriptableObject.CreateInstance(Il2CppType.Of<VolumeProfile>()).Cast<VolumeProfile>();
        profile.hideFlags = HideFlags.HideAndDontSave;
        _volume.sharedProfile = profile;

        var color = profile.Add(Il2CppType.Of<ColorAdjustments>(), false).Cast<ColorAdjustments>();
        color.postExposure.Override(Settings.NvExposure.Value);
        if (Settings.NvGreenTint.Value)
        {
            color.colorFilter.Override(new Color(0.55f, 1f, 0.55f, 1f));
            color.saturation.Override(-60f);
        }

        if (Settings.NvDisableFog.Value)
        {
            var fog = profile.Add(Il2CppType.Of<Fog>(), false).Cast<Fog>();
            fog.enabled.Override(false);
        }
    }

    static int FindVolumeLayer()
    {
        foreach (var obj in Object.FindObjectsByType(Il2CppType.Of<Volume>(), FindObjectsSortMode.None))
        {
            var v = obj.TryCast<Volume>();
            if (v != null && v.isGlobal) return v.gameObject.layer;
        }
        return 0;
    }

    public static void BeforeEyesUpdate(GoblinEyes eyes)
    {
        var light = eyes.m_goblinLight;
        if (light == null || !Lights.TryGetValue(light.Pointer, out var state) || !state.Boosted) return;

        light.intensity = state.Intensity;
        light.range = state.Range;
        light.enabled = state.Enabled;
        state.Boosted = false;
    }

    public static void AfterEyesUpdate(GoblinEyes eyes)
    {
        var light = eyes.m_goblinLight;
        if (light == null || !IsLocal(eyes)) return;

        if (!Lights.TryGetValue(light.Pointer, out var state))
            Lights[light.Pointer] = state = new LightState();

        if (light.enabled)
            state.Peak = Math.Max(state.Peak, light.intensity);

        if (!Active) return;

        state.Intensity = light.intensity;
        state.Range = light.range;
        state.Enabled = light.enabled;
        state.Boosted = true;

        light.enabled = true;
        light.intensity = Math.Max(state.Intensity, state.Peak) * Settings.NvLightBoost.Value;
        light.range = Math.Max(state.Range, Settings.NvLightRange.Value);
    }

    static bool IsLocal(GoblinEyes eyes)
    {
        var player = Cheats.Player;
        if (player == null) return false;

        if (_eyesCachedFor != Cheats.PlayerPtr)
        {
            LocalEyes.Clear();
            _eyesCachedFor = Cheats.PlayerPtr;
        }
        if (LocalEyes.TryGetValue(eyes.Pointer, out bool cached)) return cached;

        var owner = eyes.GetComponentInParent(Il2CppType.Of<EntityPlayer>(), true)?.TryCast<EntityPlayer>();
        bool local = owner != null
            ? owner.Pointer == Cheats.PlayerPtr
            : eyes.transform.IsChildOf(player.transform) ||
              Vector3.Distance(eyes.transform.position, player.transform.position) < 2.5f;

        LocalEyes[eyes.Pointer] = local;
        return local;
    }
}
