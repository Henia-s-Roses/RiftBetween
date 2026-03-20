// WorldManager.cs

using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class WorldManager : NetworkBehaviour
{
    public static WorldManager Instance { get; private set; }

    [SyncVar(hook = nameof(OnActiveWorldChanged))]
    public int activeWorld = (int)WorldState.WorldA;

    public static event System.Action<WorldState> OnWorldChanged;

    private bool _swapKeyWasHeld = false;

    private void Awake( )
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update( )
    {
        if (!isServer) return;

        var action = InputSystem.actions.FindAction("Test");
        if (action == null) return;

        bool isHeld = action.ReadValue<float>( ) > 0f;

        if (isHeld && !_swapKeyWasHeld)
            SetWorld(CurrentWorld == WorldState.WorldA ? WorldState.WorldB : WorldState.WorldA);

        _swapKeyWasHeld = isHeld;
    }

    public override void OnStartServer( )
    {
        activeWorld = (int)WorldState.WorldA;
    }

    public override void OnStartClient( )
    {
        OnWorldChanged?.Invoke((WorldState)activeWorld);
    }

    private void OnActiveWorldChanged(int oldWorld, int newWorld)
    {
        WorldState state = (WorldState)newWorld;
        GameSession.ActiveWorld = state;
        OnWorldChanged?.Invoke(state);
        RiftLogger.Log($"World changed to {state}", this);
    }

    [Server]
    public void SetWorld(WorldState world)
    {
        if ((WorldState)activeWorld == world) return;
        activeWorld = (int)world;
    }

    public WorldState CurrentWorld => (WorldState)activeWorld;
}