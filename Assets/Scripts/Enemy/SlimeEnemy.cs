// RiftSlime.cs
// Melee enemy. Rushes players on detection.
// Large slimes split into 2 mini slimes on death.
// Mini slimes do NOT split again.

using Mirror;
using UnityEngine;

public class RiftSlime : EnemyBase
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Slime Settings")]
    public float moveSpeed = 3f;
    public float attackRate = 0.8f;
    public float attackDamage = 8f;
    public float detectionRange = 8f;

    [Header("Split")]
    public bool canSplit = true;
    public GameObject miniSlimePrefab;
    public float miniSlimeScale = 0.5f;

    // ── State ─────────────────────────────────────────────────────────────────

    private float _attackTimer;

    // ── AI ────────────────────────────────────────────────────────────────────

    private void FixedUpdate( )
    {
        if (!isServer) return;

        _attackTimer -= Time.fixedDeltaTime;

        GameObject target = GetClosestPlayer( );
        if (target == null) return;

        float dist = Vector2.Distance(transform.position, target.transform.position);
        if (dist > detectionRange) return;

        Vector2 dir = ( target.transform.position - transform.position ).normalized;
        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);

        // Flip the whole GameObject — same as PlayerMovement approach
        if (dir.x != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = dir.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    // ── Contact damage ────────────────────────────────────────────────────────

    protected override void OnTriggerEnter2D(Collider2D other) { }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isServer) return;
        if (_attackTimer > 0f) return;

        var health = other.GetComponent<PlayerHealth>( );
        if (health != null)
        {
            health.TakeDamage(attackDamage);
            _attackTimer = attackRate;
        }
    }

    // ── Death ─────────────────────────────────────────────────────────────────

    [Server]
    protected override void Die( )
    {
        if (canSplit && miniSlimePrefab != null)
        {
            SpawnMiniSlime(Vector2.left);
            SpawnMiniSlime(Vector2.right);
            RiftLogger.Log("Split into 2 mini slimes", this);
        }

        RpcOnDeath( );
        NetworkServer.Destroy(gameObject);
    }

    private void SpawnMiniSlime(Vector2 offset)
    {
        Vector3 spawnPos = transform.position + (Vector3)( offset * 0.5f );
        GameObject mini = Instantiate(miniSlimePrefab, spawnPos, Quaternion.identity);

        mini.transform.localScale = transform.localScale * miniSlimeScale;

        // Configure BEFORE NetworkServer.Spawn — after Spawn, the object is
        // networked and SyncVars are locked to server-set values only
        var slime = mini.GetComponent<RiftSlime>( );
        if (slime != null)
        {
            slime.canSplit = false;
            slime.maxHP = maxHP * 0.4f;
            slime.attackDamage = attackDamage * 0.6f;
        }

        NetworkServer.Spawn(mini);
    }
}