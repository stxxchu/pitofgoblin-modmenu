using System.Collections.Generic;
using Entities;
using Game.Gameplay;
using Unity.Netcode;
using UnityEngine;

namespace PitOfGoblinCheats;

internal static class Combat
{
    public const float Lethal = 99999f;

    public static bool Kill(Entity target, string name, bool quiet = false)
    {
        if (target == null || target.IsDead)
        {
            if (!quiet) Notify.Show($"{name} is already dead");
            return false;
        }
        if (target.Pointer == Cheats.PlayerPtr)
        {
            if (!quiet) Notify.Show("Not going to kill you");
            return false;
        }

        if (target.IsOwner)
            target.Kill(false, false);
        else
            target.TakeDamage(Lethal, true, Entity.DamageType.None, Entity.DamageEffectType.Normal, target.transform.position, false, OwnReference());

        Cheats.UsedThisSession = true;
        if (!quiet) Notify.Show($"Killed {name}");
        return true;
    }

    public static void KillAllEnemies()
    {
        int killed = 0;
        foreach (var e in Targets.Enemies)
            if (Kill(e.Entity, e.Name, quiet: true)) killed++;

        Notify.Show(killed > 0 ? $"Killed {killed} enemies" : "No enemies around");
    }

    public static bool Revive(PlayerTarget p, bool quiet = false)
    {
        if (!p.IsDead)
        {
            if (!quiet) Notify.Show($"{p.Name} is alive");
            return false;
        }

        if (p.Character != null)
        {
            p.Character.Revive(0, 0);
        }
        else
        {
            var bucket = BucketSpawner.Bucket;
            if (bucket == null)
            {
                if (!quiet) Notify.Show("Can't respawn here, there is no bucket");
                return false;
            }
            bucket.RequestRespawnRpc(new NetworkObjectReference(p.Persistent.NetworkObject), 1f);
        }

        Cheats.UsedThisSession = true;
        if (!quiet) Notify.Show(p.IsLocal ? "You are back on your feet" : $"Revived {p.Name}");
        return true;
    }

    public static void ReviveAll()
    {
        int revived = 0;
        foreach (var p in Targets.Players)
            if (Revive(p, quiet: true)) revived++;

        Notify.Show(revived > 0 ? $"Revived {revived} player{(revived == 1 ? "" : "s")}" : "Nobody is dead");
    }

    public static void TeleportTo(PlayerTarget p)
    {
        var me = Cheats.Player;
        if (me == null || me.IsDead)
        {
            Notify.Show("You need to be alive to teleport");
            return;
        }
        if (p.IsDead)
        {
            Notify.Show($"{p.Name} is dead");
            return;
        }

        var target = p.Character.transform;
        var offset = -target.forward * 1.2f;
        offset.y = 0f;
        var facing = offset.sqrMagnitude > 0.01f ? Quaternion.LookRotation(-offset) : me.transform.rotation;
        me.Teleport(target.position + offset + Vector3.up * 0.1f, facing);

        Cheats.UsedThisSession = true;
        Notify.Show($"Teleported to {p.Name}");
    }

    public static bool Bring(PlayerTarget p, int slot = 0, int total = 1, bool quiet = false)
    {
        var me = Cheats.Player;
        if (me == null || me.IsDead)
        {
            if (!quiet) Notify.Show("You need to be alive to bring someone");
            return false;
        }
        if (p.IsLocal || p.IsDead)
        {
            if (!quiet) Notify.Show(p.IsLocal ? "That's you" : $"{p.Name} is dead");
            return false;
        }

        var forward = me.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();

        float angle = total > 1 ? (slot - (total - 1) / 2f) * 40f : 0f;
        var dir = Quaternion.Euler(0f, angle, 0f) * forward;
        var position = me.transform.position + dir * 1.6f + Vector3.up * 0.1f;
        p.Character.TeleportRpc(position, Quaternion.LookRotation(-dir));

        Cheats.UsedThisSession = true;
        if (!quiet) Notify.Show($"Brought {p.Name} to you");
        return true;
    }

    public static void BringAll()
    {
        var others = new List<PlayerTarget>();
        foreach (var p in Targets.Players)
            if (!p.IsLocal && !p.IsDead) others.Add(p);

        int brought = 0;
        for (int i = 0; i < others.Count; i++)
            if (Bring(others[i], i, others.Count, quiet: true)) brought++;

        Notify.Show(brought > 0 ? $"Brought {brought} player{(brought == 1 ? "" : "s")} to you" : "Nobody to bring");
    }

    public static bool IsOneHitTarget(Entity target, NetworkObjectReference damageOwner, float damage)
    {
        if (!Cheats.OneHitKill || damage <= 0f || Cheats.Player == null) return false;
        if (target.Pointer == Cheats.PlayerPtr || target.TryCast<EntityPlayer>() != null) return false;

        var nm = NetworkManager.Singleton;
        if (nm == null) return true;

        ulong id = damageOwner.NetworkObjectId;
        if (id == Cheats.Player.NetworkObjectId) return true;

        if (id != 0)
        {
            var spawned = nm.SpawnManager?.SpawnedObjects;
            if (spawned == null || !spawned.TryGetValue(id, out var source) || source == null) return false;
            return source.GetComponent<InventoryItem>() != null && source.OwnerClientId == nm.LocalClientId;
        }

        return !nm.IsServer || Vector3.Distance(target.transform.position, Cheats.Player.transform.position) < 6f;
    }

    static NetworkObjectReference OwnReference()
    {
        var me = Cheats.Player;
        return me != null && me.NetworkObject != null ? new NetworkObjectReference(me.NetworkObject) : default;
    }
}
