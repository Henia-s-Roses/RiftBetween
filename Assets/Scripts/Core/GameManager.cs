// GameManager.cs
// Minimal test version — no win/loss, no boss, no HUD.
// Handles player spawn confirmation and gives you a clean
// entry point to add game logic later.

using Mirror;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static GameManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Test Settings")]
    [Tooltip("Prefabs to place in the scene for testing — drag Knight, Mage, or RiftSlime here")]
    public GameObject[] testEnemyPrefabs;

    [Tooltip("Where to spawn test enemies")]
    public Transform[] enemySpawnPoints;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake( )
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnStartServer( )
    {
        RiftLogger.System("GameManager started on server — test stage ready", this);
        SpawnTestEnemies( );
    }

    // ── Test enemy spawning ───────────────────────────────────────────────────

    // Spawns one of each assigned enemy prefab at the assigned spawn points.
    // Only runs on server.
    [Server]
    private void SpawnTestEnemies( )
    {
        if (testEnemyPrefabs == null || testEnemyPrefabs.Length == 0)
        {
            RiftLogger.Warn("No test enemy prefabs assigned on GameManager", this);
            return;
        }

        for (int i = 0; i < testEnemyPrefabs.Length; i++)
        {
            if (testEnemyPrefabs[i] == null) continue;

            // Cycle through spawn points — wraps if fewer points than enemies
            Vector3 pos = enemySpawnPoints != null && enemySpawnPoints.Length > 0
                ? enemySpawnPoints[i % enemySpawnPoints.Length].position
                : Vector3.right * ( i * 3f ); // Fallback: spread them apart on X

            GameObject enemy = Instantiate(testEnemyPrefabs[i], pos, Quaternion.identity);
            NetworkServer.Spawn(enemy);

            RiftLogger.Log($"Spawned test enemy: {testEnemyPrefabs[i].name} at {pos}", this);
        }
    }
}
