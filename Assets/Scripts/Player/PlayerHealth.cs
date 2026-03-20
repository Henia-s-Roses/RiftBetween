// PlayerHealth.cs


using Mirror;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    private PlayerAnimator _animator;

    // ── Synced state ──────────────────────────────────────────────────────────

    [SyncVar(hook = nameof(OnHPChanged))]
    private float _currentHP;
    public delegate void DamageModifier(ref float damage);
    public event DamageModifier OnBeforeDamage;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private bool _isInvincible;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake( )
    {
        _animator = GetComponent<PlayerAnimator>();
    }

    public override void OnStartServer( )
    {
        _currentHP = GameConfig.PLAYER_MAX_HP;
    }

    // ── Taking damage ─────────────────────────────────────────────────────────


    [Server]
    public void TakeDamage(float amount)
    {
        if (_isInvincible)
        {
            RiftLogger.Log($"Damage blocked — invincible ({amount} ignored)", this);
            return;
        }


        _animator.SetKnockback(true);

        _currentHP = Mathf.Max(0f, _currentHP - amount);
        RiftLogger.Log($"Took {amount} dmg — HP {_currentHP}/{GameConfig.PLAYER_MAX_HP}", this);

        if (_currentHP <= 0f)
        {
            RiftLogger.Warn("HP hit 0 — triggering downed", this);
            GetComponent<PlayerRespawn>( )?.TriggerDeath( );
        }
    }

    [Command]
    public void CmdTakeDamage(float amount)
    {
        TakeDamage(amount);
    }

    // ── Healing ───────────────────────────────────────────────────────────────

    [Server]
    public void Heal(float amount)
    {
        _currentHP = Mathf.Min(GameConfig.PLAYER_MAX_HP, _currentHP + amount);
        RiftLogger.Log($"Healed {amount} — HP now {_currentHP}", this);
    }

    // Shared potion calls this on both players from the server directly.
    [Command]
    public void CmdHeal(float amount)
    {
        Heal(amount);
    }

    // ── Invincibility ─────────────────────────────────────────────────────────

    // Called on world swap (0.5s) and on revive (2s)
    [Server]
    public void StartInvincibility(float duration)
    {
        StartCoroutine(InvincibilityCoroutine(duration));
    }

    private System.Collections.IEnumerator InvincibilityCoroutine(float duration)
    {
        _isInvincible = true;
        RiftLogger.Log($"Invincible for {duration}s", this);
        yield return new WaitForSeconds(duration);
        _isInvincible = false;
        RiftLogger.Log("Invincibility ended", this);
    }

    // ── SyncVar hook ──────────────────────────────────────────────────────────

    // Fires on all clients when HP changes — drives the health bar UI
    private void OnHPChanged(float oldHP, float newHP)
    {
        // UI update — PlayerHealthUI subscribes to this via an event
        OnHealthChanged?.Invoke(newHP, GameConfig.PLAYER_MAX_HP);
    }
        
    // ── Events ────────────────────────────────────────────────────────────────

    // UI scripts subscribe to this instead of polling HP every frame
    public event System.Action<float, float> OnHealthChanged; // (currentHP, maxHP)

    // ── Public state ──────────────────────────────────────────────────────────

    public float CurrentHP => _currentHP;
    public float MaxHP => GameConfig.PLAYER_MAX_HP;
    public bool IsDead => _currentHP <= 0f;
}