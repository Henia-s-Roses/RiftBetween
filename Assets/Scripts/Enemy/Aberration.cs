// BossController.cs
// Stage 1 boss death → ServerChangeScene to Stage 2 (Mirror handles all clients)
// Stage 2 boss death → Win screen

using Mirror;
using UnityEngine;

public class BossController : EnemyBase
{
    [Header("Boss Settings")]
    public float moveSpeed = 1.8f;
    public float attackRate = 1.5f;
    public float attackDamage = 35f;

    [Header("Stage Outcome")]
    [Tooltip("True = Stage 2 boss. Defeat triggers win screen instead of scene change.")]
    public bool isFinalBoss = false;

    [SerializeField] public GameObject winScreen;

    private float _attackTimer;

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

    [Server]
    protected override void Die( )
    {
        RiftLogger.System($"Boss defeated — isFinalBoss: {isFinalBoss}", this);
        RpcOnDeath( );

        if (isFinalBoss)
        {
            // Final boss — trigger win on all clients, no scene change
            GameSession.CurrentPhase = GamePhase.Win;
            RpcTriggerWin( );
        } else
        {
            // Stage 1 boss — load Stage 2 for ALL clients via Mirror
            GameSession.CurrentPhase = GamePhase.Stage2;
            GameSession.IsStage2 = true;

            // Delay slightly so death RPC plays before scene tears down
            Invoke(nameof(LoadStage2), 1.5f);
        }

        NetworkServer.Destroy(gameObject);
    }

    [Server]
    private void LoadStage2( )
    {
        // ServerChangeScene broadcasts to ALL connected clients simultaneously
        // This is the only correct way to change scenes in Mirror multiplayer
        RiftNetworkManager.singleton.ServerChangeScene(
            RiftNetworkManager.singleton.stage2Scene
        );
        RiftLogger.System("Loading Stage 2 via ServerChangeScene", this);
    }

    [ClientRpc] private void RpcTriggerAttack( ) => enemyAnimator?.TriggerAttack( );
    [ClientRpc] private void RpcSetMoving(bool moving) => enemyAnimator?.SetMoving(moving);

    [ClientRpc]
    private void RpcTriggerWin( )
    {
        RiftLogger.System("Victory!", this);
        GameSession.CurrentPhase = GamePhase.Win;
        AudioManager.Instance?.PlayMusicVictory( );
        winScreen.SetActive(true);
    }
}