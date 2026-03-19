// PlayerHealth.cs
// Server-authoritative HP management.
// Never modify HP on the client — always go through CmdTakeDamage / CmdHeal.

using Mirror;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    // ── Synced state ──────────────────────────────────────────────────────────

    [SyncVar(hook = nameof(OnHPChanged))]
    private float _currentHP;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private bool _isInvincible; // Server-side only — not synced, not needed on clients

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void OnStartServer( )
    {
        _currentHP = GameConfig.PLAYER_MAX_HP;
    }

    // ── Taking damage ─────────────────────────────────────────────────────────

    // Called by enemy contact, boss attacks, etc.
    // Safe to call from server directly OR via Command from client.
    [Server]
    public void TakeDamage(float amount)
    {
        if (_isInvincible)
        {
            RiftLogger.Log($"Damage blocked — invincible ({amount} ignored)", this);
            return;
        }

        _currentHP = Mathf.Max(0f, _currentHP - amount);
        RiftLogger.Log($"Took {amount} dmg — HP {_currentHP}/{GameConfig.PLAYER_MAX_HP}", this);

        if (_currentHP <= 0f)
        {
            RiftLogger.Warn("HP hit 0 — triggering downed", this);
            GetComponent<PlayerRevive>( )?.TriggerDowned( );
        }
    }

    // Client requests damage — server validates and applies.
    // Use this when damage source is detected client-side (e.g. player walks into enemy).
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