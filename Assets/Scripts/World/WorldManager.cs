// WorldManager.cs
// Minimal version for testing — no swapping, no timer.
// Just holds the current world state and fires OnWorldChanged once on start
// so all subscribers (enemies, crates) apply their correct skin on spawn.

using Mirror;
using UnityEngine;

public class WorldManager : NetworkBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static WorldManager Instance { get; private set; }

    // ── World state ───────────────────────────────────────────────────────────

    // Synced so all clients read the same world.
    // Hook fires OnWorldChanged on every client when this changes.
    [SyncVar(hook = nameof(OnActiveWorldChanged))]
    public int activeWorld = (int)WorldState.WorldA;

    // Subscribe to this to react to world changes.
    // Enemies, crates, tilemaps, and HUD all listen here.
    public static event System.Action<WorldState> OnWorldChanged;

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


    // TEST TEST TEST //
    private void Update( )
    {
        if (!isServer) return;
        if (Input.GetKeyDown(KeyCode.T))
            SetWorld(CurrentWorld == WorldState.WorldA ? WorldState.WorldB : WorldState.WorldA);
    }

    public override void OnStartClient( )
    {
        // Fire immediately so all clients initialize with the correct world on join
        OnWorldChanged?.Invoke((WorldState)activeWorld);
    }

    public override void OnStartServer( )
    {
        // Default to World A — change this in Inspector or via SetWorld() for testing
        activeWorld = (int)WorldState.WorldA;
    }

    // ── SyncVar hook ──────────────────────────────────────────────────────────

    // Fires on ALL clients (including host) when activeWorld changes.
    private void OnActiveWorldChanged(int oldWorld, int newWorld)
    {
        WorldState state = (WorldState)newWorld;
        GameSession.ActiveWorld = state;
        OnWorldChanged?.Invoke(state);

        RiftLogger.Log($"World changed to {state}", this);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    // Call this from server to manually set the world during testing.
    // e.g. WorldManager.Instance.SetWorld(WorldState.WorldB);
    [Server]
    public void SetWorld(WorldState world)
    {
        activeWorld = (int)world;
    }

    // Convenience read for other scripts
    public WorldState CurrentWorld => (WorldState)activeWorld;
}