// MageEnemy.cs
// Ranged enemy. Stands idle until a player enters detection range,
// then stops moving and fires orb projectiles at them on a timer.

using Mirror;
using UnityEngine;

public class MageEnemy : EnemyBase
{
    // ?? Inspector ?????????????????????????????????????????????????????????????

    [Header("Mage Settings")]
    public float detectionRange = 7f;
    public float fireRate = 2.5f;   // Seconds between shots
    public float projectileDamage = 12f;
    public float preferredRange = 5f;     // Mage tries to stay at this distance from player

    [Header("Prefab")]
    [Tooltip("Assign the OrbProjectile prefab here")]
    public GameObject orbPrefab;

    // ?? State ?????????????????????????????????????????????????????????????????

    private float fireTimer = 0f;

    // ?? Server-side AI loop ???????????????????????????????????????????????????

    private void FixedUpdate( )
    {
        if (!isServer) return;

        fireTimer -= Time.fixedDeltaTime;

        GameObject target = GetClosestPlayer( );
        if (target == null) return;

        float dist = Vector2.Distance(transform.position, target.transform.position);
        if (dist > detectionRange) return; // Out of range � stay idle

        // Maintain preferred distance � back away if player gets too close
        MaintainDistance(target);

        // Fire on timer
        if (fireTimer <= 0f)
        {
            FireOrb(target.transform.position);
            fireTimer = fireRate;
        }
    }

    private void MaintainDistance(GameObject target)
    {
        float dist = Vector2.Distance(transform.position, target.transform.position);
        Vector2 dir = ( target.transform.position - transform.position ).normalized;

        if (dist < preferredRange)
        {
            // Too close � back away
            rb.linearVelocity = new Vector2(-dir.x * 2f, rb.linearVelocity.y);
        } else
        {
            // At good range � stand still
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        if (spriteRenderer != null && dir.x != 0)
            spriteRenderer.flipX = dir.x < 0;
    }

    // Spawns an orb projectile aimed at the target's current position.
    // Not homing � fires in a straight line toward where the player was.
    private void FireOrb(Vector3 targetPosition)
    {
        if (orbPrefab == null)
        {
            RiftLogger.Error("OrbPrefab not assigned on MageEnemy", this);
            return;
        }

        Vector2 dir = ( targetPosition - transform.position ).normalized;

        GameObject orb = Instantiate(orbPrefab, transform.position, Quaternion.identity);
        orb.GetComponent<OrbProjectile>( )?.Initialize(dir, projectileDamage, isEnemy: true);
        NetworkServer.Spawn(orb);

        RiftLogger.Log($"Mage fired orb toward {targetPosition}", this);
    }
}