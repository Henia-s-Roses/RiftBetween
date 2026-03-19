// PlayerAttack.cs
// Handles the basic attack hitbox and damage application.
// TryAttack() is called by PlayerInput on button press.
// Damage is applied server-side via Command.

using Mirror;
using UnityEngine;

public class PlayerAttack : NetworkBehaviour
{
    // ?? Inspector ?????????????????????????????????????????????????????????????

    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackCooldown = 0.4f;  // Seconds between attacks
    [SerializeField] private float hitboxDuration = 0.15f; // How long hitbox stays active

    [Header("References")]
    [Tooltip("Child object with a Collider2D set as Trigger — this is the swing hitbox")]
    [SerializeField] private GameObject attackHitbox;

    // ?? Runtime ???????????????????????????????????????????????????????????????

    private float _lastAttackTime = -999f;
    private bool _attackLocked;  // True when player is downed

    // ?? Called by PlayerInput ?????????????????????????????????????????????????

    public void TryAttack( )
    {
        if (!isLocalPlayer) return;
        if (_attackLocked) return;
        if (Time.time < _lastAttackTime + attackCooldown) return; // Still on cooldown

        _lastAttackTime = Time.time;
        RiftLogger.Log("Attack triggered", this);

        // Activate the hitbox locally for visual/feel, then tell server to validate hits
        StartCoroutine(ActivateHitbox( ));
        CmdNotifyAttack( );
    }

    // Enables the hitbox collider briefly, then disables it
    private System.Collections.IEnumerator ActivateHitbox( )
    {
        attackHitbox.SetActive(true);
        yield return new WaitForSeconds(hitboxDuration);
        attackHitbox.SetActive(false);
    }

    // ?? Server-side hit detection ?????????????????????????????????????????????

    // Tells the server an attack happened — server re-checks hitbox overlap
    // to prevent clients from faking hits
    [Command]
    private void CmdNotifyAttack( )
    {
        // Server checks for any enemies inside the attack range
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            attackHitbox.transform.position,
            attackHitbox.GetComponent<Collider2D>( ).bounds.size,
            0f
        );

        foreach (var hit in hits)
        {
            // Don't hit yourself or other players
            if (hit.gameObject == gameObject) continue;
            if (hit.GetComponent<RiftNetworkPlayer>( ) != null) continue;

            // Apply damage to enemies
            var enemy = hit.GetComponent<EnemyBase>( );
            if (enemy != null)
            {
                RiftLogger.Log($"Hit enemy: {hit.name} for {attackDamage} dmg", this);
                enemy.TakeDamage(attackDamage);
            }
        }
    }

    // ?? Called by PlayerRevive ????????????????????????????????????????????????

    public void SetAttackLocked(bool locked)
    {
        _attackLocked = locked;
    }
}