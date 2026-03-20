// EnemyBase.cs
// Abstract base for all enemies. Handles health, world-based texture swapping,
// SyncVar setup, and the shared death flow.
// All enemies extend this — never place this directly on a GameObject.

using Mirror;
using UnityEngine;

public abstract class EnemyBase : NetworkBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Stats")]
    public float maxHP = 30f;
    public float contactDamage = 10f;

    [Header("World Textures")]
    public Sprite worldASkin;
    public Sprite worldBSkin;
    public Sprite riftSkin;

    // ── Networked state ───────────────────────────────────────────────────────

    [SyncVar(hook = nameof(OnHPChanged))]
    public float currentHP;

    // ── Component refs ────────────────────────────────────────────────────────

    protected SpriteRenderer spriteRenderer;
    protected Rigidbody2D rb;
    protected Animator animator;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected virtual void Awake( )
    {
        spriteRenderer = GetComponent<SpriteRenderer>( );
        rb = GetComponent<Rigidbody2D>( );
        animator = GetComponent<Animator>( );
    }

    public override void OnStartServer( )
    {
        // Validate maxHP — catches the case where a scene instance was placed
        // but maxHP was never set in the Inspector (defaults to 0)
        if (maxHP <= 0f)
        {
            RiftLogger.Warn($"{GetType( ).Name} has maxHP <= 0 — defaulting to 30. Set maxHP in Inspector.", this);
            maxHP = 30f;
        }

        currentHP = maxHP;
        RiftLogger.Log($"Initialized — HP {currentHP}/{maxHP}", this);
    }

    public override void OnStartClient( )
    {
        WorldManager.OnWorldChanged += HandleWorldChanged;

        if (WorldManager.Instance != null)
            HandleWorldChanged((WorldState)WorldManager.Instance.activeWorld);
    }

    protected virtual void OnDestroy( )
    {
        WorldManager.OnWorldChanged -= HandleWorldChanged;
    }

    // ── Damage & death ────────────────────────────────────────────────────────

    [Server]
    public virtual void TakeDamage(float amount)
    {
        if (currentHP <= 0) return;

        currentHP -= amount;
        RiftLogger.Log($"Took {amount} dmg — HP {currentHP}/{maxHP}", this);

        if (currentHP <= 0)
            Die( );
    }

    [Server]
    protected virtual void Die( )
    {
        RiftLogger.Log($"{GetType( ).Name} died", this);
        RpcOnDeath( );
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    protected virtual void RpcOnDeath( )
    {
        // Particle / SFX hook
    }

    // ── World skin swap ───────────────────────────────────────────────────────

    private void HandleWorldChanged(WorldState newWorld)
    {
        if (spriteRenderer == null) return;

        spriteRenderer.sprite = newWorld switch
        {
            WorldState.WorldA => worldASkin,
            WorldState.WorldB => worldBSkin,
            WorldState.Rift => riftSkin,
            _ => worldASkin
        };
    }

    private void OnHPChanged(float oldHP, float newHP)
    {
        // Hook for HP bar / hit flash — implement later
    }

    // ── Contact damage ────────────────────────────────────────────────────────

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!isServer) return;

        var health = other.GetComponent<PlayerHealth>( );
        if (health != null)
        {
            health.TakeDamage(contactDamage);
            RiftLogger.Log($"Contact {contactDamage} dmg to player", this);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    protected GameObject GetClosestPlayer( )
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0) return null;

        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (var p in players)
        {
            var revive = p.GetComponent<PlayerRevive>( );
            if (revive != null && revive.isDown) continue;

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