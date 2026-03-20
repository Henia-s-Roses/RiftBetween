// KnightEnemy.cs
// Melee enemy. Stands idle until a player enters detection range,
// then walks toward them and deals contact damage on overlap.

using Mirror;
using UnityEngine;

public class KnightEnemy : EnemyBase
{
    // ?? Inspector ?????????????????????????????????????????????????????????????

    [Header("Knight Settings")]
    public float detectionRange = 5f;    // Distance at which the knight notices a player
    public float moveSpeed = 2.5f;
    public float attackRate = 1f;    // Seconds between contact damage ticks
    public float attackDamage = 15f;   // Overrides base contactDamage for timed hits

    // ?? State ?????????????????????????????????????????????????????????????????

    private enum KnightState { Idle, Chasing }
    private KnightState state = KnightState.Idle;
    private float attackTimer = 0f;

    // ?? Server-side AI loop ???????????????????????????????????????????????????

    // AI only runs on the server � position is synced via NetworkTransform
    private void FixedUpdate( )
    {
        if (!isServer) return;
        attackTimer -= Time.fixedDeltaTime;

        GameObject target = GetClosestPlayer( );

        if (target == null)
        {
            state = KnightState.Idle;
            return;
        }

        float dist = Vector2.Distance(transform.position, target.transform.position);

        if (dist <= detectionRange)
        {
            state = KnightState.Chasing;
            ChasePlayer(target);
        } else
        {
            state = KnightState.Idle;

            // Slow deceleration when returning to idle
            rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(rb.linearVelocity.x, 0f, moveSpeed),
                rb.linearVelocity.y
            );
        }
    }

    private void ChasePlayer(GameObject target)
    {
        Vector2 dir = ( target.transform.position - transform.position ).normalized;
        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);

        // Flip sprite to face movement direction
        if (spriteRenderer != null && dir.x != 0)
            spriteRenderer.flipX = dir.x < 0;
    }

    // ?? Contact damage (timed, not per-frame) ?????????????????????????????????

    // Override base contact to use a cooldown timer instead of firing every frame
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        // Intentionally left empty � using OnTriggerStay2D with a timer instead
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isServer) return;
        if (attackTimer > 0f) return;

        var health = other.GetComponent<PlayerHealth>( );
        if (health != null)
        {
            health.TakeDamage(attackDamage);
            attackTimer = attackRate;
            RiftLogger.Log($"Knight hit player for {attackDamage}", this);
        }
    }
}