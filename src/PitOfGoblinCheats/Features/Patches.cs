using System;
using System.Collections.Generic;
using Entities;
using Game.Platform;
using HarmonyLib;
using Unity.Netcode;

namespace PitOfGoblinCheats;

internal static class Patches
{
    static readonly HashSet<Achievements> LoggedAchievements = new();

    static bool IsProtectedPlayer(IntPtr ptr) =>
        Cheats.God && Cheats.PlayerPtr != IntPtr.Zero && ptr == Cheats.PlayerPtr;

    [HarmonyPatch(typeof(Entity), "TakeDamage")]
    static class EntityTakeDamage
    {
        static bool Prefix(Entity __instance, ref float damage, NetworkObjectReference damageOwner)
        {
            if (IsProtectedPlayer(__instance.Pointer)) return false;
            if (Combat.IsOneHitTarget(__instance, damageOwner, damage)) damage = Combat.Lethal;
            return true;
        }
    }

    [HarmonyPatch(typeof(EntityPlayer), "TakeDamageOwner")]
    static class PlayerTakeDamageOwner
    {
        static bool Prefix(EntityPlayer __instance) => !IsProtectedPlayer(__instance.Pointer);
    }

    [HarmonyPatch(typeof(Entity), "Kill")]
    static class EntityKill
    {
        static bool Prefix(Entity __instance) => !IsProtectedPlayer(__instance.Pointer);
    }

    static bool KeepStamina(StaminaSystem s) =>
        Cheats.InfiniteStamina && Cheats.StaminaPtr != IntPtr.Zero && s.Pointer == Cheats.StaminaPtr;

    [HarmonyPatch(typeof(StaminaSystem), "ConsumeStamina")]
    static class ConsumeStamina
    {
        static bool Prefix(StaminaSystem __instance) => !KeepStamina(__instance);
    }

    [HarmonyPatch(typeof(StaminaSystem), "ConsumeStaminaOnce")]
    static class ConsumeStaminaOnce
    {
        static bool Prefix(StaminaSystem __instance) => !KeepStamina(__instance);
    }

    [HarmonyPatch(typeof(StaminaSystem), "DepleteStamina")]
    static class DepleteStamina
    {
        static bool Prefix(StaminaSystem __instance) => !KeepStamina(__instance);
    }

    [HarmonyPatch(typeof(StaminaSystem), "StartDepletion")]
    static class StartDepletion
    {
        static bool Prefix(StaminaSystem __instance) => !KeepStamina(__instance);
    }

    struct SavedSpeed
    {
        public bool Active;
        public float Walk, Crouch, Accel, Decel;
    }

    [HarmonyPatch(typeof(PlayerController), "Update")]
    static class PlayerSpeed
    {
        static void Prefix(PlayerController __instance, out SavedSpeed __state)
        {
            __state = default;
            float mult = Cheats.SpeedMultiplier;
            if (mult == 1f || __instance.Pointer != Cheats.ControllerPtr)
                return;

            __state = new SavedSpeed
            {
                Active = true,
                Walk = __instance.m_movementSpeed,
                Crouch = __instance.m_crouchMovementSpeed,
                Accel = __instance.m_movementAcceleration,
                Decel = __instance.m_movementDecceleration,
            };

            __instance.m_movementSpeed = __state.Walk * mult;
            __instance.m_crouchMovementSpeed = __state.Crouch * mult;
            __instance.m_movementAcceleration = __state.Accel * mult;
            __instance.m_movementDecceleration = __state.Decel * mult;
        }

        static void Postfix(PlayerController __instance, SavedSpeed __state)
        {
            if (!__state.Active) return;

            __instance.m_movementSpeed = __state.Walk;
            __instance.m_crouchMovementSpeed = __state.Crouch;
            __instance.m_movementAcceleration = __state.Accel;
            __instance.m_movementDecceleration = __state.Decel;
        }
    }

    static bool KeepCharge(MagicClass magic)
    {
        if (!Cheats.InfiniteMagic || Cheats.PlayerPtr == IntPtr.Zero) return false;
        var owner = magic.OwnerEntity;
        return owner != null && owner.Pointer == Cheats.PlayerPtr;
    }

    [HarmonyPatch(typeof(MagicClass), "UseCharge")]
    static class UseCharge
    {
        static bool Prefix(MagicClass __instance) => !KeepCharge(__instance);
    }

    [HarmonyPatch(typeof(MagicClass), "UseChargeRpc")]
    static class UseChargeRpc
    {
        static bool Prefix(MagicClass __instance) => !KeepCharge(__instance);
    }

    [HarmonyPatch(typeof(GoblinEyes), "Update")]
    static class GoblinEyesUpdate
    {
        static void Prefix(GoblinEyes __instance) => NightVision.BeforeEyesUpdate(__instance);
        static void Postfix(GoblinEyes __instance) => NightVision.AfterEyesUpdate(__instance);
    }

    static bool AllowAchievement(Achievements achievement)
    {
        if (!Settings.BlockAchievements.Value || !Cheats.UsedThisSession) return true;
        if (LoggedAchievements.Add(achievement))
            Plugin.L.LogInfo($"Blocked achievement {achievement}, cheats were used this session");
        return false;
    }

    [HarmonyPatch(typeof(XboxRuntimeManager), "UnlockAchievement")]
    static class UnlockAchievement
    {
        static bool Prefix(Achievements achievement) => AllowAchievement(achievement);
    }

    [HarmonyPatch(typeof(XboxRuntimeManager), "ProgressAchievement", typeof(Achievements))]
    static class ProgressAchievement
    {
        static bool Prefix(Achievements achievement) => AllowAchievement(achievement);
    }

    [HarmonyPatch(typeof(XboxRuntimeManager), "ProgressAchievement", typeof(Achievements), typeof(int))]
    static class ProgressAchievementAmount
    {
        static bool Prefix(Achievements achievement) => AllowAchievement(achievement);
    }

    [HarmonyPatch(typeof(XboxRuntimeManager), "SetAchievementProgress")]
    static class SetAchievementProgress
    {
        static bool Prefix(Achievements achievement) => AllowAchievement(achievement);
    }

    [HarmonyPatch(typeof(XboxRuntimeManager), "UpdateAchievementAsync")]
    static class UpdateAchievementAsync
    {
        static bool Prefix(Achievements achievement) => AllowAchievement(achievement);
    }
}
