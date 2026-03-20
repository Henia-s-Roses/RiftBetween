// OrbProjectile.cs
// Projectile fired by MageEnemy and the Aberration boss.
// Travels in a straight line — not homing.
// Used by both enemies and (later) players if needed via isEnemy flag.

using Mirror;
using UnityEngine;

public class OrbProjectile : NetworkBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Orb Settings")]
    public float speed = 6f;
    public float lifetime = 4f;     // Auto-destroys after this many seconds

    // ── Runtime state ─────────────────────────────────────────────────────────

    private Vector2 direction;
    private float damage;
    private bool fromEnemy;      // True = damages players. False = damages enemies.
    private float lifeTimer;

    private Rigidbody2D rb;

    // ── Setup ─────────────────────────────────────────────────────────────────
    public bool FiredByEnemy => fromEnemy;

    private void Awake( )
    {
        rb = GetComponent<Rigidbody2D>( );
    }

    // Called immediately after spawning to configure the orb.
    // direction: normalized Vector2 travel direction
    // dmg: damage dealt on hit
    // isEnemy: true if fired by an enemy (hits players), false if fired by a player (hits enemies)
    public void Initialize(Vector2 dir, float dmg, bool isEnemy)
    {
        direction = dir.normalized;
        damage = dmg;
        fromEnemy = isEnemy;
        lifeTimer = lifetime;

        // Rotate sprite to face travel direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    private void FixedUpdate( )
    {
        if (!isServer) return;

        rb.linearVelocity = direction * speed;

        lifeTimer -= Time.fixedDeltaTime;
        if (lifeTimer <= 0f)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    // ── Hit detection ─────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isServer) return;

        if (fromEnemy)
        {
            // Hits players
            var health = other.GetComponent<PlayerHealth>( );
            if (health != null)
            {
                health.TakeDamage(damage);
                RiftLogger.Log($"Orb hit player for {damage}", this);
                NetworkServer.Destroy(gameObject);
            }
        } else
        {
            // Hits enemies — for future player projectile use
            var enemy = other.GetComponent<EnemyBase>( );
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                RiftLogger.Log($"Orb hit enemy for {damage}", this);
                NetworkServer.Destroy(gameObject);
            }
        }

        // Destroy on any solid surface hit (walls, platforms)
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            NetworkServer.Destroy(gameObject);
    }
}