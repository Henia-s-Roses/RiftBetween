// EnemyBase.cs

using Mirror;
using UnityEngine;

public abstract class EnemyBase : NetworkBehaviour
{
    [Header("Stats")]
    public float maxHP = 30f;
    public float contactDamage = 10f;

    [SyncVar(hook = nameof(OnHPChanged))]
    public float currentHP;

    protected SpriteRenderer spriteRenderer;
    protected Rigidbody2D rb;
    protected Animator animator;
    protected Collider2D[] colliders;
    protected EnemyAnimator enemyAnimator;

    protected virtual void Awake( )
    {
        spriteRenderer = GetComponent<SpriteRenderer>( );
        rb = GetComponent<Rigidbody2D>( );
        animator = GetComponent<Animator>( );
        colliders = GetComponentsInChildren<Collider2D>( );
        enemyAnimator = GetComponent<EnemyAnimator>( ); 
    }

    public override void OnStartServer( )
    {
        currentHP = maxHP;

        // Server runs full physics — keep rigidbody normal
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.gravityScale = 1f;
        }
    }

    public override void OnStartClient( )
    {

        if (!isServer && rb != null)
        {
            rb.isKinematic = true;
            rb.gravityScale = 0f;   // ← this was missing — kills floating
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void OnHPChanged(float oldHP, float newHP) { }

    [Server]
    public virtual void TakeDamage(float amount)
    {
        if (currentHP <= 0) return;
        currentHP -= amount;
        RiftLogger.Log($"Took {amount} — HP {currentHP}/{maxHP}", this);
        RpcTriggerHit( );
        if (currentHP <= 0) Die( );
    }

    [Server]
    protected virtual void Die( )
    {
        RiftLogger.Log($"{GetType( ).Name} died", this);
        RpcOnDeath( );
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    protected void RpcTriggerHit( )
    {
        enemyAnimator?.TriggerHit( );
    }

    // Update RpcOnDeath to also trigger death animation:
    [ClientRpc]
    protected virtual void RpcOnDeath( )
    {
        enemyAnimator?.TriggerDeath( );
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!isServer) return;
        var health = other.GetComponent<PlayerHealth>( );
        if (health != null)
        {
            health.TakeDamage(contactDamage);
            RiftLogger.Log($"Contact damage {contactDamage} dealt to player", this);
        }
    }

    protected GameObject GetClosestPlayer( )
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0) return null;

        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (var p in players)
        {
            float dist = Vector2.Distance(transform.position, p.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = p;
            }
        }

        return closest;
    }
}