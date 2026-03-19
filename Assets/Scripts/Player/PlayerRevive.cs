// PlayerRevive.cs
// Manages the downed state and partner-revive mechanic.
//
// Flow:
//   TriggerDowned()         — called by PlayerHealth when HP hits 0
//   SetReviveInput(bool)    — called by PlayerInput each frame (I key held)
//   CmdAccumulateRevive()   — sent to server each frame partner is in range + holding I
//   CompleteRevive()        — server fires when 3s channel finishes
//   RpcGameOver()           — server fires when 15s downed timer expires

using Mirror;
using UnityEngine;
using System.Collections;

public class PlayerRevive : NetworkBehaviour
{
    // ── Synced state ──────────────────────────────────────────────────────────

    [SyncVar(hook = nameof(OnIsDownChanged))]
    public bool isDown = false;

    [SyncVar(hook = nameof(OnDownedTimerChanged))]
    public float downedTimer = 0f;

    [SyncVar(hook = nameof(OnReviveProgressChanged))]
    public float reviveProgress = 0f;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private bool _reviveInputHeld;
    private bool _partnerInRange;
    private Coroutine _downedCoroutine;

    // Cached references
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
    public void TriggerDowned( )
    {
        if (isDown) return; // Already downed — don't double trigger

        isDown = true;
        downedTimer = GameConfig.DOWNED_DURATION;
        reviveProgress = 0f;

        RiftLogger.System("Player entered DOWNED state", this);

        _downedCoroutine = StartCoroutine(DownedCountdown( ));
    }

    // Counts down the 15s downed timer — game over on expiry
    [Server]
    private IEnumerator DownedCountdown( )
    {
        while (downedTimer > 0f)
        {
            yield return new WaitForSeconds(0.1f); // Tick every 100ms — smooth enough for UI
            downedTimer -= 0.1f;
        }

        // Check if the other player is also downed
        RiftLogger.Warn("Downed timer expired — triggering Game Over", this);
        RpcGameOver("Downed timer expired.");
    }

    // ── Revive input — called by PlayerInput each frame ───────────────────────

    // The local player calls this every frame to report their held state
    public void SetReviveInput(bool held)
    {
        if (!isLocalPlayer) return;

        _reviveInputHeld = held;

        // Send to server every frame while held and partner is in range
        if (held && _partnerInRange)
            CmdAccumulateRevive(Time.deltaTime);
        else if (!held && _partnerInRange)
            CmdResetReviveProgress( ); // Released input — reset progress
    }

    // ── Partner proximity — called by ReviveZone ──────────────────────────────

    public void SetPartnerInRange(bool inRange)
    {
        _partnerInRange = inRange;

        // Partner left range mid-channel — reset progress
        if (!inRange)
            CmdResetReviveProgress( );
    }

    // ── Commands — client → server ────────────────────────────────────────────

    [Command]
    private void CmdAccumulateRevive(float delta)
    {
        // Only accumulate if the target player (not this player) is downed
        // Find the other player and check their isDown state
        var otherPlayer = GetOtherPlayer( );
        if (otherPlayer == null || !otherPlayer.isDown) return;

        otherPlayer.reviveProgress += delta;
        RiftLogger.Log($"Revive progress: {otherPlayer.reviveProgress:F1}/{GameConfig.REVIVE_DURATION}s", this);

        if (otherPlayer.reviveProgress >= GameConfig.REVIVE_DURATION)
            otherPlayer.CompleteRevive( );
    }

    [Command]
    private void CmdResetReviveProgress( )
    {
        var otherPlayer = GetOtherPlayer( );
        if (otherPlayer == null) return;

        if (otherPlayer.reviveProgress > 0f)
        {
            RiftLogger.Log("Revive progress reset — partner left range or released input", this);
            otherPlayer.reviveProgress = 0f;
        }
    }

    // ── Server: complete the revive ───────────────────────────────────────────

    [Server]
    public void CompleteRevive( )
    {
        if (_downedCoroutine != null)
            StopCoroutine(_downedCoroutine);

        isDown = false;
        reviveProgress = 0f;
        downedTimer = 0f;

        // Restore 30% HP
        float healAmount = GameConfig.PLAYER_MAX_HP * GameConfig.REVIVE_HP_PCT;
        _health.Heal(healAmount);

        // Grant invincibility frames
        _health.StartInvincibility(GameConfig.REVIVE_INVINCIBILITY);

        RiftLogger.System("Revive complete", this);

        // Update score
        GameSession.SuccessfulRevives++;
        GameSession.AddScore(GameConfig.SCORE_REVIVE, ScoreCategory.Revive);

        RpcOnRevived( );
    }

    // ── RPCs — server → all clients ──────────────────────────────────────────

    [ClientRpc]
    private void RpcOnRevived( )
    {
        RiftLogger.Log("RpcOnRevived received", this);
        // Animator will pick this up via the isDown SyncVar hook going false
    }

    [ClientRpc]
    private void RpcGameOver(string reason)
    {
        RiftLogger.System($"Game Over: {reason}", this);
        GameSession.CompletedNoWipe = false;
        GameSession.CurrentPhase = GamePhase.GameOver;

        // GameOverManager handles the actual UI — stub for now
        // GameOverManager.Instance?.TriggerGameOver(reason);
    }

    // ── SyncVar hooks — fire on all clients ──────────────────────────────────

    private void OnIsDownChanged(bool oldVal, bool newVal)
    {
        RiftLogger.Log($"isDown changed → {newVal}", this);

        // Lock/unlock movement and attack on all clients
        _movement?.SetMovementLocked(newVal);
        _attack?.SetAttackLocked(newVal);
        _animator?.SetDowned(newVal);
    }

    private void OnDownedTimerChanged(float oldVal, float newVal)
    {
        // UI reacts — ReviveTimerUI subscribes to this event
        OnTimerUpdated?.Invoke(newVal);
    }

    private void OnReviveProgressChanged(float oldVal, float newVal)
    {
        // UI reacts — ReviveProgressUI subscribes to this event
        OnProgressUpdated?.Invoke(newVal, GameConfig.REVIVE_DURATION);
    }

    // ── Events for UI ────────────────────────────────────────────────────────

    public event System.Action<float> OnTimerUpdated;    // (remainingSeconds)
    public event System.Action<float, float> OnProgressUpdated; // (progress, maxProgress)

    // ── Helper ────────────────────────────────────────────────────────────────

    // Finds the other RiftNetworkPlayer in the scene — works for 2-player games
    private PlayerRevive GetOtherPlayer( )
    {
        foreach (var netPlayer in FindObjectsOfType<RiftNetworkPlayer>( ))
        {
            if (netPlayer.gameObject != gameObject)
                return netPlayer.GetComponent<PlayerRevive>( );
        }
        return null;
    }
}