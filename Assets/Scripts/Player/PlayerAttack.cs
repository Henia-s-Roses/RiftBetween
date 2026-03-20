// PlayerAttack.cs



using Mirror;
using System.Collections;
using UnityEngine;

public enum AttackMode { Melee, Projectile }

public class PlayerAttack : NetworkBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float hitboxDuration = 0.15f;

    [Header("Mode")]
    [SerializeField] private AttackMode attackMode = AttackMode.Melee;

    [Header("Melee References")]
    [SerializeField] private GameObject attackHitbox;

    [Header("Projectile References")]
    [Tooltip("OrbProjectile prefab — Wanderer only")]
    [SerializeField] private GameObject orbPrefab;

    [SerializeField] private Transform projectileOrigin;

    private PlayerAnimator _playerAnimator;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private float _lastAttackTime = -999f;
    private bool _attackLocked;

    private float _facingDirection = 1f;

    // ── Setup ─────────────────────────────────────────────────────────────────

    private void Awake( )
    {
        _playerAnimator = GetComponent<PlayerAnimator>( );
    }

    // Called by RiftNetworkPlayer.OnCharacterConfirmed() after the game starts.
    public void SetAttackMode(AttackMode mode)
    {
        attackMode = mode;
        RiftLogger.Log($"Attack mode set to {mode}", this);

        // Hide the melee hitbox GameObject entirely if this player is a Wanderer
        if (attackHitbox != null)
            attackHitbox.SetActive(mode == AttackMode.Melee);
    }

    // Called by PlayerMovement whenever the player flips direction.
    public void SetFacingDirection(float dir)
    {
        _facingDirection = dir;
    }

    // ── Called by PlayerInput ─────────────────────────────────────────────────

    public void TryAttack( )
    {
        if (!isLocalPlayer) return;
        if (_attackLocked) return;
        if (Time.time < _lastAttackTime + attackCooldown) return;

        _lastAttackTime = Time.time;

        switch (attackMode)
        {
            case AttackMode.Melee:
                StartCoroutine(ActivateHitbox( ));
                CmdMeleeAttack( );
                break;

            case AttackMode.Projectile:
                CmdFireProjectile(_facingDirection);
                break;
        }

        _playerAnimator.TriggerAttack( );

        RiftLogger.Log($"Attack triggered ({attackMode})", this);
    }

    // ── Melee path ────────────────────────────────────────────────────────────

    private IEnumerator ActivateHitbox( )
    {
        attackHitbox.SetActive(true);
        yield return new WaitForSeconds(hitboxDuration);
        attackHitbox.SetActive(false);
    }

    [Command]
    private void CmdMeleeAttack( )
    {
        if (attackHitbox == null) return;

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            attackHitbox.transform.position,
            attackHitbox.GetComponent<Collider2D>( ).bounds.size,
            0f
        );

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (hit.GetComponent<RiftNetworkPlayer>( ) != null) continue;

            // Hit enemy
            var enemy = hit.GetComponent<EnemyBase>( );
            if (enemy != null)
            {
                RiftLogger.Log($"Melee hit {hit.name} for {attackDamage}", this);
                enemy.TakeDamage(attackDamage);
                continue;
            }

        }
    }

    // ── Projectile dir ───────────────────────────────────────────────────────
    [Command]
    private void CmdFireProjectile(float facingDir)
    {
        if (orbPrefab == null || projectileOrigin == null) return;

        float dir = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 fireDir = new Vector2(dir, 0f);

        GameObject orb = Instantiate(orbPrefab, projectileOrigin.position, Quaternion.identity);
        orb.GetComponent<OrbProjectile>( )?.Initialize(fireDir, attackDamage, isEnemy: false);
        NetworkServer.Spawn(orb);

    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    public void ApplyDamageBuff(float multiplier, float duration)
    {
        StartCoroutine(DamageBuffCoroutine(multiplier, duration));
    }

    private IEnumerator DamageBuffCoroutine(float multiplier, float duration)
    {
        attackDamage *= multiplier;
        RiftLogger.Log($"Damage buff active — {multiplier}x for {duration}s", this);
        yield return new WaitForSeconds(duration);
        attackDamage /= multiplier;
        RiftLogger.Log("Damage buff expired", this);
    }

    public void SetAttackLocked(bool locked)
    {
        _attackLocked = locked;
    }
}