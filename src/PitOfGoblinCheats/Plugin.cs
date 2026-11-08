using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace PitOfGoblinCheats;

[BepInPlugin(Guid, Name, Version)]
public class Plugin : BasePlugin
{
    public const string Guid = "pl.gobliny.pitofgoblincheats";
    public const string Name = "Pit of Goblin Cheats";
    public const string Version = "1.1.0";

    internal static ManualLogSource L;

    public override void Load()
    {
        L = Log;
        Settings.Bind(Config);
        ApplyPatches();
        CursorFix.Install();
        AddComponent<CheatRunner>();
        L.LogInfo($"{Name} {Version} ready");
    }

    static void ApplyPatches()
    {
        var harmony = new Harmony(Guid);

        foreach (var type in typeof(Patches).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
        {
            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception e)
            {
                L.LogWarning($"Patch {type.Name} failed: {e.GetBaseException().Message}");
            }
        }
    }
}
