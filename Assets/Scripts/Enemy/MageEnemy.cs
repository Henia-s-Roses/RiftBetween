// MageEnemy.cs


using Mirror;
using UnityEngine;

public class MageEnemy : EnemyBase
{
    [Header("Mage Settings")]
    public float detectionRange = 10f;   
    public float preferredRange = 5f;    
    public float retreatSpeed = 2f;    
    public float fireRate = 2.5f;  
    public float projectileDamage = 15f;

    [Header("Projectile")]
    [Tooltip("Assign the OrbProjectile prefab")]
    public GameObject orbPrefab;

    public Transform firePoint;

    private float _fireTimer;

    private void FixedUpdate( )
    {
        if (!isServer) return;

        _fireTimer -= Time.fixedDeltaTime;

        GameObject target = GetClosestPlayer( );

        if (target == null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            RpcSetMoving(false);
            return;
        }

        float dist = Vector2.Distance(transform.position, target.transform.position);

        if (dist > detectionRange)
        {
            // Out of range — idle
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            RpcSetMoving(false);
            return;
        }

        // Face the target regardless of movement
        Vector2 dir = ( target.transform.position - transform.position ).normalized;

        if (dir.x != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = dir.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        if (dist < preferredRange)
        {
            // Too close — back away from the player
            rb.linearVelocity = new Vector2(-dir.x * retreatSpeed, rb.linearVelocity.y);
            RpcSetMoving(true);
        } else
        {
            // Good range — stand still and shoot
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            RpcSetMoving(false);
        }

        // Fire on cooldown
        if (_fireTimer <= 0f)
        {
            FireOrb(target.transform.position);
            _fireTimer = fireRate;
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other) { }

    [Server]
    private void FireOrb(Vector3 targetPos)
    {
        if (orbPrefab == null)
        {
            RiftLogger.Error("MageEnemy: orbPrefab not assigned", this);
            return;
        }

        // Use firePoint if assigned, otherwise fire from center
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        Vector2 fireDir = ( targetPos - spawnPos ).normalized;

        GameObject orb = Instantiate(orbPrefab, spawnPos, Quaternion.identity);
        orb.GetComponent<OrbProjectile>( )?.Initialize(fireDir, projectileDamage, isEnemy: true);
        NetworkServer.Spawn(orb);

        RpcTriggerAttack( );
        RiftLogger.Log($"Mage fired orb toward {targetPos}", this);
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