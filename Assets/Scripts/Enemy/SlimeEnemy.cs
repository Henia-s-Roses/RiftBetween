// RiftSlime.cs

using Mirror;
using UnityEngine;

public class RiftSlime : EnemyBase
{
    [Header("Slime Settings")]
    public float moveSpeed = 3f;
    public float attackRate = 0.8f;
    public float attackDamage = 8f;
    public float detectionRange = 8f;

    private float _attackTimer;

    private void FixedUpdate( )
    {
        if (!isServer) return;

        _attackTimer -= Time.fixedDeltaTime;

        GameObject target = GetClosestPlayer( );
        if (target == null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            RpcSetMoving(false); // idle
            return;
        }

        float dist = Vector2.Distance(transform.position, target.transform.position);

        if (dist > detectionRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            RpcSetMoving(false); // idle
            return;
        }

        Vector2 dir = ( target.transform.position - transform.position ).normalized;

        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
        RpcSetMoving(true); // moving

        // flip sprite
        if (dir.x != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = dir.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

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

            RpcTriggerAttack( ); 
        }
    }

    [ClientRpc]
    private void RpcTriggerAttack( )
    {
        enemyAnimator?.TriggerAttack( );
    }

    [ClientRpc]
    private void RpcSetMoving(bool moving)
    {
        enemyAnimator?.SetMoving(moving);
    }

    [Server]
    protected override void Die( )
    {
        RpcOnDeath( );
        NetworkServer.Destroy(gameObject);
    }
}