// WorldSwapTimer.cs
// Runs the countdown and triggers world swaps on the server.
// Attach this to the same GameObject as WorldManager.
//
// The timer only runs on the server — clients read timeRemaining
// via SyncVar for HUD display only.

using Mirror;
using UnityEngine;
using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

public class WorldSwapTimer : NetworkBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Stage 1 Interval (seconds)")]
    public float minInterval1 = GameConfig.SWAP_MIN_1;
    public float maxInterval1 = GameConfig.SWAP_MAX_1;

    [Header("Stage 2 Interval (seconds)")]
    public float minInterval2 = GameConfig.SWAP_MIN_2;
    public float maxInterval2 = GameConfig.SWAP_MAX_2;

    // ── Synced state ──────────────────────────────────────────────────────────

    // Clients read this for the HUD countdown display
    [SyncVar]
    public float timeRemaining = 0f;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private bool _frozen = false;   // True during boss fight — no swaps
    private Coroutine _timerCoroutine;
    private WorldManager _worldManager;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake( )
    {
        _worldManager = GetComponent<WorldManager>( );
    }

    public override void OnStartServer( )
    {
        // Start the timer automatically when the scene loads on the server
        StartSwapTimer( );
    }

    // ── Timer ─────────────────────────────────────────────────────────────────

    // Starts or restarts the swap countdown.
    // Called automatically on scene load, and again after each swap.
    [Server]
    public void StartSwapTimer( )
    {
        if (_timerCoroutine != null)
            StopCoroutine(_timerCoroutine);

        _timerCoroutine = StartCoroutine(SwapCoroutine( ));
    }

    [Server]
    private IEnumerator SwapCoroutine( )
    {
        // Pick interval based on current stage
        float min = GameSession.IsStage2 ? minInterval2 : minInterval1;
        float max = GameSession.IsStage2 ? maxInterval2 : maxInterval1;

        timeRemaining = Random.Range(min, max);
        RiftLogger.Log($"Next swap in {timeRemaining:F1}s", this);

        while (timeRemaining > 0f)
        {
            // Pause countdown while frozen (boss fight)
            if (!_frozen)
                timeRemaining -= Time.deltaTime;

            yield return null;
        }

        timeRemaining = 0f;
        PerformSwap( );

        // Immediately queue the next swap
        StartSwapTimer( );
    }

    [Server]
    private void PerformSwap( )
    {
        if (_frozen) return;

        WorldState current = _worldManager.CurrentWorld;
        WorldState next;

        if (GameSession.IsStage2)
        {
            // Stage 2: cycles WorldA → WorldB → Rift → WorldA
            next = current switch
            {
                WorldState.WorldA => WorldState.WorldB,
                WorldState.WorldB => WorldState.Rift,
                WorldState.Rift => WorldState.WorldA,
                _ => WorldState.WorldA
            };
        } else
        {
            // Stage 1: toggles between WorldA and WorldB only
            next = current == WorldState.WorldA
                ? WorldState.WorldB
                : WorldState.WorldA;
        }

        RiftLogger.System($"World swap: {current} → {next}", this);
        _worldManager.SetWorld(next);
    }

    // ── Freeze control (called by BossTriggerZone) ────────────────────────────

    // Stops the countdown — world stays locked during boss fight
    [Server]
    public void FreezeSwap( )
    {
        _frozen = true;
        RiftLogger.Log("Swap timer frozen — boss active", this);
    }

    // Resumes the countdown — call this if you ever want swaps to resume
    [Server]
    public void UnfreezeSwap( )
    {
        _frozen = false;
        RiftLogger.Log("Swap timer unfrozen", this);
    }

    public bool IsFrozen => _frozen;
}
