// PlayerRespawn.cs


using Mirror;
using UnityEngine;
using System.Collections;

public class PlayerRespawn : NetworkBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Respawn Settings")]
    public float respawnDelay = 5f;     // Seconds before respawning
    public float respawnHP = 0.5f;   // HP percentage restored on respawn (50%)

    // ── Synced state ──────────────────────────────────────────────────────────

    // Synced so both clients can see if a player is dead (for game over check)
    [SyncVar(hook = nameof(OnIsDeadChanged))]
    public bool isDead = false;

    // Countdown shown on the dead player's screen
    [SyncVar(hook = nameof(OnRespawnTimerChanged))]
    public float respawnTimer = 0f;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private PlayerMovement _movement;
    private PlayerAttack _attack;
    private PlayerHealth _health;
    private PlayerAnimator _animator;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake( )
    {
        _movement = GetComponent<PlayerMovement>( );
        _attack = GetComponent<PlayerAttack>( );
        _health = GetComponent<PlayerHealth>( );
        _animator = GetComponent<PlayerAnimator>( );
    }

    // ── Called by PlayerHealth when HP hits 0 ─────────────────────────────────

    [Server]
    public void TriggerDeath( )
    {
        if (isDead) return; // Prevent double trigger

        isDead = true;
        respawnTimer = respawnDelay;

        RiftLogger.System($"Player died — checking game over", this);

        // Check if the other player is also dead
        var other = GetOtherPlayer( );
        if (other != null && other.isDead)
        {
            // Both dead — Game Over
            RiftLogger.System("Both players dead — Game Over", this);
            RpcGameOver( );
            return;
        }

        // Partner is alive — start respawn countdown
        StartCoroutine(RespawnCountdown( ));
    }

    // ── Respawn countdown ─────────────────────────────────────────────────────

    [Server]
    private IEnumerator RespawnCountdown( )
    {
        RiftLogger.Log($"Respawning in {respawnDelay}s", this);

        while (respawnTimer > 0f)
        {
            yield return new WaitForSeconds(0.1f);
            respawnTimer -= 0.1f;
        }

        respawnTimer = 0f;
        Respawn( );
    }

    [Server]
    private void Respawn( )
    {
        // Find the surviving partner to spawn next to
        var other = GetOtherPlayer( );

        if (other == null)
        {
            RiftLogger.Warn("Respawn: no surviving partner found", this);
            return;
        }

        // Teleport to partner's current position
        Vector3 spawnPos = other.transform.position;

        // Slight offset so they don't overlap exactly
        spawnPos += new Vector3(1f, 0f, 0f);

        RpcTeleport(spawnPos);

        // Restore HP
        float healAmt = GameConfig.PLAYER_MAX_HP * respawnHP;
        _health.Heal(healAmt);

        // Brief invincibility so they don't die instantly on spawn
        _health.StartInvincibility(2f);

        isDead = false; // SyncVar — fires OnIsDeadChanged on all clients

        RiftLogger.System($"Player respawned at {spawnPos}", this);
    }

    // ── RPCs ──────────────────────────────────────────────────────────────────

    // Teleports the player on all clients — keeps position in sync immediately
    // without waiting for NetworkTransform's next tick
    [ClientRpc]
    private void RpcTeleport(Vector3 position)
    {
        transform.position = position;
        RiftLogger.Log($"Teleported to {position}", this);
    }

    [ClientRpc]
    private void RpcGameOver( )
    {
        RiftLogger.System("Game Over — both players dead", this);
        GameSession.CurrentPhase = GamePhase.GameOver;
        GameSession.CompletedNoWipe = false;

        // GameOverManager.Instance?.TriggerGameOver("Both players died.");
    }

    // ── SyncVar hooks — fire on all clients ──────────────────────────────────

    private void OnIsDeadChanged(bool oldVal, bool newVal)
    {
        RiftLogger.Log($"isDead → {newVal}", this);

        // Lock input and play death animation when dead
        // Unlock everything when respawned (newVal = false)
        _movement?.SetMovementLocked(newVal);
        _attack?.SetAttackLocked(newVal);
        _animator?.SetDowned(newVal); // Reuses the downed animation for death
    }

    private void OnRespawnTimerChanged(float oldVal, float newVal)
    {
        // UI can subscribe here for a countdown display
        OnTimerUpdated?.Invoke(newVal);
    }

    // ── UI event ──────────────────────────────────────────────────────────────

    public event System.Action<float> OnTimerUpdated; // (remainingSeconds)

    // ── Helper ────────────────────────────────────────────────────────────────

    private PlayerRespawn GetOtherPlayer( )
    {
        foreach (var p in FindObjectsOfType<PlayerRespawn>( ))
        {
            if (p != this && !p.isDead) return p;
        }
        return null;
    }
}