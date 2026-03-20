// Aberration.cs
// The sole boss of Rift Between. Appears at the end of Stage 1 and Stage 2.
// Stage 1: Phase 1 and Phase 2 only.
// Stage 2: Phase 1, Phase 2, and Phase 3.
//
// Phase 1 (100% - 60% HP): Melee like Knight, but stronger
// Phase 2 (60% - 30% HP):  Phase 1 + fires 3 orbs at each player every 3 seconds
// Phase 3 (30% HP, Stage 2 only): Phase 1 + Phase 2 + spawns a random enemy every 3 seconds

using Mirror;
using UnityEngine;
using System.Collections;

public class Aberration : EnemyBase
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Aberration Stats")]
    public float moveSpeed = 2f;
    public float attackRate = 1.2f;
    public float meleeDamage = 25f;
    public float detectionRange = 20f;   // Boss always detects players

    [Header("Phase 2 — Orb Attack")]
    public GameObject orbPrefab;
    public float orbDamage = 18f;
    public float orbFireRate = 3f;      // Seconds between orb volleys
    public int orbsPerVolley = 3;      // Fired at each player simultaneously

    [Header("Phase 3 — Enemy Spawning (Stage 2 Only)")]
    public float enemySpawnRate = 3f;
    public GameObject[] spawnableEnemies;  // Assign Knight, Mage, RiftSlime prefabs

    [Header("Phase Thresholds")]
    public float phase2Threshold = 0.6f;  // Enter phase 2 below 60% HP
    public float phase3Threshold = 0.3f;  // Enter phase 3 below 30% HP (Stage 2 only)

    // ── State ─────────────────────────────────────────────────────────────────

    [SyncVar(hook = nameof(OnPhaseChanged))]
    public int currentPhase = 1;

    private float meleeTimer = 0f;
    private bool phase2CoroutineRunning = false;
    private bool phase3CoroutineRunning = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void OnStartServer( )
    {
        base.OnStartServer( );

        // Boss stats are much higher than regular enemies
        maxHP = GameSession.IsStage2 ? 600f : 400f;
        currentHP = maxHP;

        RiftLogger.System("Aberration spawned — fight started", this);
    }

    // ── Server AI loop ────────────────────────────────────────────────────────

    private void FixedUpdate( )
    {
        if (!isServer) return;

        meleeTimer -= Time.fixedDeltaTime;

        CheckPhaseTransitions( );

        // Always chase and melee regardless of phase
        GameObject target = GetClosestPlayer( );
        if (target != null)
            ChaseMelee(target);
    }

    private void ChaseMelee(GameObject target)
    {
        Vector2 dir = ( target.transform.position - transform.position ).normalized;
        rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);

        if (spriteRenderer != null && dir.x != 0)
            spriteRenderer.flipX = dir.x < 0;
    }

    // ── Phase transitions ─────────────────────────────────────────────────────

    private void CheckPhaseTransitions( )
    {
        float hpPct = currentHP / maxHP;

        // Enter Phase 2
        if (currentPhase == 1 && hpPct <= phase2Threshold)
        {
            currentPhase = 2;   // SyncVar — all clients see the change
            RiftLogger.System($"Aberration entering Phase 2 at {hpPct:P0} HP", this);
        }

        // Enter Phase 3 — Stage 2 only
        if (currentPhase == 2 && hpPct <= phase3Threshold && GameSession.IsStage2)
        {
            currentPhase = 3;
            RiftLogger.System($"Aberration entering Phase 3 at {hpPct:P0} HP", this);
        }
    }

    // ── SyncVar hook — phase change ───────────────────────────────────────────

    // Fires on all clients when currentPhase changes.
    // Start phase behavior coroutines here.
    private void OnPhaseChanged(int oldPhase, int newPhase)
    {
        RiftLogger.Log($"Phase changed: {oldPhase} → {newPhase}", this);

        // Visual/audio feedback — add camera shake or audio sting here
        // e.g. CameraShake.Instance?.Shake(0.5f);

        // Phase coroutines only run on server
        if (!isServer) return;

        if (newPhase >= 2 && !phase2CoroutineRunning)
            StartCoroutine(Phase2OrbRoutine( ));

        if (newPhase >= 3 && !phase3CoroutineRunning)
            StartCoroutine(Phase3SpawnRoutine( ));
    }

    // ── Phase 2 — Orb volleys ─────────────────────────────────────────────────

    // Fires orbsPerVolley orbs spread in a fan toward each player every orbFireRate seconds.
    private IEnumerator Phase2OrbRoutine( )
    {
        phase2CoroutineRunning = true;
        RiftLogger.Log("Phase 2 orb routine started", this);

        while (currentPhase >= 2 && currentHP > 0)
        {
            yield return new WaitForSeconds(orbFireRate);

            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (var player in players)
            {
                // Skip downed players
                var revive = player.GetComponent<PlayerRevive>( );
                if (revive != null && revive.isDown) continue;

                FireOrbVolley(player.transform.position);
            }
        }

        phase2CoroutineRunning = false;
    }

    // Fires orbsPerVolley orbs in a spread fan toward a target position.
    private void FireOrbVolley(Vector3 targetPos)
    {
        if (orbPrefab == null) return;

        // Spread the orbs in a fan — e.g. 3 orbs: -15°, 0°, +15°
        float spreadAngle = 15f;
        float startAngle = -spreadAngle * ( orbsPerVolley - 1 ) / 2f;

        Vector2 baseDir = ( targetPos - transform.position ).normalized;
        float baseAngleDeg = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < orbsPerVolley; i++)
        {
            float angle = baseAngleDeg + startAngle + ( spreadAngle * i );
            Vector2 dir = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            GameObject orb = Instantiate(orbPrefab, transform.position, Quaternion.identity);
            orb.GetComponent<OrbProjectile>( )?.Initialize(dir, orbDamage, isEnemy: true);
            NetworkServer.Spawn(orb);
        }

        RiftLogger.Log($"Fired {orbsPerVolley} orbs toward {targetPos}", this);
    }

    // ── Phase 3 — Enemy spawning (Stage 2 only) ───────────────────────────────

    private IEnumerator Phase3SpawnRoutine( )
    {
        phase3CoroutineRunning = true;
        RiftLogger.Log("Phase 3 spawn routine started", this);

        while (currentPhase >= 3 && currentHP > 0)
        {
            yield return new WaitForSeconds(enemySpawnRate);
            SpawnRandomEnemy( );
        }

        phase3CoroutineRunning = false;
    }

    private void SpawnRandomEnemy( )
    {
        if (spawnableEnemies == null || spawnableEnemies.Length == 0)
        {
            RiftLogger.Warn("No spawnable enemies assigned on Aberration", this);
            return;
        }

        // Pick a random enemy type
        int index = Random.Range(0, spawnableEnemies.Length);
        GameObject prefab = spawnableEnemies[index];

        // Spawn slightly offset from the boss so it doesn't overlap
        Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0f, 0f);
        Vector3 spawnPos = transform.position + offset;

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        NetworkServer.Spawn(enemy);

        RiftLogger.Log($"Phase 3 spawned {prefab.name}", this);
    }

    // ── Contact damage ────────────────────────────────────────────────────────

    protected override void OnTriggerEnter2D(Collider2D other) { }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isServer) return;
        if (meleeTimer > 0f) return;

        var health = other.GetComponent<PlayerHealth>( );
        if (health != null)
        {
            health.TakeDamage(meleeDamage);
            meleeTimer = attackRate;
            RiftLogger.Log($"Aberration melee hit for {meleeDamage}", this);
        }
    }

    // ── Death ─────────────────────────────────────────────────────────────────

    [Server]
    protected override void Die( )
    {
        RiftLogger.System("Aberration defeated!", this);

        StopAllCoroutines( );
        RpcOnDeath( );

        // Notify the stage manager — triggers win sequence or stage transition
        GameSession.BossDefeated = true;
        GameSession.CurrentPhase = GameSession.IsStage2
            ? GamePhase.Win
            : GamePhase.Transition;

        // Give StageManager a moment to react before destroying
        Invoke(nameof(DestroyBoss), 1.5f);
    }

    private void DestroyBoss( )
    {
        NetworkServer.Destroy(gameObject);
    }
}