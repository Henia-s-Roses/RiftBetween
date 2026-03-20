// BossController.cs


using Mirror;
using UnityEngine;

public class BossController : EnemyBase
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Boss Settings")]
    public float moveSpeed = 1.8f;
    public float attackRate = 1.5f;
    public float attackDamage = 35f;

    [Header("Stage Outcome")]
    [Tooltip("True = this is the Stage 2 boss. Defeating it triggers the Win screen instead of loading a new scene.")]
    public bool isFinalBoss = false;

    [Tooltip("Scene to load after defeating this boss. Ignored if isFinalBoss is true.")]
    public string nextScene = "Stage2";

    // ── Runtime ───────────────────────────────────────────────────────────────

    private float _attackTimer;

    // ── AI ────────────────────────────────────────────────────────────────────

    private void FixedUpdate( )
    {
        if (!isServer) return;

        _attackTimer -= Time.fixedDeltaTime;

        GameObject target = GetClosestPlayer( );

        if (target == null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            RpcSetMoving(false);
            return;
        }

        Vector2 dir = ( target.transform.position - transform.position ).normalized;
        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
        RpcSetMoving(true);

        // Flip to face target
        if (dir.x != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = dir.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    // Use timed contact damage — not per-frame
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

    // ── Death — triggers stage transition or win ───────────────────────────────

    [Server]
    protected override void Die( )
    {
        RiftLogger.System($"Boss defeated — isFinalBoss: {isFinalBoss}", this);

        RpcOnDeath( );

        if (isFinalBoss)
        {
            // Stage 2 boss dead — trigger win on all clients
            GameSession.CurrentPhase = GamePhase.Win;
            RpcTriggerWin( );
        } else
        {
            // Stage 1 boss dead — load Stage 2
            GameSession.CurrentPhase = GamePhase.Transition;
            RpcPrepareTransition( );

            // Small delay so death animation can play before scene change
            Invoke(nameof(LoadNextScene), 1.5f);
        }

        NetworkServer.Destroy(gameObject);
    }

    [Server]
    private void LoadNextScene( )
    {
        RiftNetworkManager.singleton.ServerChangeScene(nextScene);
    }

    // ── RPCs ──────────────────────────────────────────────────────────────────

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

    // Fires on all clients just before the scene loads —
    // use this to show a "Stage Clear" banner or fade out if you have one
    [ClientRpc]
    private void RpcPrepareTransition( )
    {
        RiftLogger.System("Stage 1 boss defeated — transitioning to Stage 2", this);
        // StageClearUI.Instance?.Show("Stage Clear!");
    }

    // Fires on all clients when the final boss dies
    [ClientRpc]
    private void RpcTriggerWin( )
    {
        RiftLogger.System("Final boss defeated — Victory!", this);
        GameSession.CurrentPhase = GamePhase.Win;
        // WinUI.Instance?.Show();
    }
}