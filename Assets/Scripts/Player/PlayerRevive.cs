// PlayerRevive.cs

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

    private PlayerMovement _movement;
    private PlayerAttack _attack;
    private PlayerHealth _health;
    private PlayerAnimator _animator;

    // ── Public read — used by ReviveZone to avoid double-setting ─────────────
    public bool IsPartnerInRange => _partnerInRange;

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
        if (isDown) return;

        isDown = true;
        downedTimer = GameConfig.DOWNED_DURATION;
        reviveProgress = 0f;

        RiftLogger.System("Player entered DOWNED state", this);
        _downedCoroutine = StartCoroutine(DownedCountdown( ));
    }

    [Server]
    private IEnumerator DownedCountdown( )
    {
        while (downedTimer > 0f)
        {
            yield return new WaitForSeconds(0.1f);
            downedTimer -= 0.1f;
        }

        RiftLogger.Warn("Downed timer expired — Game Over", this);
        RpcGameOver("Downed timer expired.");
    }

    // ── Revive input — called by PlayerInput every frame ─────────────────────

    public void SetReviveInput(bool held)
    {
        if (!isLocalPlayer) return;

        _reviveInputHeld = held;

        if (held && _partnerInRange)
            CmdAccumulateRevive(Time.deltaTime);
        else if (!held && _partnerInRange)
            CmdResetReviveProgress( );
    }

    // ── Partner proximity — called by ReviveZone ──────────────────────────────

    // Made public so ReviveZone (MonoBehaviour) can call it directly.
    // ReviveZone is on the same GameObject so this is a safe local call.
    public void SetPartnerInRange(bool inRange)
    {
        _partnerInRange = inRange;

        if (!inRange)
            CmdResetReviveProgress( );
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [Command]
    private void CmdAccumulateRevive(float delta)
    {
        var other = GetOtherPlayer( );
        if (other == null || !other.isDown) return;

        other.reviveProgress += delta;
        RiftLogger.Log($"Revive: {other.reviveProgress:F1}/{GameConfig.REVIVE_DURATION}s", this);

        if (other.reviveProgress >= GameConfig.REVIVE_DURATION)
            other.CompleteRevive( );
    }

    [Command]
    private void CmdResetReviveProgress( )
    {
        var other = GetOtherPlayer( );
        if (other == null || other.reviveProgress <= 0f) return;

        RiftLogger.Log("Revive progress reset", this);
        other.reviveProgress = 0f;
    }

    // ── Revive completion ─────────────────────────────────────────────────────

    [Server]
    public void CompleteRevive( )
    {
        if (_downedCoroutine != null)
            StopCoroutine(_downedCoroutine);

        isDown = false;
        reviveProgress = 0f;
        downedTimer = 0f;

        _health?.Heal(GameConfig.PLAYER_MAX_HP * GameConfig.REVIVE_HP_PCT);
        _health?.StartInvincibility(GameConfig.REVIVE_INVINCIBILITY);

        GameSession.SuccessfulRevives++;
        GameSession.AddScore(GameConfig.SCORE_REVIVE, ScoreCategory.Revive);

        RiftLogger.System("Revive complete", this);
        RpcOnRevived( );
    }

    // ── RPCs ──────────────────────────────────────────────────────────────────

    [ClientRpc]
    private void RpcOnRevived( )
    {
        // isDown SyncVar hook fires SetDowned(false) on animator automatically
        RiftLogger.Log("RpcOnRevived received", this);
    }

    [ClientRpc]
    private void RpcGameOver(string reason)
    {
        RiftLogger.System($"Game Over: {reason}", this);
        GameSession.CompletedNoWipe = false;
        GameSession.CurrentPhase = GamePhase.GameOver;
        // GameOverManager.Instance?.TriggerGameOver(reason);
    }

    // ── SyncVar hooks ─────────────────────────────────────────────────────────

    private void OnIsDownChanged(bool oldVal, bool newVal)
    {
        RiftLogger.Log($"isDown → {newVal}", this);
        _movement?.SetMovementLocked(newVal);
        _attack?.SetAttackLocked(newVal);
        _animator?.SetDowned(newVal);
    }

    private void OnDownedTimerChanged(float oldVal, float newVal)
        => OnTimerUpdated?.Invoke(newVal);

    private void OnReviveProgressChanged(float oldVal, float newVal)
        => OnProgressUpdated?.Invoke(newVal, GameConfig.REVIVE_DURATION);

    // ── UI events ─────────────────────────────────────────────────────────────

    public event System.Action<float> OnTimerUpdated;
    public event System.Action<float, float> OnProgressUpdated;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private PlayerRevive GetOtherPlayer( )
    {
        foreach (var p in FindObjectsOfType<PlayerRevive>( ))
        {
            if (p != this) return p;
        }
        return null;
    }
}