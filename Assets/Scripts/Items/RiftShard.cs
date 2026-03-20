// RiftShard.cs
// Rare item. On pickup, immediately applies buffs to BOTH players:
//   - Full heal
//   - Increased damage output
//   - Increased movement speed
// Buffs last for the passive duration (GameConfig.PASSIVE_DURATION).

using Mirror;
using UnityEngine;
using System.Collections;

public class RiftShard : PassiveItem
{
    // ?? Inspector ?????????????????????????????????????????????????????????????

    [Header("Rift Shard Buffs")]
    public float damageMultiplier = 1.5f;   // 50% increased damage output
    public float speedMultiplier = 1.3f;   // 30% increased movement speed

    // ?? PassiveItem implementation ????????????????????????????????????????????

    // RiftShard applies to ALL players, not just the one who picked it up.
    [Server]
    protected override void OnActivate(GameObject player)
    {
        RiftLogger.System("RiftShard activated — buffing all players", this);

        foreach (var health in FindObjectsOfType<PlayerHealth>( ))
        {
            var buffTarget = health.gameObject;

            // Full heal
            health.Heal(GameConfig.PLAYER_MAX_HP);

            // Apply damage and speed buffs
            var movement = buffTarget.GetComponent<PlayerMovement>( );
            var attack = buffTarget.GetComponent<PlayerAttack>( );

            if (movement != null)
            {
                movement.ApplySpeedBuff(speedMultiplier, GameConfig.PASSIVE_DURATION);
                RiftLogger.Log($"RiftShard: speed buff applied to {buffTarget.name}", this);
            }

            if (attack != null)
            {
                attack.ApplyDamageBuff(damageMultiplier, GameConfig.PASSIVE_DURATION);
                RiftLogger.Log($"RiftShard: damage buff applied to {buffTarget.name}", this);
            }
        }

        RpcOnRiftShardActivated( );
    }

    // RiftShard doesn't need to clean up on expiry —
    // buffs are self-expiring via coroutines on PlayerMovement and PlayerAttack.
    [Server]
    protected override void OnExpire(GameObject player)
    {
        RiftLogger.Log("RiftShard duration ended", this);
    }

    // Fires on all clients — use for screen flash, sound, or VFX
    [ClientRpc]
    private void RpcOnRiftShardActivated( )
    {
        // e.g. ScreenFlash.Instance?.Flash(Color.magenta);
    }
}