// WorldSwapTimer.cs

using Mirror;
using UnityEngine;
using System.Collections;

public class WorldSwapTimer : NetworkBehaviour
{
    [Header("Swap Interval (seconds)")]
    public float minInterval = GameConfig.SWAP_MIN_1;
    public float maxInterval = GameConfig.SWAP_MAX_1;

    [SyncVar]
    public float timeRemaining = 0f;

    private bool _frozen = false;
    private Coroutine _timerCoroutine;
    private WorldManager _worldManager;

    private void Awake( )
    {
        _worldManager = GetComponent<WorldManager>( );
    }

    public override void OnStartServer( )
    {
        StartSwapTimer( );
    }

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
        timeRemaining = Random.Range(minInterval, maxInterval);
        RiftLogger.Log($"Next swap in {timeRemaining:F1}s", this);

        while (timeRemaining > 0f)
        {
            if (!_frozen)
                timeRemaining -= Time.deltaTime;

            yield return null;
        }

        timeRemaining = 0f;
        PerformSwap( );
        StartSwapTimer( );
    }

    [Server]
    private void PerformSwap( )
    {
        if (_frozen) return;

        // Simple toggle — only two worlds now
        WorldState next = _worldManager.CurrentWorld == WorldState.WorldA
            ? WorldState.WorldB
            : WorldState.WorldA;

        RiftLogger.System($"World swap: {_worldManager.CurrentWorld} → {next}", this);
        _worldManager.SetWorld(next);
    }

    [Server]
    public void FreezeSwap( )
    {
        _frozen = true;
        RiftLogger.Log("Swap timer frozen", this);
    }

    [Server]
    public void UnfreezeSwap( )
    {
        _frozen = false;
        RiftLogger.Log("Swap timer unfrozen", this);
    }

    public bool IsFrozen => _frozen;
}