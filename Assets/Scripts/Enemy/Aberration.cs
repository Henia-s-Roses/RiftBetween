// BossController.cs
// On Stage 1 boss death: swaps stage GameObjects and teleports players to Stage 2 spawn points.
// On Stage 2 boss death: triggers win screen.
// Everything happens in one scene — no scene loading.

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
    [Tooltip("True = this is the Stage 2 boss. Defeat triggers win screen.")]
    public bool isFinalBoss = false;

    [Header("Stage Transition (Stage 1 boss only)")]
    [Tooltip("Root GameObject of Stage 1 — disabled after Stage 1 boss dies")]
    public GameObject stage1Root;

    [Tooltip("Root GameObject of Stage 2 — enabled after Stage 1 boss dies")]
    public GameObject stage2Root;

    [Tooltip("Spawn point for Player 1 at the start of Stage 2")]
    public Transform stage2SpawnPoint1;

    [Tooltip("Spawn point for Player 2 at the start of Stage 2")]
    public Transform stage2SpawnPoint2;

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

        if (dir.x != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = dir.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

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

    // ── Death ─────────────────────────────────────────────────────────────────

    [Server]
    protected override void Die( )
    {
        RiftLogger.System($"Boss defeated — isFinalBoss: {isFinalBoss}", this);
        RpcOnDeath( );

        if (isFinalBoss)
        {
            GameSession.CurrentPhase = GamePhase.Win;
            RpcTriggerWin( );
        } else
        {
            GameSession.CurrentPhase = GamePhase.Stage2;
            GameSession.IsStage2 = true;

            // Collect player positions before doing anything else
            Vector3 spawn1 = stage2SpawnPoint1 != null
                ? stage2SpawnPoint1.position
                : new Vector3(-2f, 0f, 0f);

            Vector3 spawn2 = stage2SpawnPoint2 != null
                ? stage2SpawnPoint2.position
                : new Vector3(2f, 0f, 0f);

            // Swap stage geometry on all clients
            RpcSwapStage(spawn1, spawn2);
        }

        NetworkServer.Destroy(gameObject);
    }

    // ── RPCs ──────────────────────────────────────────────────────────────────

    [ClientRpc]
    private void RpcSwapStage(Vector3 spawn1, Vector3 spawn2)
    {
        RiftLogger.System("Stage 1 → Stage 2 swap", this);

        if (stage1Root != null) stage1Root.SetActive(false);
        if (stage2Root != null) stage2Root.SetActive(true);

        // Find and teleport players by sorted index so P1 always hits spawn1
        var players = FindObjectsOfType<RiftNetworkPlayer>( );
        System.Array.Sort(players, (a, b) => a.playerIndex.CompareTo(b.playerIndex));

        for (int i = 0; i < players.Length; i++)
        {
            Vector3 spawnPos = i == 0 ? spawn1 : spawn2;
            players[i].transform.position = spawnPos;

            // Restore full HP on stage transition
            var health = players[i].GetComponent<PlayerHealth>( );
            health?.Heal(GameConfig.PLAYER_MAX_HP);

            // Brief invincibility so they don't take damage on spawn
            health?.StartInvincibility(2f);

            RiftLogger.Log($"Player {players[i].playerIndex} teleported to {spawnPos}", this);
        }

        // Swap music to Stage 2 track
        AudioManager.Instance?.PlayMusicStage2( );
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

    [ClientRpc]
    private void RpcTriggerWin( )
    {
        RiftLogger.System("Final boss defeated — Victory!", this);
        GameSession.CurrentPhase = GamePhase.Win;
        AudioManager.Instance?.PlayMusicVictory( );
        // WinUI.Instance?.Show();
    }
}