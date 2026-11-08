using System;
using System.Runtime.InteropServices;
using BepInEx.Unity.IL2CPP.Hook;
using Il2CppInterop.Runtime;

namespace PitOfGoblinCheats;

internal static class CursorFix
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void SetLockStateFn(int mode);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void SetVisibleFn(byte visible);

    const int CursorNone = 0;

    static SetLockStateFn _lockHook, _lockOriginal;
    static SetVisibleFn _visibleHook, _visibleOriginal;
    static INativeDetour _lockDetour, _visibleDetour;

    public static void Install()
    {
        _lockHook = SetLockState;
        _lockDetour = Hook("UnityEngine.Cursor::set_lockState", _lockHook, out _lockOriginal);

        _visibleHook = SetVisible;
        _visibleDetour = Hook("UnityEngine.Cursor::set_visible", _visibleHook, out _visibleOriginal);
    }

    static INativeDetour Hook<T>(string icall, T hook, out T original) where T : Delegate
    {
        original = null;
        try
        {
            IntPtr target = IL2CPP.il2cpp_resolve_icall(icall);
            if (target == IntPtr.Zero)
            {
                Plugin.L.LogWarning($"{icall} not found, the cursor may stick in the menu");
                return null;
            }
            return INativeDetour.CreateAndApply(target, hook, out original);
        }
        catch (Exception e)
        {
            Plugin.L.LogWarning($"Hook {icall} failed: {e.Message}");
            return null;
        }
    }

    static void SetLockState(int mode)
    {
        try
        {
            if (Menu.Open) mode = CursorNone;
        }
        catch
        {
        }
        _lockOriginal(mode);
    }

    static void SetVisible(byte visible)
    {
        try
        {
            if (Menu.Open) visible = 1;
        }
        catch
        {
        }
        _visibleOriginal(visible);
    }
}
